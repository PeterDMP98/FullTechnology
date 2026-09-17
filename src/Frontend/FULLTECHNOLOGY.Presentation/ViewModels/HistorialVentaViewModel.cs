// HistorialVentaViewModel.cs — Registro de ventas con filtros (fechas, medio de pago, búsqueda), total del periodo y detalle por venta.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.HistorialVenta;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// HISTORIAL DE VENTAS. Replica HistorialVentaForm sobre
// SalesService.Search: filtros Desde/Hasta/medio/búsqueda,
// tabla read-only con total del periodo y detalle de factura
// (reemplaza el MessageBox del v2 por IDialogService).
// Usa el patrón de recarga versionado (R29).
// ============================================================
public partial class HistorialVentaViewModel : ViewModelBase, IRefreshable
{
    private readonly SalesService _ventas;
    private readonly CurrencyService _currency;
    private readonly IDialogService _dialogs;

    // Recarga versionada: descarta resultados obsoletos de cargas anteriores.
    private int _version;

    public string Subtitle => "Registro de todas las ventas con filtros y total del periodo.";

    // Medios de pago (índice 0 = "Todos", sin filtro de medio).
    public IReadOnlyList<string> MetodoOptions { get; } = new[]
    {
        "Todos", "Efectivo", "Nequi", "Transferencia", "Tarjeta", "Daviplata"
    };

    // Filtros del historial: periodo (por defecto el último mes) y medio.
    [ObservableProperty]
    public partial DateTimeOffset? FechaDesde { get; set; } = DateTimeOffset.Now.AddMonths(-1);

    [ObservableProperty]
    public partial DateTimeOffset? FechaHasta { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial int MetodoIndex { get; set; }

    // Búsqueda libre (número de factura, cliente, etc.).
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public ObservableCollection<HistorialVentaRowViewModel> Rows { get; } = new();

    // Resumen del periodo mostrado en el encabezado.
    [ObservableProperty]
    public partial string TotalText { get; set; } = "";

    [ObservableProperty]
    public partial string ResumenText { get; set; } = "";

    public HistorialVentaViewModel(SalesService ventas, CurrencyService currency, IDialogService dialogs)
    {
        _ventas = ventas;
        _currency = currency;
        _dialogs = dialogs;
        Refresh();
    }

    public void Refresh() => Reload();

    // Cambiar cualquier filtro recarga la lista al instante.
    partial void OnFechaDesdeChanged(DateTimeOffset? value) => Reload();
    partial void OnFechaHastaChanged(DateTimeOffset? value) => Reload();
    partial void OnMetodoIndexChanged(int value) => Reload();
    partial void OnSearchTextChanged(string value) => Reload();

    // [RelayCommand] genera VerCommand: muestra el detalle de la factura en un diálogo.
    [RelayCommand]
    private async Task VerAsync(HistorialVentaRowViewModel? row)
    {
        if (row is null) return;
        await _dialogs.ShowMessageAsync("Detalle de venta", row.DetalleTexto);
    }

    private void Reload()
    {
        var version = Interlocked.Increment(ref _version);
        IsLoading = true;
        IsEmpty = false;
        IsError = false;
        _ = LoadAsync(version);
    }

    // Carga la búsqueda fuera del hilo de UI; solo aplica resultados si la
    // versión sigue vigente (guarda anti filas duplicadas por recargas rápidas).
    private async Task LoadAsync(int version)
    {
        try
        {
            var from = (FechaDesde ?? DateTimeOffset.Now).Date;
            var to = (FechaHasta ?? DateTimeOffset.Now).Date;
            var metodo = MetodoIndex <= 0 ? "" : MetodoOptions[MetodoIndex];
            var search = SearchText;

            var ventas = await Task.Run(() =>
                _ventas.Search(from, to, string.IsNullOrEmpty(metodo) ? null : metodo, search));
            if (version != _version) return; // sobrevino una recarga más nueva

            // Se rellena la tabla y se acumula el total del periodo.
            Rows.Clear();
            decimal total = 0;
            foreach (var v in ventas)
            {
                Rows.Add(new HistorialVentaRowViewModel(v, _currency, VerCommand));
                total += v.Total;
            }
            IsEmpty = Rows.Count == 0;
            TotalText = $"Total: {_currency.Fmt(total)}";
            ResumenText = $"Ventas: {ventas.Count} · Periodo: {from:dd/MM/yyyy} al {to:dd/MM/yyyy}"
                + (metodo.Length > 0 ? $" · Medio: {metodo}" : "");
            IsLoading = false;
        }
        catch (Exception ex)
        {
            if (version != _version) return;
            Rows.Clear();
            IsLoading = false;
            IsEmpty = false;
            IsError = true;
            ErrorMessage = $"No se pudieron cargar las ventas. Detalle: {ex.Message}";
        }
    }
}