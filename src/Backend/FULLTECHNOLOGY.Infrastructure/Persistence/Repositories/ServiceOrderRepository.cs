// ServiceOrderRepository.cs — CRUD de órdenes de servicio y auto-generación del número de orden OT-año-NNNNN.
using System.Data;
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a la tabla ServiceOrders replicando 1:1 la v2.x (Database.cs), la
/// entidad central del mantenimiento de equipos. El número de orden se genera
/// automáticamente con el patrón OT-año-NNNNN (NextOrderNumber) y no cambia al
/// editar; la búsqueda libre es un LIKE multi-campo y el estado un filtro
/// exacto; al borrar una orden también se borran sus pagos (la integridad se
/// resuelve en código, no con claves foráneas en cascada).
/// </summary>
public sealed class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ServiceOrderRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public List<ServiceOrder> GetAll(string? search = null, string? status = null)
    {
        var list = new List<ServiceOrder>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // Patrón de la v2.x: cada condición viene precedida de un comparador de
        // "desactivado" (@s='' u @status='') que hace que la cláusula completa
        // sea verdadera y no filtre; de ese modo una única consulta parametrizada
        // cubre sin/búsqueda/estado sin reescribir el SQL. El LIKE busca en los
        // campos visibles del listado (número, cliente, teléfono, IMEI, marca,
        // modelo) y el resultado sale de la más reciente a la más antigua.
        cmd.CommandText = @"SELECT * FROM ServiceOrders
WHERE (@s='' OR OrderNumber LIKE @like OR CustomerName LIKE @like OR CustomerPhone LIKE @like OR SerialImei LIKE @like OR Brand LIKE @like OR Model LIKE @like)
AND (@status='' OR Status=@status) ORDER BY Id DESC";
        cmd.Parameters.AddWithValue("@s", search ?? "");
        cmd.Parameters.AddWithValue("@like", $"%{search ?? ""}%");
        cmd.Parameters.AddWithValue("@status", status ?? "");
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(ReadOrder(r));
        return list;
    }

    /// <summary>
    /// Órdenes con saldo pendiente (Balance &gt; 0). Replica la v2.x haciendo el
    /// filtro EN MEMORIA sobre todas las órdenes: la entidad ServiceOrder expone
    /// Balance como la resta de sus costos menos el Deposit acumulado (que ya
    /// mantiene consistente PagoRepository), y aquí solo se conservan las que
    /// siguen debiendo algo. Como el número de órdenes es bajo, cargar todo y
    /// filtrar en objeto es tan aceptable como el original.
    /// </summary>
    public List<ServiceOrder> GetAllPendingBalance()
    {
        var list = new List<ServiceOrder>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM ServiceOrders";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var o = ReadOrder(r);
            if (o.Balance > 0) list.Add(o);
        }
        return list;
    }

    /// <summary>Consulta una orden por su Id interno; devuelve null si no existe.</summary>
    public ServiceOrder? Get(long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM ServiceOrders WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadOrder(r) : null;
    }

    /// <summary>Inserta o actualiza una orden según su Id (0 = nueva) y devuelve el Id; autogenera el número de orden al insertar.</summary>
    public long Save(ServiceOrder o)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        if (o.Id == 0)
        {
            // Orden nueva: se adjudica el siguiente número OT-año-NNNNN antes de
            // insertar; como OrderNumber es UNIQUE en el esquema, el número se
            // asigna una única vez y ya no se modifica en ediciones posteriores.
            o.OrderNumber = NextOrderNumber(c);
            cmd.CommandText = @"INSERT INTO ServiceOrders (OrderNumber,ClienteId,CustomerName,CustomerDoc,CustomerPhone,DeviceType,Brand,Model,SerialImei,Color,PhysicalCondition,Accessories,ReportedFault,Diagnosis,RepairDetails,Status,DiagnosisCost,PartsCost,LaborCost,Discount,Deposit,PaymentMethod,ReceivedAt,DeliveredAt,Notes)
VALUES (@OrderNumber,@ClienteId,@CustomerName,@CustomerDoc,@CustomerPhone,@DeviceType,@Brand,@Model,@SerialImei,@Color,@PhysicalCondition,@Accessories,@ReportedFault,@Diagnosis,@RepairDetails,@Status,@DiagnosisCost,@PartsCost,@LaborCost,@Discount,@Deposit,@PaymentMethod,@ReceivedAt,@DeliveredAt,@Notes);";
            AddOrder(cmd, o);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT last_insert_rowid();";
            o.Id = Convert.ToInt64(cmd.ExecuteScalar());
        }
        else
        {
            // Orden existente: actualización completa por Id. El UPDATE no
            // incluye OrderNumber para que el número ya emitido sea estable.
            cmd.CommandText = @"UPDATE ServiceOrders SET ClienteId=@ClienteId,CustomerName=@CustomerName,CustomerDoc=@CustomerDoc,CustomerPhone=@CustomerPhone,DeviceType=@DeviceType,Brand=@Brand,Model=@Model,SerialImei=@SerialImei,Color=@Color,PhysicalCondition=@PhysicalCondition,Accessories=@Accessories,ReportedFault=@ReportedFault,Diagnosis=@Diagnosis,RepairDetails=@RepairDetails,Status=@Status,DiagnosisCost=@DiagnosisCost,PartsCost=@PartsCost,LaborCost=@LaborCost,Discount=@Discount,Deposit=@Deposit,PaymentMethod=@PaymentMethod,ReceivedAt=@ReceivedAt,DeliveredAt=@DeliveredAt,Notes=@Notes WHERE Id=@Id;";
            cmd.Parameters.AddWithValue("@Id", o.Id);
            AddOrder(cmd, o);
            cmd.ExecuteNonQuery();
        }
        return o.Id;
    }

    /// <summary>
    /// Borra la orden y, acto seguido, TODOS sus pagos. La v2.x no usaba claves
    /// foráneas en cascada: la limpieza se hace aquí explícitamente para no
    /// dejar pagos huérfanos que ensuciarían los reportes contables.
    /// </summary>
    public void Delete(long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM ServiceOrders WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        using var cp = c.CreateCommand();
        cp.CommandText = "DELETE FROM Pagos WHERE ServiceOrderId=@id";
        cp.Parameters.AddWithValue("@id", id);
        cp.ExecuteNonQuery();
    }

    /// <summary>
    /// Genera el número de la siguiente orden de servicio como OT-<año>-<secuencial
    /// de 5 dígitos>. Replica exactamente la v2.x: el secuencial es COUNT(*) de
    /// todas las órdenes + 1 (no el MAX), por lo que tras un borrado puede
    /// reutilizar un número; el prefijo del año mantiene legible el listado.
    /// Ejemplo: OT-2026-00007.
    /// </summary>
    private static string NextOrderNumber(SqliteConnection c)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ServiceOrders";
        var n = Convert.ToInt64(cmd.ExecuteScalar()) + 1;
        return $"OT-{DateTime.Now:yyyy}-{n:00000}";
    }

    /// <summary>
    /// Carga los parámetros de la orden para INSERT/UPDATE. Los campos de texto
    /// opcionales se guardan como NULL si vienen vacíos; las fechas se serializan
    /// en formato ISO-8601 ("o") para que las comparaciones de rango del resto de
    /// repositorios sean correctas como cadenas; los costos/laborales numéricos
    /// siempre se envían con valor.
    /// </summary>
    private static void AddOrder(SqliteCommand cmd, ServiceOrder o)
    {
        foreach (var p in new[]{
            ("@OrderNumber",(object?)o.OrderNumber),("@ClienteId",(object?)o.ClienteId),
            ("@CustomerName",(object?)o.CustomerName),("@CustomerDoc",(object?)o.CustomerDoc),
            ("@CustomerPhone",(object?)o.CustomerPhone),("@DeviceType",(object?)o.DeviceType),
            ("@Brand",(object?)o.Brand),("@Model",(object?)o.Model),("@SerialImei",(object?)o.SerialImei),
            ("@Color",(object?)o.Color),("@PhysicalCondition",(object?)o.PhysicalCondition),
            ("@Accessories",(object?)o.Accessories),("@ReportedFault",(object?)o.ReportedFault),
            ("@Diagnosis",(object?)o.Diagnosis),("@RepairDetails",(object?)o.RepairDetails),
            ("@Status",(object?)o.Status),("@PaymentMethod",(object?)o.PaymentMethod),
            ("@ReceivedAt",(object?)o.ReceivedAt.ToString("o")),("@DeliveredAt",(object?)o.DeliveredAt?.ToString("o")),
            ("@Notes",(object?)o.Notes)})
            cmd.Parameters.AddWithValue(p.Item1, p.Item2 ?? DBNull.Value);
        foreach (var p in new[]{
            ("@DiagnosisCost",(object)o.DiagnosisCost),("@PartsCost",(object)o.PartsCost),
            ("@LaborCost",(object)o.LaborCost),("@Discount",(object)o.Discount),("@Deposit",(object)o.Deposit)})
            cmd.Parameters.AddWithValue(p.Item1, p.Item2);
    }

    /// <summary>
    /// Convierte la fila actual a una entidad ServiceOrder. A diferencia de otros
    /// repositorios, lee por NOMBRE de columna (GetOrdinal), no por posición, lo
    /// que lo hace robusto ante cambios de orden en el esquema; los campos
    /// ausentes/NULL se normalizan (cadena vacía, 0 o null) con ayuda de los
    /// lectores auxiliares siguientes.
    /// </summary>
    internal static ServiceOrder ReadOrder(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        OrderNumber = r.GetString(r.GetOrdinal("OrderNumber")),
        ClienteId = r.IsDBNull(r.GetOrdinal("ClienteId")) ? null : r.GetInt64(r.GetOrdinal("ClienteId")),
        CustomerName = ReadString(r, "CustomerName"),
        CustomerDoc = ReadString(r, "CustomerDoc"),
        CustomerPhone = ReadString(r, "CustomerPhone"),
        DeviceType = ReadString(r, "DeviceType"),
        Brand = ReadString(r, "Brand"),
        Model = ReadString(r, "Model"),
        SerialImei = ReadString(r, "SerialImei"),
        Color = ReadString(r, "Color"),
        PhysicalCondition = ReadString(r, "PhysicalCondition"),
        Accessories = ReadString(r, "Accessories"),
        ReportedFault = ReadString(r, "ReportedFault"),
        Diagnosis = ReadString(r, "Diagnosis"),
        RepairDetails = ReadString(r, "RepairDetails"),
        Status = r.GetString(r.GetOrdinal("Status")),
        DiagnosisCost = ReadDecimal(r, "DiagnosisCost"),
        PartsCost = ReadDecimal(r, "PartsCost"),
        LaborCost = ReadDecimal(r, "LaborCost"),
        Discount = ReadDecimal(r, "Discount"),
        Deposit = ReadDecimal(r, "Deposit"),
        PaymentMethod = ReadString(r, "PaymentMethod"),
        ReceivedAt = DateTime.Parse(ReadString(r, "ReceivedAt")),
        DeliveredAt = ReadNullableDate(r, "DeliveredAt"),
        Notes = ReadString(r, "Notes")
    };

    // Lee el texto de una columna como cadena vacía si viene NULL.
    internal static string ReadString(SqliteDataReader r, string name)
    {
        int i = r.GetOrdinal(name);
        return r.IsDBNull(i) ? "" : r.GetString(i);
    }

    // Lee un valor numérico como 0 si viene NULL.
    internal static decimal ReadDecimal(SqliteDataReader r, string name)
    {
        int i = r.GetOrdinal(name);
        return r.IsDBNull(i) ? 0m : r.GetDecimal(i);
    }

    // Lee una fecha opcional como null si la columna está vacía.
    internal static DateTime? ReadNullableDate(SqliteDataReader r, string name)
    {
        int i = r.GetOrdinal(name);
        return r.IsDBNull(i) ? null : DateTime.Parse(r.GetString(i));
    }
}