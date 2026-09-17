// Money.cs — Value object de formato de moneda puro (sin I/O); réplica del Money.Fmt del v2.

using System.Globalization;

namespace FULLTECHNOLOGY.Domain.ValueObjects;

// ============================================================
// Value object MONEDERO. Formato de moneda puro (sin I/O):
// recibe la moneda configurada y devuelve el texto formateado.
// La moneda NO se hardcodea: COP es solo el default del sistema.
// La fuente de la moneda actual la aporta Infrastructure
// (CurrencyStore); CurrencyService la inyecta aquí.
// ============================================================
/// <summary>Pareja {cantidad, moneda} con el formato de presentación que consumen la UI y las exportaciones.</summary>
public readonly record struct Money(decimal Amount, string CurrencyCode)
{
    // Las tres culturas dan los formatos del v2: miles COP, símbolo USD y euro con coma europea.
    private static readonly CultureInfo EsCO = new("es-CO");
    private static readonly CultureInfo EnUS = new("en-US");
    private static readonly CultureInfo EsES = new("es-ES");

    // Catálogo de monedas soportadas por el sistema; la app corre por defecto en COP.
    public static readonly string[] KnownCurrencies = { "COP", "USD", "EUR" };

    // Símbolo corto para listados compactos; COP y USD comparten "$" y se distinguen por el sufijo COP.
    public string Symbol => CurrencyCode switch
    {
        "USD" => "$",
        "EUR" => "€",
        _ => "$"
    };

    /// <summary>Formatea una cantidad con la moneda indicada (mismo resultado que Money.Fmt v2).</summary>
    public static string Fmt(decimal amount, string currency)
    {
        // Se normaliza la moneda a mayúsculas para no depender de cómo la digitó el usuario.
        return (currency ?? "").Trim().ToUpperInvariant() switch
        {
            "USD" => amount.ToString("C2", EnUS),
            "EUR" => amount.ToString("C2", EsES),
            // COP (o cualquier otra): miles sin decimales + sufijo literal "COP" (formato histórico).
            _ => amount.ToString("N0", EsCO) + " COP"
        };
    }

    public string Fmt() => Fmt(Amount, CurrencyCode);

    // Cero en la moneda indicada; una moneda vacía/ausente cae a COP (default del sistema).
    public static Money Zero(string currency) => new(0, string.IsNullOrWhiteSpace(currency) ? "COP" : currency);
}