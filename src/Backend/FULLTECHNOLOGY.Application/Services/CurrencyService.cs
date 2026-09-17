// CurrencyService.cs — Moneda configurada con caché; el formato puro lo provee Domain.Money.

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.ValueObjects;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Formato de moneda (reemplaza Money.cs). La moneda se persiste
// en settings.ini a través de ICurrencyStore; el formato puro
// vive en el value object Domain Money.
// ============================================================
public class CurrencyService
{
    // Las mismas del value object; se re-exportan para que la UI no dependa de Domain.
    public static string[] KnownCurrencies { get; } = Money.KnownCurrencies;

    private readonly ICurrencyStore _store;
    // Caché bajo lock: la moneda casi nunca cambia pero se lee desde hilos de UI distintos.
    private string? _cache;
    private readonly object _lock = new();

    public CurrencyService(ICurrencyStore store)
    {
        _store = store;
    }

    public string Currency => Cache();

    // Símbolo corto para encabezados e indicadores donde no se muestra una cantidad.
    public string Symbol => new Money(0, Cache()).Symbol;

    /// <summary>Formatea con la moneda configurada (delegado en Domain Money).</summary>
    public string Fmt(decimal value) => Money.Fmt(value, Cache());

    public void SetCurrency(string currency)
    {
        lock (_lock)
        {
            // Se persiste y se refresca la caché en la misma sección crítica: siempre coherentes.
            _store.SetCurrency(currency);
            _cache = currency;
        }
    }

    private string Cache()
    {
        // Lectura única al store: la primera vez se carga del ini y las siguientes se sirve desde memoria.
        lock (_lock) return _cache ??= _store.GetCurrency();
    }
}