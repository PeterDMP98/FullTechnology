// VentaRepository.cs — Registro atómico de ventas con detalles, descuento de stock y auto-generación del número VNT-año-NNNNN.
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a Ventas y VentaDetalles replicando 1:1 la v2.x (Database.cs).
/// La venta se guarda en una transacción: cabecera + cada una de sus líneas +
/// descuento de stock, de modo que si algo falla a mitad de camino no queda una
/// venta a la que le falten productos (ni stock descontado sin venta). El número
/// de venta se auto-genera con el patrón VNT-año-NNNNN (NextVentaNumber).
/// </summary>
public sealed class VentaRepository : IVentaRepository
{
    private readonly SqliteConnectionFactory _factory;

    public VentaRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Persiste una venta completa (cabecera + líneas) y descuenta el stock de
    /// cada producto vendido, todo dentro de una única transacción SQLite.
    /// La contabilidad del negocio depende de que venta y detalles sean
    /// consistentes: venta sin líneas no tendría sentido, y un descuento de
    /// stock sin venta sería un inventario corrupto; por eso el commit solo se
    /// llega a hacer si TODAS las inserciones terminan bien.
    /// </summary>
    public long SaveVenta(Venta v, List<VentaDetalle> det)
    {
        using var c = _factory.CreateConnection();
        using var tx = c.BeginTransaction();
        using (var cmd = c.CreateCommand())
        {
            // Cabecera de la venta; VentaNumber lo adjudica el UI (NextVentaNumber)
            // y el Id autogenerado se captura para vincular las líneas después.
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Ventas (VentaNumber,Fecha,Subtotal,Descuento,Total,MetodoPago,ClienteId,ClienteNombre) VALUES (@vn,@f,@st,@d,@t,@mp,@ci,@cn);";
            cmd.Parameters.AddWithValue("@vn", v.VentaNumber);
            cmd.Parameters.AddWithValue("@f", v.Fecha.ToString("o"));
            cmd.Parameters.AddWithValue("@st", v.Subtotal);
            cmd.Parameters.AddWithValue("@d", v.Descuento);
            cmd.Parameters.AddWithValue("@t", v.Total);
            cmd.Parameters.AddWithValue("@mp", (object?)v.MetodoPago ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ci", (object?)v.ClienteId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cn", (object?)v.ClienteNombre ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT last_insert_rowid();";
            v.Id = Convert.ToInt64(cmd.ExecuteScalar());
        }
        // Por cada línea vendida se inserta su detalle (con nombre y precio de
        // venta congelados en el momento de la venta) y se descuenta la cantidad
        // del stock del producto en el mismo paso transaccional.
        foreach (var d in det)
        {
            using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO VentaDetalles (VentaId,ProductoId,NombreProducto,PrecioVenta,Cantidad) VALUES (@vi,@pi,@np,@pv,@ca);";
            cmd.Parameters.AddWithValue("@vi", v.Id);
            cmd.Parameters.AddWithValue("@pi", d.ProductoId);
            cmd.Parameters.AddWithValue("@np", (object?)d.ProductoNombre ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pv", d.PrecioUnitario);
            cmd.Parameters.AddWithValue("@ca", d.Cantidad);
            cmd.ExecuteNonQuery();

            // Descontar stock
            // El descuento es atómico sobre el valor actual (Stock - @ca), no
            // sobre un valor leído de antes: si no hay existencias suficientes
            // podría quedar negativo, comportamiento deliberadamente igual al de
            // la v2.x (el control de stock disponible se hace en la pantalla).
            using var up = c.CreateCommand();
            up.Transaction = tx;
            up.CommandText = "UPDATE Productos SET Stock = Stock - @ca WHERE Id=@pi";
            up.Parameters.AddWithValue("@ca", d.Cantidad);
            up.Parameters.AddWithValue("@pi", d.ProductoId);
            up.ExecuteNonQuery();
        }
        tx.Commit();
        return v.Id;
    }

    /// <summary>
    /// Genera el número de la siguiente venta como VNT-<año>-<secuencial de 5
    /// dígitos>. Replica exactamente la v2.x: COUNT(*) + 1 (no el MAX) sobre la
    /// tabla Ventas, por lo que tras un borrado puede reutilizar un número; el
    /// prefijo del año lo mantiene legible. Ejemplo: VNT-2026-00023.
    /// </summary>
    public string NextVentaNumber()
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Ventas";
        var n = Convert.ToInt64(cmd.ExecuteScalar()) + 1;
        return $"VNT-{DateTime.Now:yyyy}-{n:00000}";
    }

    /// <summary>
    /// Listado de ventas con filtros opcionales. El patrón "WHERE 1=1 + añadir
    /// condiciones" se mantiene de la v2.x: rango de fechas (el límite superior
    /// se lleva al final del día con AddDays(1).AddSeconds(-1)), método de pago
    /// EXACTO y búsqueda libre con LIKE sobre número y nombre de cliente;
    /// ordenadas de la más reciente a la más antigua.
    /// </summary>
    public List<Venta> GetVentas(DateTime? from = null, DateTime? to = null, string? metodo = null, string? search = null)
    {
        var list = new List<Venta>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        var sql = "SELECT * FROM Ventas WHERE 1=1";
        if (from != null) { sql += " AND Fecha>=@from"; cmd.Parameters.AddWithValue("@from", from.Value.ToString("o")); }
        if (to != null) { sql += " AND Fecha<=@to"; cmd.Parameters.AddWithValue("@to", to.Value.Date.AddDays(1).AddSeconds(-1).ToString("o")); }
        if (!string.IsNullOrWhiteSpace(metodo)) { sql += " AND MetodoPago=@metodo"; cmd.Parameters.AddWithValue("@metodo", metodo); }
        if (!string.IsNullOrWhiteSpace(search)) { sql += " AND (VentaNumber LIKE @like OR ClienteNombre LIKE @like)"; cmd.Parameters.AddWithValue("@like", $"%{search.Trim()}%"); }
        sql += " ORDER BY Fecha DESC";
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Venta
            {
                Id = r.GetInt64(0),
                VentaNumber = r.GetString(1),
                Fecha = DateTime.Parse(r.GetString(2)),
                Subtotal = r.GetDecimal(3),
                Descuento = r.GetDecimal(4),
                Total = r.GetDecimal(5),
                MetodoPago = r.IsDBNull(6) ? "" : r.GetString(6),
                ClienteId = r.IsDBNull(7) ? null : r.GetInt64(7),
                ClienteNombre = r.IsDBNull(8) ? "" : r.GetString(8)
            });
        return list;
    }

    /// <summary>
    /// Ventas del rango con TODAS sus líneas (una tupla por línea vendida), para
    /// generar los reportes/cierres detallados. Replica la v2.x usando un JOIN
    /// venta-detalle y la misma regla de método con LIKE %...% para tolerar
    /// variantes de texto. Ordena por fecha descendente y desempata por Id.
    /// </summary>
    public List<(Venta venta, VentaDetalle linea)> GetVentasConLineas(DateTime from, DateTime to, string? metodo = null)
    {
        var list = new List<(Venta, VentaDetalle)>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // JOIN cabecera-línea: una fila por producto vendido, que repite los
        // datos de la venta en cada línea; así el reporte puede mostrar el
        // detalle por producto y a la vez totales por venta.
        var sql = @"SELECT v.Id, v.VentaNumber, v.Fecha, v.Subtotal, v.Descuento, v.Total, v.MetodoPago, v.ClienteId, v.ClienteNombre,
                            d.Id, d.VentaId, d.ProductoId, d.NombreProducto, d.PrecioVenta, d.Cantidad
                     FROM Ventas v JOIN VentaDetalles d ON d.VentaId = v.Id
                     WHERE v.Fecha>=@from AND v.Fecha<=@to";
        if (!string.IsNullOrWhiteSpace(metodo)) { sql += " AND v.MetodoPago LIKE @metodo"; cmd.Parameters.AddWithValue("@metodo", $"%{metodo.Trim()}%"); }
        sql += " ORDER BY v.Fecha DESC, v.Id DESC";
        cmd.Parameters.AddWithValue("@from", from.Date.ToString("o"));
        cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1).AddSeconds(-1).ToString("o"));
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var venta = new Venta
            {
                Id = r.GetInt64(0),
                VentaNumber = r.GetString(1),
                Fecha = DateTime.Parse(r.GetString(2)),
                Subtotal = r.GetDecimal(3),
                Descuento = r.GetDecimal(4),
                Total = r.GetDecimal(5),
                MetodoPago = r.IsDBNull(6) ? "" : r.GetString(6),
                ClienteId = r.IsDBNull(7) ? null : r.GetInt64(7),
                ClienteNombre = r.IsDBNull(8) ? "" : r.GetString(8)
            };
            var linea = new VentaDetalle
            {
                Id = r.GetInt64(9),
                VentaId = r.GetInt64(10),
                ProductoId = r.GetInt64(11),
                ProductoNombre = r.IsDBNull(12) ? "" : r.GetString(12),
                PrecioUnitario = r.GetDecimal(13),
                Cantidad = r.GetInt32(14)
            };
            list.Add((venta, linea));
        }
        return list;
    }

    /// <summary>
    /// Líneas de detalle de una venta concreta, en orden de inserción: alimenta la
    /// reimpresión/exportación de la factura del historial (misma foto congelada de
    /// nombre, precio y cantidad que tenía al momento de facturar).
    /// </summary>
    public List<VentaDetalle> GetVentaConLineas(long ventaId)
    {
        var list = new List<VentaDetalle>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id, VentaId, ProductoId, NombreProducto, PrecioVenta, Cantidad FROM VentaDetalles WHERE VentaId=@vi ORDER BY Id";
        cmd.Parameters.AddWithValue("@vi", ventaId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new VentaDetalle
            {
                Id = r.GetInt64(0),
                VentaId = r.GetInt64(1),
                ProductoId = r.GetInt64(2),
                ProductoNombre = r.IsDBNull(3) ? "" : r.GetString(3),
                PrecioUnitario = r.GetDecimal(4),
                Cantidad = r.GetInt32(5)
            });
        }
        return list;
    }
}
