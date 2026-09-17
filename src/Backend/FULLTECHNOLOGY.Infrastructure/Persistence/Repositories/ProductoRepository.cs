// ProductoRepository.cs — CRUD de productos, auto-generación de código y ajuste de stock.
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a la tabla Productos replicando 1:1 la v2.x (Database.cs).
/// Reglas de negocio replicadas: el código del producto se auto-genera con el
/// patrón PRD-año-NNNNN cuando se crea sin especificar (NextProductoCodigo) y
/// luego queda inmutable (el UPDATE no lo toca); el filtro por Tipo es exacto y
/// la búsqueda libre es LIKE sobre Nombre/Codigo/Proveedor; el stock se
/// descuenta al registrar ventas con UpdateProductoStock / el UPDATE de la venta.
/// </summary>
public sealed class ProductoRepository : IProductoRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ProductoRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public List<Producto> GetProductos(string? tipo = null, string? search = null)
    {
        var list = new List<Producto>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // Mismo patrón "WHERE 1=1 + concatenar filtros" de la v2.x: Tipo con
        // igualdad exacta y búsqueda libre con LIKE sobre Nombre/Codigo/Proveedor.
        // La lista sale siempre ordenada por Nombre para listados estables.
        var sql = "SELECT * FROM Productos WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(tipo)) { sql += " AND Tipo=@tipo"; cmd.Parameters.AddWithValue("@tipo", tipo); }
        if (!string.IsNullOrWhiteSpace(search)) { sql += " AND (Nombre LIKE @like OR Codigo LIKE @like OR Proveedor LIKE @like)"; cmd.Parameters.AddWithValue("@like", $"%{search.Trim()}%"); }
        sql += " ORDER BY Nombre";
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(ReadProducto(r));
        return list;
    }

    /// <summary>Consulta un producto por su Id; devuelve null si no existe.</summary>
    public Producto? GetProducto(long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM Productos WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadProducto(r) : null;
    }

    /// <summary>Inserta o actualiza un producto según su Id (0 = nuevo) y devuelve el Id; autogenera el código al insertar.</summary>
    public long SaveProducto(Producto p)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        if (p.Id == 0)
        {
            // Producto nuevo: si no trae código se fabrica uno automático
            // (PRD-año-NNNNN). El código se asigna UNA sola vez en la inserción;
            // el UPDATE de la rama else no lo modifica, por lo que un código ya
            // repartido nunca cambia aunque se edite el resto del producto.
            p.Codigo = string.IsNullOrWhiteSpace(p.Codigo) ? NextProductoCodigo(c) : p.Codigo;
            cmd.CommandText = @"INSERT INTO Productos (Codigo,Nombre,Tipo,Costo,PrecioVenta,Proveedor,FechaIngreso,Garantia,Ubicado,Stock) VALUES (@Codigo,@Nombre,@Tipo,@Costo,@PrecioVenta,@Proveedor,@FechaIngreso,@Garantia,@Ubicado,@Stock);";
            AddProducto(cmd, p);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT last_insert_rowid();";
            p.Id = Convert.ToInt64(cmd.ExecuteScalar());
        }
        else
        {
            cmd.CommandText = @"UPDATE Productos SET Nombre=@Nombre,Tipo=@Tipo,Costo=@Costo,PrecioVenta=@PrecioVenta,Proveedor=@Proveedor,FechaIngreso=@FechaIngreso,Garantia=@Garantia,Ubicado=@Ubicado,Stock=@Stock WHERE Id=@Id;";
            cmd.Parameters.AddWithValue("@Id", p.Id);
            AddProducto(cmd, p);
            cmd.ExecuteNonQuery();
        }
        return p.Id;
    }

    /// <summary>Borra físicamente un producto. El llamador debe comprobar antes que no esté en uso en ventas.</summary>
    public void DeleteProducto(long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Productos WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Establece el stock de un producto. Usado para corregir existencias a mano
    /// y también como apoyo al descuento de stock al vender. Almacena el valor
    /// ya calculado por quien llama (nunca aplica la diferencia aquí).
    /// </summary>
    public void UpdateProductoStock(long id, int newStock)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE Productos SET Stock=@s WHERE Id=@id";
        cmd.Parameters.AddWithValue("@s", newStock);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Genera el código del siguiente producto como PRD-<año>-<secuencial de 5
    /// dígitos>. Replica exactamente la v2.x: el secuencial es COUNT(*) de la
    /// tabla + 1 (no el MAX), por lo que tras un borrado puede reutilizar un
    /// número; el código conserva la unicidad en la práctica por el sufijo del
    /// año corriente. Ejemplo: PRD-2026-00012.
    /// </summary>
    private static string NextProductoCodigo(SqliteConnection c)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Productos";
        var n = Convert.ToInt64(cmd.ExecuteScalar()) + 1;
        return $"PRD-{DateTime.Now:yyyy}-{n:00000}";
    }

    /// <summary>
    /// Carga todos los parámetros del producto para INSERT. Los campos de texto
    /// opcionales se guardan como NULL si vienen vacíos (misma convención que en
    /// Clientes), mientras que los numéricos y la fecha siempre se envían con
    /// valor (los REAL/NULL de SQLite los interpreta coherentemente).
    /// </summary>
    private static void AddProducto(SqliteCommand cmd, Producto p)
    {
        foreach (var x in new[]{
            ("@Codigo",(object?)p.Codigo),("@Nombre",(object?)p.Nombre),("@Tipo",(object?)p.Tipo),
            ("@Proveedor",(object?)p.Proveedor),("@Garantia",(object?)p.Garantia),("@Ubicado",(object?)p.Ubicado)})
            cmd.Parameters.AddWithValue(x.Item1, x.Item2 ?? DBNull.Value);
        foreach (var x in new[]{
            ("@Costo",(object)p.Costo),("@PrecioVenta",(object)p.PrecioVenta),("@Stock",(object)p.Stock),
            ("@FechaIngreso",(object)p.FechaIngreso.ToString("o"))})
            cmd.Parameters.AddWithValue(x.Item1, x.Item2);
    }

    /// <summary>Convierte la fila actual del lector a una entidad Producto (columnas en orden posicional fijo).</summary>
    private static Producto ReadProducto(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Codigo = r.GetString(1),
        Nombre = r.GetString(2),
        Tipo = r.GetString(3),
        Costo = r.GetDecimal(4),
        PrecioVenta = r.GetDecimal(5),
        Proveedor = r.IsDBNull(6) ? "" : r.GetString(6),
        FechaIngreso = DateTime.Parse(r.GetString(7)),
        Garantia = r.IsDBNull(8) ? "" : r.GetString(8),
        Ubicado = r.IsDBNull(9) ? "" : r.GetString(9),
        Stock = r.GetInt32(10)
    };
}