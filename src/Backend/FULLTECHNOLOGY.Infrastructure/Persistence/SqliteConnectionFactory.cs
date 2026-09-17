// SqliteConnectionFactory.cs — Ubicación del archivo de datos y creación de conexiones SQLite.
using Microsoft.Data.Sqlite;

namespace FULLTECHNOLOGY.Infrastructure.Persistence;

// ============================================================
// Ubicación y creación de la conexión SQLite. La ruta se
// mantiene idéntica a la v2.x para no perder los datos:
//   %LOCALAPPDATA%\DecoTechnology\DecoTechnology.db
// ============================================================

/// <summary>
/// Fábrica de conexiones SQLite.
/// La ruta del archivo de datos se mantiene EXACTA a la de la v2.x
/// (%LOCALAPPDATA%\DecoTechnology\DecoTechnology.db) para que la migración
/// abra la misma base existente y no cree una copia paralela. Además garantiza
/// que la carpeta contenedora exista antes de que cualquier repositorio
/// intente crear o abrir la BD.
/// </summary>
public class SqliteConnectionFactory
{
    /// <summary>Carpeta de datos de la aplicación (contiene la BD y settings.ini).</summary>
    public string Folder { get; }
    /// <summary>Ruta completa del archivo DecoTechnology.db sobre el que se trabaja.</summary>
    public string DbPath { get; }

    /// <summary>Crea la fábrica contra la carpeta de datos estándar del usuario (LocalApplicationData).</summary>
    public SqliteConnectionFactory()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DecoTechnology"))
    {
    }

    /// <summary>Crea la fábrica contra una carpeta concreta y se asegura de que exista.</summary>
    public SqliteConnectionFactory(string folder)
    {
        Folder = folder;
        DbPath = Path.Combine(folder, "DecoTechnology.db");
        Directory.CreateDirectory(folder);
    }

    /// <summary>
    /// Abre y devuelve una conexión SQLite sobre el archivo de datos.
    /// Se usa aislada: cada repositorio abre su propia conexión, ejecuta su
    /// operación y la libera al salir del bloque using (patrón sin pool de
    /// conexiones, replicando la v2.x que siempre abría/cerraba por operación).
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        return connection;
    }
}