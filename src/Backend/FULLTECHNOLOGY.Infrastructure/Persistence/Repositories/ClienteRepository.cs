// ClienteRepository.cs — CRUD de clientes, búsqueda del comprador por documento/celular y verificación de uso.
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a la tabla Clientes replicando 1:1 el CRUD de la v2.x (Database.cs).
/// Particularidades replicadas: la búsqueda por Tipo es comparación exacta y la
/// de texto libre es LIKE %...% sobre Nombre/Documento/Celular; los campos en
/// blanco se guardan como NULL (no como cadena vacía); el cliente solo puede
/// borrarse si no está referenciado por órdenes o ventas (ClienteEnUso).
/// </summary>
public sealed class ClienteRepository : IClienteRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ClienteRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public List<Cliente> GetClientes(string? tipo = null, string? search = null)
    {
        var list = new List<Cliente>();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        // WHERE 1=1 es el "ancla" que permite ir concatenando filtros opcionales
        // sin reescribir la consulta base (mismo truco que en la v2.x). El tipo
        // se filtra con igualdad exacta y la búsqueda libre con LIKE sobre
        // Nombre/Documento/Celular, terminando siempre ordenado por Nombre.
        var sql = "SELECT * FROM Clientes WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(tipo)) sql += " AND Tipo=@tipo";
        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += @" AND (Nombre LIKE @like OR Documento LIKE @like OR Celular LIKE @like)";
            cmd.Parameters.AddWithValue("@like", $"%{search.Trim()}%");
        }
        sql += " ORDER BY Nombre";
        cmd.CommandText = sql;
        if (!string.IsNullOrWhiteSpace(tipo)) { cmd.Parameters.AddWithValue("@tipo", tipo); }

        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(ReadCliente(r));
        return list;
    }

    /// <summary>Consulta un cliente por su Id; devuelve null si no existe.</summary>
    public Cliente? GetClienteById(long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM Clientes WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadCliente(r) : null;
    }

    /// <summary>
    /// Busca el cliente "comprador" que la pantalla de nueva venta reconocerá:
    /// debe cumplir Tipo='Cliente comprador' y coincidir EXACTO en Documento O
    /// Celular (sin LIKE) para identificar sin ambigüedad a una misma persona
    /// aunque varie la forma en que se escriben nombre/apellido. LIMIT 1 evita
    /// duplicados si existieran varios registros con el mismo dato.
    /// </summary>
    public Cliente? FindClienteComprador(string docOrCel)
    {
        var v = docOrCel.Trim();
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM Clientes WHERE (Documento=@v OR Celular=@v) AND Tipo='Cliente comprador' LIMIT 1";
        cmd.Parameters.AddWithValue("@v", v);
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadCliente(r) : null;
    }

    /// <summary>Inserta o actualiza un cliente según su Id (0 = nuevo) y devuelve el Id vigente.</summary>
    public long SaveCliente(Cliente c0)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        if (c0.Id == 0)
        {
            // Cliente nuevo: se inserta y luego se recupera el Id autogenerado
            // con last_insert_rowid() para devolverlo a la capa de aplicación.
            cmd.CommandText = @"INSERT INTO Clientes (Tipo,Nombre,Documento,Celular,Direccion,Web,RedSocial) VALUES (@Tipo,@Nombre,@Documento,@Celular,@Direccion,@Web,@RedSocial);";
            AddCliente(cmd, c0);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT last_insert_rowid();";
            c0.Id = Convert.ToInt64(cmd.ExecuteScalar());
        }
        else
        {
            // Cliente existente: actualización completa por Id. El Id nunca se
            // regresa, es la identidad permanente del registro.
            cmd.CommandText = @"UPDATE Clientes SET Tipo=@Tipo,Nombre=@Nombre,Documento=@Documento,Celular=@Celular,Direccion=@Direccion,Web=@Web,RedSocial=@RedSocial WHERE Id=@Id;";
            cmd.Parameters.AddWithValue("@Id", c0.Id);
            AddCliente(cmd, c0);
            cmd.ExecuteNonQuery();
        }
        return c0.Id;
    }

    /// <summary>Borra físicamente un cliente. El llamador debe comprobar antes ClienteEnUso.</summary>
    public void DeleteCliente(long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM Clientes WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Indica si el cliente está referenciado por alguna orden de servicio o por
    /// alguna venta (integridad referencial que la v2.x verifica de igual forma
    /// antes de permitir el borrado). Venta sin cliente se guarda con ClienteId
    /// NULL, por lo que nunca cuenta como "en uso" aunque la venta exista.
    /// </summary>
    public bool ClienteEnUso(long id) =>
        Count("ServiceOrders", "ClienteId", id) > 0 || Count("Ventas", "ClienteId", id) > 0;

    /// <summary>Cuenta registros de una tabla donde la columna indicada vale id.</summary>
    private long Count(string table, string col, long id)
    {
        using var c = _factory.CreateConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table} WHERE {col}=@id";
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Carga en el comando los parámetros del cliente. Los campos descriptivos
    /// se envían NullIfBlank: un campo vacío o de solo espacios se guarda como
    /// NULL, conservando la semántica de la v2.x (donde el espacio en blanco
    /// duplicado provocaba choques UNIQUE antes de la migración).
    /// </summary>
    private static void AddCliente(SqliteCommand cmd, Cliente cl)
    {
        foreach (var p in new[]{
            ("@Tipo",(object?)cl.Tipo),("@Nombre",(object?)cl.Nombre),
            ("@Documento",NullIfBlank(cl.Documento)),("@Celular",NullIfBlank(cl.Celular)),
            ("@Direccion",NullIfBlank(cl.Direccion)),("@Web",NullIfBlank(cl.Web)),
            ("@RedSocial",NullIfBlank(cl.RedSocial))})
            cmd.Parameters.AddWithValue(p.Item1, p.Item2 ?? DBNull.Value);
    }

    private static object? NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>Convierte la fila actual del lector a una entidad Cliente (columnas en orden posicional fijo).</summary>
    private static Cliente ReadCliente(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Tipo = r.GetString(1),
        Nombre = r.GetString(2),
        Documento = r.IsDBNull(3) ? "" : r.GetString(3),
        Celular = r.IsDBNull(4) ? "" : r.GetString(4),
        Direccion = r.IsDBNull(5) ? "" : r.GetString(5),
        Web = r.IsDBNull(6) ? "" : r.GetString(6),
        RedSocial = r.IsDBNull(7) ? "" : r.GetString(7)
    };
}