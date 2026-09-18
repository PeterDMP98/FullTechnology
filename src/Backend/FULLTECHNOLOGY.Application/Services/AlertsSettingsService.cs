// AlertsSettingsService.cs — Umbrales configurables de las alertas del panel Inicio (v3.5).

using FULLTECHNOLOGY.Application.Ports;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Umbrales de las alertas del Inicio, persistidos en settings.ini
// (compatible con la v2.x). El panel Inicio decide qué mostrar
// consultando estos valores y la tarjeta de Configuración los
// edita; al guardar se escriben las cuatro claves a la vez.
// Las claves son literales (convención de BusinessSettingsService)
// y tienen su constante gemela en SettingsKeys, en Infrastructure.
// ============================================================
public sealed class AlertsSettingsService
{
    internal const string KeyStockAccesorio = "alertaStockAccesorio";
    internal const string KeyStockRepuesto = "alertaStockRepuesto";
    internal const string KeyDiasRecibidoAListo = "alertaDiasRecibidoAListo";
    internal const string KeyDiasListoAEntregado = "alertaDiasListoAEntregado";

    private readonly ISettingsStore _settings;

    public AlertsSettingsService(ISettingsStore settings)
    {
        _settings = settings;
        StockAccesorioUmbral = ReadInt(KeyStockAccesorio, 3);
        StockRepuestoUmbral = ReadInt(KeyStockRepuesto, 3);
        DiasRecibidoAListo = ReadInt(KeyDiasRecibidoAListo, 15);
        DiasListoAEntregado = ReadInt(KeyDiasListoAEntregado, 20);
    }

    // Unidades mínimas de stock que disparan la alerta de "agotando" por tipo.
    public int StockAccesorioUmbral { get; set; }
    public int StockRepuestoUmbral { get; set; }

    // Días a partir de los cuales una orden "en proceso" (recibido→listo) o
    // "lista sin entregar" (listo→entregado) se marca como alerta de tiempo.
    public int DiasRecibidoAListo { get; set; }
    public int DiasListoAEntregado { get; set; }

    /// <summary>Persiste los cuatro umbrales; un valor menor a 1 restaura el predeterminado.</summary>
    public void Save()
    {
        _settings.Set(KeyStockAccesorio, Sanitize(StockAccesorioUmbral, 3).ToString());
        _settings.Set(KeyStockRepuesto, Sanitize(StockRepuestoUmbral, 3).ToString());
        _settings.Set(KeyDiasRecibidoAListo, Sanitize(DiasRecibidoAListo, 15).ToString());
        _settings.Set(KeyDiasListoAEntregado, Sanitize(DiasListoAEntregado, 20).ToString());
    }

    private int ReadInt(string key, int fallback) =>
        int.TryParse(_settings.Get(key), out var v) && v >= 1 ? v : fallback;

    private static int Sanitize(int value, int fallback) => value >= 1 ? value : fallback;
}