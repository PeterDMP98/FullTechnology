// AccountingRepository.cs — Consultas contables: cobros, ventas y series de balance agrupadas por período.
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consultas de contabilidad para el cierre: cobros con su orden de servicio,
/// totales por método de pago, series diarias de ventas/cobros y totales de un
/// período. Replican 1:1 el SQL de la v2.x (Database.cs), incluidas las reglas
/// de clasificación por subcadena (efectivo/nequi/otros) y el uso de fechas
/// como TEXT ISO-8601 con el rango "desde las 00:00 del inicio hasta las
/// 23:59:59 del cierre" (to.Date.AddDays(1).AddSeconds(-1)).
/// </summary>
public sealed class AccountingRepository : IAccountingRepository
{
    private readonly SqliteConnectionFactory _factory;

    public AccountingRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public List<(Pago pago, ServiceOrder orden)> GetCobrosConOrden(DateTime from, DateTime to, string? metodo = null)
    {
        var list = new List<(Pago, ServiceOrder)>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // Cobros del rango con los datos de su orden de servicio. El LEFT JOIN
        // conserva incluso pagos huérfanos (orden borrada o desvinculada): si no
        // hay orden, los campos de la derecha vienen NULL y el repositorio los
        // convierte a cadena vacía. El filtro de método usa LIKE %...% (no =)
        // para que 'Nequi', 'NEQUI', 'Efectivo' etc. coincidan igual que en
        // la v2.x. Ordena del más reciente al más antiguo (y desempata por Id).
        var sql = @"SELECT p.Id, p.ServiceOrderId, p.Fecha, p.Monto, p.Metodo, p.Concepto, p.SaldoRestante,
                            o.OrderNumber, o.CustomerName, o.DeviceType, o.Brand, o.Model, o.CustomerDoc
                     FROM Pagos p LEFT JOIN ServiceOrders o ON o.Id = p.ServiceOrderId
                     WHERE p.Fecha>=@from AND p.Fecha<=@to";
        if (!string.IsNullOrWhiteSpace(metodo)) { sql += " AND p.Metodo LIKE @metodo"; cmd.Parameters.AddWithValue("@metodo", $"%{metodo.Trim()}%"); }
        sql += " ORDER BY p.Fecha DESC, p.Id DESC";
        cmd.Parameters.AddWithValue("@from", from.Date.ToString("o"));
        cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1).AddSeconds(-1).ToString("o"));
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var pago = new Pago
            {
                Id = r.GetInt64(0),
                ServiceOrderId = r.GetInt64(1),
                Fecha = DateTime.Parse(r.GetString(2)),
                Monto = r.GetDecimal(3),
                Metodo = r.IsDBNull(4) ? "" : r.GetString(4),
                Concepto = r.IsDBNull(5) ? "" : r.GetString(5),
                SaldoRestante = r.GetDecimal(6)
            };
            var orden = new ServiceOrder
            {
                Id = pago.ServiceOrderId,
                OrderNumber = r.IsDBNull(7) ? "" : r.GetString(7),
                CustomerName = r.IsDBNull(8) ? "" : r.GetString(8),
                DeviceType = r.IsDBNull(9) ? "" : r.GetString(9),
                Brand = r.IsDBNull(10) ? "" : r.GetString(10),
                Model = r.IsDBNull(11) ? "" : r.GetString(11),
                CustomerDoc = r.IsDBNull(12) ? "" : r.GetString(12)
            };
            list.Add((pago, orden));
        }
        return list;
    }

    public (decimal efectivo, decimal nequi, decimal otros) VentasPorMetodo(DateTime from, DateTime to)
    {
        var fFrom = from.Date;
        var fTo = to.Date.AddDays(1).AddSeconds(-1);
        decimal ef = 0, nq = 0, otros = 0;
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // Total de ventas del rango agrupado por método de pago (una fila por
        // MétodoPago con su suma). La clasificación final en efectivo/nequi/otros
        // se hace por subcadena en minúsculas, de modo que variantes como
        // "Efectivo", "Pago en efectivo" o "Nequi" caen en la misma columna y
        // cualquier método no reconocido se acumula en "otros".
        cmd.CommandText = "SELECT MetodoPago, SUM(Total) FROM Ventas WHERE Fecha>=@from AND Fecha<=@to GROUP BY MetodoPago";
        cmd.Parameters.AddWithValue("@from", fFrom.ToString("o"));
        cmd.Parameters.AddWithValue("@to", fTo.ToString("o"));
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var m = (r.IsDBNull(0) ? "" : r.GetString(0)).ToLower();
            var t = r.GetDecimal(1);
            if (m.Contains("efectivo")) ef += t;
            else if (m.Contains("nequi")) nq += t;
            else otros += t;
        }
        return (ef, nq, otros);
    }

    public (decimal efectivo, decimal nequi, decimal otros) CobrosPorMetodo(DateTime from, DateTime to)
    {
        var fFrom = from.Date;
        var fTo = to.Date.AddDays(1).AddSeconds(-1);
        decimal ef = 0, nq = 0, otros = 0;
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // Análogo a VentasPorMetodo pero sobre los abonos de mantenimiento: suma
        // los Monto de cada Pago del rango agrupado por su Método, con la misma
        // clasificación por subcadena efectivo/nequi/otros.
        cmd.CommandText = "SELECT Metodo, SUM(Monto) FROM Pagos WHERE Fecha>=@from AND Fecha<=@to GROUP BY Metodo";
        cmd.Parameters.AddWithValue("@from", fFrom.ToString("o"));
        cmd.Parameters.AddWithValue("@to", fTo.ToString("o"));
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var m = (r.IsDBNull(0) ? "" : r.GetString(0)).ToLower();
            var t = r.GetDecimal(1);
            if (m.Contains("efectivo")) ef += t;
            else if (m.Contains("nequi")) nq += t;
            else otros += t;
        }
        return (ef, nq, otros);
    }

    /// <summary>
    /// Serie diaria ventas vs. cobros para graficar el cierre. Invariante clave:
    /// se pre-rellena un día para cada fecha del rango con (0, 0), de modo que
    /// los días sin movimientos aparecen como cero y la serie nunca tiene huecos.
    /// Luego se agregan ventas y cobros por día (GROUP BY date(Fecha)) y se
    /// suman al valor ya existente del día.
    /// </summary>
    public List<(DateTime fecha, decimal ventas, decimal cobros)> BalanceSeries(DateTime from, DateTime to)
    {
        var dic = new SortedDictionary<DateTime, (decimal v, decimal c)>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1)) dic[d] = (0, 0);

        using var conn = _factory.CreateConnection();

        using (var cmd = conn.CreateCommand())
        {
            // Ventas del rango totalizadas por día calendario; junto con el
            // pre-relleno esto produce un registro por cada día del período.
            cmd.CommandText = "SELECT Fecha, SUM(Total) FROM Ventas WHERE Fecha>=@from AND Fecha<=@to GROUP BY date(Fecha)";
            cmd.Parameters.AddWithValue("@from", from.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1).AddSeconds(-1).ToString("o"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var f = DateTime.Parse(r.GetString(0)).Date;
                if (dic.ContainsKey(f)) dic[f] = (dic[f].v + r.GetDecimal(1), dic[f].c);
            }
        }
        using (var cmd = conn.CreateCommand())
        {
            // Igual que arriba, pero sobre los cobros (Pagos) para combinarlos
            // día a día con las ventas en la misma serie.
            cmd.CommandText = "SELECT Fecha, SUM(Monto) FROM Pagos WHERE Fecha>=@from AND Fecha<=@to GROUP BY date(Fecha)";
            cmd.Parameters.AddWithValue("@from", from.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1).AddSeconds(-1).ToString("o"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var f = DateTime.Parse(r.GetString(0)).Date;
                if (dic.ContainsKey(f)) dic[f] = (dic[f].v, dic[f].c + r.GetDecimal(1));
            }
        }
        return dic.Select(x => (x.Key, x.Value.v, x.Value.c)).ToList();
    }

    /// <summary>Total de ventas del período; COALESCE garantiza 0 cuando no hay registros.</summary>
    public decimal TotalVentasPeriodo(DateTime from, DateTime to)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(Total),0) FROM Ventas WHERE Fecha>=@from AND Fecha<=@to";
        cmd.Parameters.AddWithValue("@from", from.Date.ToString("o"));
        cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1).AddSeconds(-1).ToString("o"));
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    /// <summary>Total de cobros (abonos de mantenimiento) del período; COALESCE garantiza 0 sin registros.</summary>
    public decimal TotalCobrosPeriodo(DateTime from, DateTime to)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(Monto),0) FROM Pagos WHERE Fecha>=@from AND Fecha<=@to";
        cmd.Parameters.AddWithValue("@from", from.Date.ToString("o"));
        cmd.Parameters.AddWithValue("@to", to.Date.AddDays(1).AddSeconds(-1).ToString("o"));
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }
}