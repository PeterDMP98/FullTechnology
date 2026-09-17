// ISettings.cs — Contratos de persistencia de configuración (settings.ini de la v2.x).

namespace FULLTECHNOLOGY.Application.Ports;

// ============================================================
// Persistencia de preferencias: settings.ini en
// %LOCALAPPDATA%\DecoTechnology (compatible con la v2.x).
// ============================================================
/// <summary>Pares clave/valor del settings.ini, compatible con el archivo de la v2.x.</summary>
public interface ISettingsStore
{
    string? Get(string key);
    void Set(string key, string value);
}

/// <summary>Moneda activa; el formato de presentación vive en el value object Domain.Money.</summary>
public interface ICurrencyStore
{
    string GetCurrency();
    void SetCurrency(string currency);
}

/// <summary>Datos de identidad del negocio: nombre de la marca y banderas de PIN / email diario.</summary>
public interface IBusinessInfoStore
{
    string GetBusinessName();
    void SetBusinessName(string name);
    bool PinHabilitado { get; }
    bool EmailDiarioHabilitado { get; }
}