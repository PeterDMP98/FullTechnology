// BusinessSettingsService.cs — Configuración del negocio: nombre, tema y moneda sobre settings.ini.

using FULLTECHNOLOGY.Application.Ports;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Configuración del negocio (Settings en v2): nombre, tema y
// moneda. Lee/escribe settings.ini a través de los stores.
// ============================================================
public class BusinessSettingsService
{
    private readonly IBusinessInfoStore _business;
    private readonly ISettingsStore _settings;
    private readonly CurrencyService _currency;

    public BusinessSettingsService(IBusinessInfoStore business, ISettingsStore settings, CurrencyService currency)
    {
        _business = business;
        _settings = settings;
        _currency = currency;
    }

    // Se lee del store en cada llamada: el nombre puede cambiarlo otra instancia o ventana.
    public string BusinessName => _business.GetBusinessName();

    /// <summary>Notifica cuando cambia el nombre del negocio (para que el shell refresque la marca).</summary>
    public event Action? BusinessNameChanged;

    public void SetBusinessName(string name)
    {
        // Un nombre en blanco restaura la marca por defecto (mismo fallback que el settings del v2).
        _business.SetBusinessName(string.IsNullOrWhiteSpace(name) ? "DECO TECHNOLOGY" : name.Trim());
        BusinessNameChanged?.Invoke();
    }

    /// <summary>'claro' (default) u 'oscuro', definido en settings.ini SoloConfig=true.</summary>
    public string Theme => _settings.Get("Tema") ?? "claro";

    public void SetTheme(string theme)
    {
        // Cualquier valor distinto de "oscuro" se normaliza a "claro": no se persisten temas inválidos.
        _settings.Set("Tema", theme == "oscuro" ? "oscuro" : "claro");
    }

    public string Currency => _currency.Currency;

    // Cambiar la moneda refresca el formato de todos los montos de la UI (delegado en CurrencyService).
    public void SetCurrency(string currency) => _currency.SetCurrency(currency);

    public bool PinHabilitado => _business.PinHabilitado;

    public bool EmailDiarioHabilitado => _business.EmailDiarioHabilitado;
}