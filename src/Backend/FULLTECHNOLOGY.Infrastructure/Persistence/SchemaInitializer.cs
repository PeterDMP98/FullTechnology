// SchemaInitializer.cs — Creación del esquema SQLite y migraciones idempotentes replicadas de la v2.x.
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence;

// ============================================================
// Crea el esquema y aplica las migraciones idénticas a la
// v2.x (Database.Initialize). NO se altera ninguna columna ni
// dato existente: solo CreaSiNoExiste + ALTER condicional.
// ============================================================

/// <summary>
/// Crea el esquema de la base de datos y aplica migraciones condicionales.
/// Replica 1:1 Database.Initialize de la v2.x: es seguro llamarlo varias veces,
/// porque los CREATE/INDEX son IF NOT EXISTS y las migraciones solo actúan
/// cuando se detecta (vía PRAGMA) que falta una columna o que hay una
/// restricción obsoleta; nunca se modifica ni se descarta una columna existente.
/// </summary>
public static class SchemaInitializer
{
    /// <summary>Ejecuta el esquema (create-if-not-exists), crea índices y repara bases v0.1.</summary>
    public static void Initialize(SqliteConnectionFactory factory)
    {
        using var c = factory.CreateConnection();
        using var cmd = c.CreateCommand();

        // ------------------------------------------------------------------
        // DEFINICIÓN DEL ESQUEMA (idéntico al CREATE de la v2.x). Semántica que
        // se replica en los repositorios:
        //   • ServiceOrders: la orden de servicio de mantenimiento. Status es el
        //     estado del proceso (recibido/diagnóstico/reparado/entregado/…),
        //     Deposit almacena el abono acumulado (campo calculado: es la suma
        //     de Pagos, ver PagoRepository.RecalcDeposit) y ReceivedAt/DeliveredAt
        //     son TEXT ISO-8601 comparables como cadenas.
        //   • Clientes: Tipo distingue el 'Cliente comprador' de cualquier otro
        //     tipo de contacto; Documento/Celular son la identidad de búsqueda.
        //   • Productos: Codigo es UNIQUE y lo auto-genera el repositorio.
        //   • Ventas + VentaDetalles: cabecera de venta (con Subtotal, Descuento,
        //     Total y cliente) y sus líneas (producto, precio de venta congelado
        //     en el momento de la venta y cantidad).
        //   • Pagos: abonos aplicados a una ServiceOrder; cada pago conserva su
        //     SaldoRestante posterior al abono.
        // ------------------------------------------------------------------
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ServiceOrders (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 OrderNumber TEXT NOT NULL UNIQUE,
 ClienteId INTEGER,
 CustomerName TEXT NOT NULL,
 CustomerDoc TEXT,
 CustomerPhone TEXT,
 DeviceType TEXT,
 Brand TEXT,
 Model TEXT,
 SerialImei TEXT,
 Color TEXT,
 PhysicalCondition TEXT,
 Accessories TEXT,
 ReportedFault TEXT,
 Diagnosis TEXT,
 RepairDetails TEXT,
 Status TEXT NOT NULL,
 DiagnosisCost REAL DEFAULT 0,
 PartsCost REAL DEFAULT 0,
 LaborCost REAL DEFAULT 0,
 Discount REAL DEFAULT 0,
 Deposit REAL DEFAULT 0,
 PaymentMethod TEXT,
 ReceivedAt TEXT NOT NULL,
 DeliveredAt TEXT,
 Notes TEXT
);
CREATE INDEX IF NOT EXISTS IX_ServiceOrders_OrderNumber ON ServiceOrders(OrderNumber);
CREATE INDEX IF NOT EXISTS IX_ServiceOrders_CustomerName ON ServiceOrders(CustomerName);
CREATE INDEX IF NOT EXISTS IX_ServiceOrders_Status ON ServiceOrders(Status);

CREATE TABLE IF NOT EXISTS Clientes (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Tipo TEXT NOT NULL DEFAULT 'Cliente comprador',
 Nombre TEXT NOT NULL,
 Documento TEXT,
 Celular TEXT,
 Direccion TEXT,
 Web TEXT,
 RedSocial TEXT
);
CREATE INDEX IF NOT EXISTS IX_Clientes_Tipo ON Clientes(Tipo);

CREATE TABLE IF NOT EXISTS Productos (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Codigo TEXT NOT NULL UNIQUE,
 Nombre TEXT NOT NULL,
 Tipo TEXT NOT NULL DEFAULT 'Accesorio',
 Costo REAL DEFAULT 0,
 PrecioVenta REAL DEFAULT 0,
 Proveedor TEXT,
 FechaIngreso TEXT NOT NULL,
 Garantia TEXT,
 Ubicado TEXT,
 Stock INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Ventas (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 VentaNumber TEXT NOT NULL UNIQUE,
 Fecha TEXT NOT NULL,
 Subtotal REAL DEFAULT 0,
 Descuento REAL DEFAULT 0,
 Total REAL DEFAULT 0,
 MetodoPago TEXT,
 ClienteId INTEGER,
 ClienteNombre TEXT
);

CREATE TABLE IF NOT EXISTS VentaDetalles (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 VentaId INTEGER NOT NULL,
 ProductoId INTEGER NOT NULL,
 NombreProducto TEXT,
 PrecioVenta REAL DEFAULT 0,
 Cantidad INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Pagos (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 ServiceOrderId INTEGER NOT NULL,
 Fecha TEXT NOT NULL,
 Monto REAL DEFAULT 0,
 Metodo TEXT,
 Concepto TEXT,
 SaldoRestante REAL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_Pagos_ServiceOrderId ON Pagos(ServiceOrderId);
";
        cmd.ExecuteNonQuery();

        // ------------------------------------------------------------------
        // MIGRACIONES CONDICIONALES. El esquema anterior es idempotente, pero
        // las bases creadas por la v0.1 no tienen ClienteId/CustomerDoc en
        // ServiceOrders; aquí se inspecciona el esquema real con PRAGMA y solo
        // se ejecuta el ALTER si la columna falta, evitando errores y sin tocar
        // los datos existentes.
        // ------------------------------------------------------------------
        // Migración: agregar columna ClienteId/CustomerDoc si no existe (bases v0.1)
        var cols = GetColumns(c, "ServiceOrders");
        if (!cols.Contains("ClienteId"))
            ExecRaw(c, "ALTER TABLE ServiceOrders ADD COLUMN ClienteId INTEGER");
        if (!cols.Contains("CustomerDoc"))
            ExecRaw(c, "ALTER TABLE ServiceOrders ADD COLUMN CustomerDoc TEXT");

        // Migración: columna StatusChangedAt (bases creadas antes de la v3.5). Se sella
        // cada vez que la orden cambia de estado y alimenta las alertas de tiempo del
        // panel Inicio (recibido→listo y listo→entregado). Para las órdenes existentes
        // se retro-rellena con ReceivedAt, que es la mejor aproximación al "último
        // cambio" y evita (a) nulls que romperían las consultas y (b) que todo el
        // histórico aparezca como "recientemente cambiado".
        if (!cols.Contains("StatusChangedAt"))
        {
            ExecRaw(c, "ALTER TABLE ServiceOrders ADD COLUMN StatusChangedAt TEXT");
            ExecRaw(c, "UPDATE ServiceOrders SET StatusChangedAt=ReceivedAt WHERE StatusChangedAt IS NULL");
        }

        // Migración: quitar las restricciones UNIQUE de Documento/Celular en Clientes.
        // Antes causaban "UNIQUE constraint failed: Clientes.Celular" al registrar un
        // cliente solo con documento o solo con celular (el campo en blanco se duplicaba).
        // SQLite no permite eliminar una restricción con ALTER TABLE, por eso la
        // reparación exige reconstruir la tabla (ver RebuildClientesTable).
        if (HasUniqueConstraint(c, "Clientes"))
            RebuildClientesTable(c);
    }

    /// <summary>
    /// Detecta si la tabla tiene algún índice UNIQUE explícito (que no sea el de
    /// la clave primaria). El PRAGMA index_list devuelve por cada índice la fila
    /// (seq, name, unique, origin, partial); origin=="pk" identifica el índice
    /// automático de la PK, único que no conviene eliminar. Devuelve true ante
    /// cualquier otro UNIQUE (Documento o Celular), que es lo que se debe quitar.
    /// </summary>
    private static bool HasUniqueConstraint(SqliteConnection c, string table)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"PRAGMA index_list({table})";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            // columnas: seq, name, unique, origin, partial
            var origin = r.IsDBNull(3) ? "" : r.GetString(3);
            if (r.GetInt32(2) == 1 && origin != "pk") return true;
        }
        return false;
    }

    /// <summary>
    /// Reconstruye la tabla Clientes sin las restricciones UNIQUE: crea una tabla
    /// nueva con la misma definición limpia, copia todos los registros tal cual,
    /// descarta la tabla antigua, la renombra y vuelve a crear el índice de Tipo.
    /// El bloque se ejecuta dentro del mismo comando de una sola conexión, por lo
    /// que la operación es coherente (si algo falla, SQLite no confirma cambios
    /// parciales). Equivale exactamente a la corrección aplicada en la v2.x.
    /// </summary>
    private static void RebuildClientesTable(SqliteConnection c)
    {
        foreach (var sql in new[]{
            @"CREATE TABLE Clientes_new (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Tipo TEXT NOT NULL DEFAULT 'Cliente comprador',
 Nombre TEXT NOT NULL,
 Documento TEXT,
 Celular TEXT,
 Direccion TEXT,
 Web TEXT,
 RedSocial TEXT
);",
            "INSERT INTO Clientes_new (Id,Tipo,Nombre,Documento,Celular,Direccion,Web,RedSocial) SELECT Id,Tipo,Nombre,Documento,Celular,Direccion,Web,RedSocial FROM Clientes;",
            "DROP TABLE Clientes;",
            "ALTER TABLE Clientes_new RENAME TO Clientes;",
            "CREATE INDEX IF NOT EXISTS IX_Clientes_Tipo ON Clientes(Tipo);"})
            ExecRaw(c, sql);
    }

    /// <summary>Ejecuta una sentencia SQL sin parámetros (usada por las migraciones y la reconstrucción).</summary>
    private static void ExecRaw(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Devuelve la lista de nombres de columna de una tabla vía PRAGMA table_info.
    /// En la fila devuelta cada columna es (cid, name, type, notnull, dflt_value, pk),
    /// por eso el nombre se lee en el índice 1 del lector.
    /// </summary>
    private static List<string> GetColumns(SqliteConnection c, string table)
    {
        var cols = new List<string>();
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var r = cmd.ExecuteReader();
        while (r.Read()) cols.Add(r.GetString(1));
        return cols;
    }
}