// InicioViewModel.cs — Panel Inicio (v3.5): notificaciones de stock por agotarse y de órdenes estancadas en el tiempo.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.Inicio;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// INICIO (v3.5). Reemplaza al placeholder: espacio de alertas.
// Cuatro cards cliqueables (Accesorios, Repuestos, En proceso y
// Listas sin entregar) que al hacer clic abren una ventana
// flotante con los ítems concretos. Los umbrales los define la
// Configuración (AlertsSettingsService) y el cálculo lo resuelve
// DashboardService.
// ============================================================
public partial class InicioViewModel : ViewModelBase, IRefreshable
{
    private readonly DashboardService _dashboard;
    private readonly AlertsSettingsService _alerts;
    private readonly IDialogService _dialogs;

    public string Subtitle => "Notificaciones de stock por agotarse y órdenes que llevan tiempo sin avanzar.";

    public ObservableCollection<InicioCardViewModel> Cards { get; } = new();

    // Última carga de alertas (se usa al abrir las ventanas flotantes).
    private IReadOnlyList<StockAlertItem> _stock = Array.Empty<StockAlertItem>();
    private IReadOnlyList<TimeAlertItem> _proceso = Array.Empty<TimeAlertItem>();
    private IReadOnlyList<TimeAlertItem> _entrega = Array.Empty<TimeAlertItem>();

    /// <summary>True cuando no hay ninguna alerta activa (estado "todo en orden").</summary>
    [ObservableProperty]
    public partial bool IsClear { get; set; } = true;

    [ObservableProperty]
    public partial string SummaryText { get; set; } = "";

    /// <summary>Glifo del estado general (✅ con alertas limpias / ⚠ con pendientes).</summary>
    [ObservableProperty]
    public partial string StateGlyph { get; set; } = "\u2705";

    /// <summary>Título del estado general.</summary>
    [ObservableProperty]
    public partial string StateTitle { get; set; } = "Todo en orden";

    /// <summary>Token de color del estado general (StatusSuccess / StatusWarning).</summary>
    [ObservableProperty]
    public partial string StateToken { get; set; } = "StatusSuccess";

    public InicioViewModel(DashboardService dashboard, AlertsSettingsService alerts, IDialogService dialogs)
    {
        _dashboard = dashboard;
        _alerts = alerts;
        _dialogs = dialogs;

        Cards.Add(new InicioCardViewModel("Accesorios por agotarse", "\uD83D\uDCE6", "StatusWarning", () => VerAccesoriosCommand.Execute(null)));
        Cards.Add(new InicioCardViewModel("Repuestos por agotarse", "\uD83D\uDD27", "StatusTeal", () => VerRepuestosCommand.Execute(null)));
        Cards.Add(new InicioCardViewModel("En proceso (recibido → listo)", "\u23F1\uFE0F", "StatusInfo", () => VerProcesoCommand.Execute(null)));
        Cards.Add(new InicioCardViewModel("Listos sin entregar (listo → entregado)", "\uD83D\uDCE4", "StatusPurple", () => VerEntregaCommand.Execute(null)));
    }

    public void Refresh() => Load();

    private void Load()
    {
        _stock = _dashboard.GetLowStockAlerts();
        _proceso = _dashboard.GetProcessAlerts();
        _entrega = _dashboard.GetPickupAlerts();

        var acc = _stock.Count(i => i.Tipo == ProductTypes.Accesorio);
        var rep = _stock.Count(i => i.Tipo == ProductTypes.Repuesto);
        Cards[0].Value = acc.ToString();
        Cards[1].Value = rep.ToString();
        Cards[2].Value = _proceso.Count.ToString();
        Cards[3].Value = _entrega.Count.ToString();

        var total = acc + rep + _proceso.Count + _entrega.Count;
        IsClear = total == 0;
        StateGlyph = IsClear ? "\u2705" : "\u26A0\uFE0F";
        StateTitle = IsClear ? "Todo en orden" : "Hay notificaciones pendientes";
        StateToken = IsClear ? "StatusSuccess" : "StatusWarning";
        SummaryText = IsClear
            ? "Todo en orden: no hay productos por agotarse ni órdenes estancadas."
            : $"Tienes {total} notificación{(total == 1 ? "" : "es")} pendiente{(total == 1 ? "" : "s")}. Haz clic en una tarjeta para ver el detalle.";
    }

    // --- Apertura de ventanas flotantes (el umbral configurado se muestra como contexto) ---

    [RelayCommand]
    private Task VerAccesoriosAsync() => OpenListAsync(
        "Accesorios por agotarse",
        $"Cantidad mínima configurada: {_alerts.StockAccesorioUmbral} unidades.",
        _stock
            .Where(i => i.Tipo == ProductTypes.Accesorio)
            .Select(i => new AlertRowItem("\uD83D\uDCE6",
                $"{i.Producto.Nombre} · {i.Producto.Codigo}",
                $"Stock: {i.Producto.Stock} · Umbral: {i.Umbral} unidades"))
            .ToList());

    [RelayCommand]
    private Task VerRepuestosAsync() => OpenListAsync(
        "Repuestos por agotarse",
        $"Cantidad mínima configurada: {_alerts.StockRepuestoUmbral} unidades.",
        _stock
            .Where(i => i.Tipo == ProductTypes.Repuesto)
            .Select(i => new AlertRowItem("\uD83D\uDD27",
                $"{i.Producto.Nombre} · {i.Producto.Codigo}",
                $"Stock: {i.Producto.Stock} · Umbral: {i.Umbral} unidades"))
            .ToList());

    [RelayCommand]
    private Task VerProcesoAsync() => OpenListAsync(
        "Órdenes en proceso (recibido → listo)",
        $"Alerta después de {_alerts.DiasRecibidoAListo} días sin pasar a listo.",
        _proceso
            .Select(i => new AlertRowItem("\u23F1\uFE0F",
                $"{i.Order.OrderNumber} · {i.Order.CustomerName}",
                $"{i.Order.Brand} {i.Order.Model}".Trim() + $" · {i.Order.Status} · {i.Dias} días"))
            .ToList());

    [RelayCommand]
    private Task VerEntregaAsync() => OpenListAsync(
        "Órdenes listas sin entregar (listo → entregado)",
        $"Alerta después de {_alerts.DiasListoAEntregado} días sin entregar.",
        _entrega
            .Select(i => new AlertRowItem("\uD83D\uDCE4",
                $"{i.Order.OrderNumber} · {i.Order.CustomerName}",
                $"{i.Order.Brand} {i.Order.Model}".Trim() + $" · {i.Order.Status} · {i.Dias} días"))
            .ToList());

    private Task OpenListAsync(string title, string subtitle, IReadOnlyList<AlertRowItem> rows) =>
        _dialogs.ShowListAsync(title, subtitle, rows);
}