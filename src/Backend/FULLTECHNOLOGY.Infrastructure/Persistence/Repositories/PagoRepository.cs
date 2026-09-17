// PagoRepository.cs — Abonos de mantenimiento: alta/baja de pagos y recálculo del depósito acumulado de la orden.
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;

/// <summary>
/// Gestión de los abonos (Pagos) asociados a una orden de servicio.
/// Regla de negocio central replicada de la v2.x: el campo Deposit de la orden
/// NUNCA se escribe a mano desde la pantalla, sino que se recalcula siempre
/// como la suma de los Monto de sus Pagos (RecalcDeposit). Así, añadir o
/// eliminar cualquier abono deja el depósito de la orden consistente y el
/// saldo pendiente (Balance) de la entidad se obtiene restando ese Deposit.
/// </summary>
public sealed class PagoRepository : IPagoRepository
{
    private readonly SqliteConnectionFactory _factory;

    public PagoRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public List<Pago> GetPagos(long serviceOrderId)
    {
        var list = new List<Pago>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // Historial de abonos de una orden en orden cronológico ascendente
        // (el primer abono primero): así la pantalla muestra la secuencia de
        // pagos tal como se hicieron para validar el saldo restante.
        cmd.CommandText = "SELECT * FROM Pagos WHERE ServiceOrderId=@id ORDER BY Fecha ASC";
        cmd.Parameters.AddWithValue("@id", serviceOrderId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Pago
            {
                Id = r.GetInt64(0),
                ServiceOrderId = r.GetInt64(1),
                Fecha = DateTime.Parse(r.GetString(2)),
                Monto = r.GetDecimal(3),
                Metodo = r.IsDBNull(4) ? "" : r.GetString(4),
                Concepto = r.IsDBNull(5) ? "" : r.GetString(5),
                SaldoRestante = r.GetDecimal(6)
            });
        return list;
    }

    /// <summary>
    /// Registra un nuevo abono y de inmediato actualiza el depósito acumulado
    /// de la orden (la suma de todos sus pagos). Si la operación de recálculo
    /// fallara, el pago quedaría inconsistentemente sin contar; por eso se hace
    /// sobre la misma conexión, antes de devolver el Id.
    /// </summary>
    public long AddPago(Pago p)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO Pagos (ServiceOrderId,Fecha,Monto,Metodo,Concepto,SaldoRestante) VALUES (@so,@f,@m,@me,@c,@s);";
        cmd.Parameters.AddWithValue("@so", p.ServiceOrderId);
        cmd.Parameters.AddWithValue("@f", p.Fecha.ToString("o"));
        cmd.Parameters.AddWithValue("@m", p.Monto);
        cmd.Parameters.AddWithValue("@me", (object?)p.Metodo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)p.Concepto ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@s", p.SaldoRestante);
        cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT last_insert_rowid();";
        var id = Convert.ToInt64(cmd.ExecuteScalar());

        // Actualizar abono acumulado y saldo de la orden
        // El depósito de la orden es la suma de sus pagos; se reconsulta y se
        // escribe en ServiceOrders para que cualquier otra pantalla lea el dato
        // ya consistente sin volver a agregar los pagos (COALESCE => 0 si no
        // quedara ningún pago).
        using var up = c.CreateCommand();
        up.CommandText = "SELECT COALESCE(SUM(Monto),0) FROM Pagos WHERE ServiceOrderId=@id";
        up.Parameters.AddWithValue("@id", p.ServiceOrderId);
        var dep = Convert.ToDecimal(up.ExecuteScalar());
        up.Parameters.Clear();
        up.CommandText = "UPDATE ServiceOrders SET Deposit=@dep WHERE Id=@id";
        up.Parameters.AddWithValue("@dep", dep);
        up.Parameters.AddWithValue("@id", p.ServiceOrderId);
        up.ExecuteNonQuery();
        return id;
    }

    /// <summary>Elimina el abono y vuelve a recalcular el depósito de la orden (puede quedar en 0).</summary>
    public void DeletePago(long pagoId, long serviceOrderId)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Pagos WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", pagoId);
        cmd.ExecuteNonQuery();
        RecalcDeposit(serviceOrderId);
    }

    /// <summary>
    /// Recalcula y persiste el depósito (Deposit) de una orden como la suma de
    /// todos sus pagos. Fuente única de verdad del abono: nunca se escribe el
    /// valor manualmente, sino que deriva del historial de Pagos.
    /// </summary>
    public void RecalcDeposit(long serviceOrderId)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(Monto),0) FROM Pagos WHERE ServiceOrderId=@id";
        cmd.Parameters.AddWithValue("@id", serviceOrderId);
        var dep = Convert.ToDecimal(cmd.ExecuteScalar());
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE ServiceOrders SET Deposit=@dep WHERE Id=@id";
        cmd.Parameters.AddWithValue("@dep", dep);
        cmd.Parameters.AddWithValue("@id", serviceOrderId);
        cmd.ExecuteNonQuery();
    }
}