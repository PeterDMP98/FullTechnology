// DatabaseBackupService.cs — Respaldo e importación de la base de datos SQLite mediante copia física del archivo .db.
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Infrastructure.Persistence;

namespace FULLTECHNOLOGY.Infrastructure.Persistence;

// ============================================================
// Respaldo e importación de la base de datos (equivale a
// Database.Backup / Database.Import). La importación siempre
// hace un respaldo previo automático numerado por fecha/hora.
// ============================================================

/// <summary>
/// Respaldo e importación de la base de datos SQLite.
/// Replica 1:1 Database.Backup / Database.Import de la v2.x: el respaldo es una
/// copia física del archivo DecoTechnology.db y la importación siempre se
/// protege con un respaldo previo automático con sello de fecha/hora, de modo
/// que una restauración equivocada nunca destruye el estado vigente.
/// </summary>
public sealed class DatabaseBackupService : IDatabaseBackupService
{
    private readonly SqliteConnectionFactory _factory;

    /// <summary>Recibe la fábrica que define dónde vive el archivo .db y su carpeta de datos.</summary>
    public DatabaseBackupService(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Carpeta de datos de la aplicación (raíz donde se guardan la BD y los respaldos automáticos).</summary>
    public string Folder => _factory.Folder;
    /// <summary>Ruta completa del archivo de base de datos activo.</summary>
    public string DbPath => _factory.DbPath;

    /// <summary>
    /// Copia el archivo de BD activo al destino indicado.
    /// Crea los directorios intermedios si faltan y sobrescribe el destino:
    /// se usa tanto para el respaldo manual como para exportar la BD.
    /// </summary>
    public void Backup(string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(DbPath, destination, true);
    }

    /// <summary>
    /// Importa una BD externa como la actual. Invariante de seguridad: antes de
    /// sobrescribir se genera un respaldo automático numerado por fecha/hora
    /// (DecoTechnology_backup_yyyyMMdd_HHmmss.db) con la BD vigente, y solo
    /// después se reemplaza el archivo activo por la copia entrante.
    /// Devuelve false sin tocar nada si el archivo de origen no existe.
    /// </summary>
    public bool Import(string sourcePath)
    {
        if (!File.Exists(sourcePath)) return false;
        var backupDest = Path.Combine(Folder, $"DecoTechnology_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        File.Copy(DbPath, backupDest, true);
        File.Copy(sourcePath, DbPath, true);
        return true;
    }
}