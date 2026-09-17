// SettingsStore.cs — Persistencia de ajustes en settings.ini y lectores tipados (moneda, negocio) sobre ese archivo.
using FULLTECHNOLOGY.Application.Ports;

namespace FULLTECHNOLOGY.Infrastructure.Settings;

// ============================================================
// settings.ini en %LOCALAPPDATA%\DecoTechnology (mismo archivo
// y mismos pares clave=valor que la v2.x). Preserva las líneas.
// ============================================================

/// <summary>
/// Almacén de configuración en un archivo settings.ini ubicado en la MISMA
/// carpeta y con los MISMOS pares clave=valor que la v2.x, para que los ajustes
/// del WinForms (nombre del negocio, moneda, PIN, envío diario, tema) sobrevivan
/// a la migración. Cada línea del archivo es "clave=valor"; Set reemplaza solo
/// la clave indicada sin tocar las demás líneas y Get busca el prefijo
/// "clave=" exacto. El acceso se serializa con un lock para evitar escrituras
/// concurrentes sobre el archivo desde distintos hilos.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private static readonly object Lock = new();

    /// <summary>Carpeta de datos donde se guarda settings.ini (%LOCALAPPDATA%\DecoTechnology por defecto).</summary>
    public string Folder { get; }

    /// <summary>Crea el almacén sobre la carpeta de datos estándar del usuario (LocalApplicationData).</summary>
    public SettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DecoTechnology"))
    {
    }

    /// <summary>Crea el almacén sobre una carpeta concreta y garantiza que exista para poder escribir el .ini.</summary>
    public SettingsStore(string folder)
    {
        Folder = folder;
        Directory.CreateDirectory(folder);
    }

    /// <summary>Ruta del archivo de configuración flat (clave=valor por línea).</summary>
    private string SettingsPath() => Path.Combine(Folder, "settings.ini");

    /// <summary>Devuelve el valor de una clave, o null si el archivo no existe o la clave no está definida.</summary>
    public string? Get(string key)
    {
        lock (Lock)
        {
            var path = SettingsPath();
            if (!File.Exists(path)) return null;
            foreach (var line in File.ReadAllLines(path))
            {
                // Emparejamiento por prefijo "clave=" (StartsWith ordinal): una
                // clave "pin" no se confunde con "pinHabilitado" porque esta
                // última empieza por "pinHab…", no por "pin=".
                if (line.StartsWith(key + "=", StringComparison.Ordinal))
                    return line.Substring(key.Length + 1).Trim();
            }
            return null;
        }
    }

    /// <summary>
    /// Escribe/actualiza una clave con su valor. Invariante de preservación: se
    /// eliminan SOLO las líneas existentes de esa clave y el resto del archivo
    /// queda intacto (los ajustes de la v2.x y los comentarios se conservan).
    /// Si el archivo no existe aún, se crea con esa única línea.
    /// </summary>
    public void Set(string key, string value)
    {
        lock (Lock)
        {
            var path = SettingsPath();
            var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
            lines.RemoveAll(l => l.StartsWith(key + "=", StringComparison.Ordinal));
            lines.Add($"{key}={value}");
            File.WriteAllLines(path, lines);
        }
    }
}

/// <summary>
/// Lee/escribe la moneda del negocio (clave "currency"). Si está vacía o
/// ausente devuelve COP como predeterminada, replicando el comportamiento por
/// defecto de la v2.x para impresión de precios y cierres.
/// </summary>
public sealed class CurrencyStore : ICurrencyStore
{
    private const string Key = "currency";
    private readonly ISettingsStore _settings;

    public CurrencyStore(ISettingsStore settings)
    {
        _settings = settings;
    }

    public string GetCurrency()
    {
        var v = _settings.Get(Key);
        return string.IsNullOrWhiteSpace(v) ? "COP" : v.Trim();
    }

    public void SetCurrency(string currency) => _settings.Set(Key, currency);
}

/// <summary>
/// Datos configurables del negocio (claves businessName, pinHabilitado y
/// emailDiario) persistidos en settings.ini. La v2.x los tenía en su propio .ini
/// y aquí se mantienen las mismas claves para conservar los valores al migrar.
/// Los booleanos se interpretan como "true" (OrdinalIgnoreCase): cualquier otro
/// valor equivale a false.
/// </summary>
public sealed class BusinessInfoStore : IBusinessInfoStore
{
    private const string BusinessNameKey = "businessName";
    private const string PinKey = "pinHabilitado";
    private const string EmailKey = "emailDiario";
    private readonly ISettingsStore _settings;

    public BusinessInfoStore(ISettingsStore settings)
    {
        _settings = settings;
    }

    public string GetBusinessName()
    {
        var v = _settings.Get(BusinessNameKey);
        return string.IsNullOrWhiteSpace(v) ? "DECO TECHNOLOGY" : v.Trim();
    }

    /// <summary>Guarda el nombre del negocio; un valor vacío restaura el predeterminado "DECO TECHNOLOGY".</summary>
    public void SetBusinessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _settings.Set(BusinessNameKey, "DECO TECHNOLOGY");
            return;
        }
        _settings.Set(BusinessNameKey, name.Trim());
    }

    public bool PinHabilitado => _settings.Get(PinKey)?.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    public bool EmailDiarioHabilitado => _settings.Get(EmailKey)?.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Claves de configuración compartidas entre la capa de aplicación y la de
/// infraestructura. Usar estas constantes (y no string literals sueltos) mantiene
/// el formato settings.ini 100% compatible con la v2.x.
/// </summary>
public static class SettingsKeys
{
    public const string Currency = "currency";
    public const string BusinessName = "businessName";
    public const string PinHabilitado = "pinHabilitado";
    public const string EmailDiario = "emailDiario";
    public const string Tema = "tema";
}