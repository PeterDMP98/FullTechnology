// AccountingViewModel.cs — Cierre contable del periodo (movimientos por distintas vistas) y gráfica de balance con LiveCharts2.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.Accounting;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// GESTIÓN CONTABLE (F10). Replica ContableView v2 sobre
// AccountingService: cierre del periodo (movimientos / tipo de
// ingreso / método / producto / cliente) con filtros y la
// gráfica de balance (serie diaria de ventas + cobros) usando
// LiveCharts2.
// ============================================================
public partial class AccountingViewModel : ViewModelBase, IRefreshable
{
    private readonly AccountingService _accounting;
    private readonly CurrencyService _currency;

    // Contador de recarga versionado: cada Reload() incrementa _version y la
    // tarea de carga más antigua se descarta al terminar (guarda en LoadAsync),
    // evitando filas duplicadas/obsoletas ante recargas concurrentes del usuario.
    private int _version;

    // Paleta de colores de la gráfica (reutilizada al reconstruir series y ejes).
    private static readonly SKColor VentasColor = new(78, 129, 191);
    private static readonly SKColor CobrosColor = new(88, 165, 92);
    private static readonly SKColor AxisColor = new(138, 138, 138);
    private static readonly SKColor SeparatorColor = new(70, 70, 70);

    public string Subtitle => "Cierres por periodo y balance con gráficas.";

    // Vistas disponibles del cierre: cada una agrupa los movimientos de otra forma.
    public IReadOnlyList<string> VistaOptions { get; } = new[]
    {
        "Facturas (movimientos)", "Tipo de ingreso", "Por método de pago", "Por producto", "Por cliente"
    };

    // Métodos de pago posibles (índice 0 = "Todos", sin filtro).
    public IReadOnlyList<string> MetodoOptions { get; } = new[]
    {
        "Todos", "Efectivo", "Nequi", "Transferencia", "Tarjeta", "Daviplata"
    };

    // Filtros del periodo de cierre (los cambios disparan Reload por OnXxxChanged).
    [ObservableProperty]
    public partial DateTimeOffset? FechaDesde { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial DateTimeOffset? FechaHasta { get; set; } = DateTimeOffset.Now;

    // Vista de agrupación del cierre y método de pago seleccionados.
    [ObservableProperty]
    public partial int VistaIndex { get; set; }

    [ObservableProperty]
    public partial int MetodoIndex { get; set; }

    // Texto de búsqueda libre (filtra la vista actual).
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    // Filas del cierre según la vista elegida (se reconstruye en cada recarga).
    public ObservableCollection<CierreRowViewModel> Rows { get; } = new();

    // Totales del periodo
    [ObservableProperty]
    public partial string VentasText { get; set; } = "";

    [ObservableProperty]
    public partial string CobrosText { get; set; } = "";

    [ObservableProperty]
    public partial string TotalText { get; set; } = "";

    [ObservableProperty]
    public partial string FacturasText { get; set; } = "";

    [ObservableProperty]
    public partial string PeriodoText { get; set; } = "";

    // Catálogo visible (encabezados reutilizados por PDF/Excel)
    [ObservableProperty]
    public partial string[] Columns { get; set; } = Array.Empty<string>();

    public int ColumnCount => Columns.Length;

    [ObservableProperty]
    public partial string[] ExportHeaders { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    public partial float[] ExportWidths { get; set; } = Array.Empty<float>();

    [ObservableProperty]
    public partial IReadOnlyList<string[]> ExportRows { get; set; } = Array.Empty<string[]>();

    [ObservableProperty]
    public partial string ExportTitle { get; set; } = "";

    [ObservableProperty]
    public partial string ExportSublinea { get; set; } = "";

    [ObservableProperty]
    public partial string FileNameBase { get; set; } = "Cierre";

    // Gráfica de balance (LiveCharts2)
    [ObservableProperty]
    public partial ISeries[] Series { get; set; } = Array.Empty<ISeries>();

    [ObservableProperty]
    public partial Axis[] XAxes { get; set; } = Array.Empty<Axis>();

    [ObservableProperty]
    public partial Axis[] YAxes { get; set; } = Array.Empty<Axis>();

    public AccountingViewModel(AccountingService accounting, CurrencyService currency)
    {
        _accounting = accounting;
        _currency = currency;
        // Eje Y sin decimales (cop) y paleta predefinida; se inicializa una vez.
        YAxes = new[]
        {
            new Axis { Labeler = v => v.ToString("N0"), LabelsPaint = new SolidColorPaint(AxisColor) }
        };
        Refresh();
    }

    public void Refresh() => Reload();

    // Cualquier cambio en los filtros recarga el cierre al instante.
    partial void OnFechaDesdeChanged(DateTimeOffset? value) => Reload();
    partial void OnFechaHastaChanged(DateTimeOffset? value) => Reload();
    partial void OnVistaIndexChanged(int value) => Reload();
    partial void OnMetodoIndexChanged(int value) => Reload();
    partial void OnSearchTextChanged(string value) => Reload();

    private DateTime From() => (FechaDesde ?? DateTimeOffset.Now).Date;
    private DateTime To() => (FechaHasta ?? DateTimeOffset.Now).Date;

    // Da inicio a una recarga versionada: marca la versión actual y lanza la
    // carga en segundo plano (fire-and-forget) para no bloquear la UI.
    private void Reload()
    {
        var version = Interlocked.Increment(ref _version);
        IsLoading = true;
        IsEmpty = false;
        IsError = false;
        _ = LoadAsync(version);
    }

    // Carga el cierre fuera del hilo de UI (Task.Run) y, al terminar, solo
    // aplica los resultados si sigue siendo la versión más reciente; de lo
    // contrario descarta los datos obsoletos (evita filas duplicadas).
    private async Task LoadAsync(int version)
    {
        try
        {
            var from = From();
            var to = To();
            var vista = VistaIndex;
            var metodo = MetodoIndex <= 0 ? "" : MetodoOptions[MetodoIndex];
            var search = SearchText;

            // Trabajo pesado en pool de hilos: construye cierre y serie de balance.
            var (summary, balance) = await Task.Run(() =>
            {
                var s = _accounting.BuildCierre(from, to, metodo, search, vista);
                var b = _accounting.BalanceSeries(from, to);
                return (s, b);
            });
            if (version != _version) return; // sobrevino una recarga más nueva

            // Se vuelca el catálogo de columnas (encabezados reutilizados por PDF/Excel).
            var cat = summary.Catalogo;
            Columns = cat.Headers;
            OnPropertyChanged(nameof(ColumnCount));

            Rows.Clear();
            foreach (var r in cat.Rows)
                Rows.Add(new CierreRowViewModel(r));
            IsEmpty = Rows.Count == 0;

            // Resumen del periodo: ventas, cobros, total y nº de facturas.
            var t = summary.Totals;
            VentasText = Fmt(t.Ventas);
            CobrosText = Fmt(t.Cobros);
            TotalText = Fmt(t.Total);
            FacturasText = $"{t.FacturasAcc} accesorios · {t.FacturasMant} mantenimiento";

            PeriodoText = $"Periodo: desde {from:dd/MM/yyyy} hasta {to:dd/MM/yyyy}";

            // Datos de exportación a PDF/Excel (encabezados, anchos, filas y título).
            ExportHeaders = cat.Headers;
            ExportWidths = cat.Widths;
            ExportRows = summary.ExportRows;
            ExportTitle = "CIERRE CONTABLE";
            var vistaLabel = VistaOptions[vista];
            ExportSublinea = $"{PeriodoText} · Vista: {vistaLabel}";
            FileNameBase = $"Cierre_{from:yyyyMMdd}-{to:yyyyMMdd}_{vistaLabel}";

            RebuildChart(balance);
            IsLoading = false;
        }
        catch (Exception ex)
        {
            // Un error se reporta solo si no lo ha invalidado otra recarga posterior.
            if (version != _version) return;
            Rows.Clear();
            IsLoading = false;
            IsEmpty = false;
            IsError = true;
            ErrorMessage = $"No se pudo cargar el cierre contable. Detalle: {ex.Message}";
        }
    }

    // Reconstruye la gráfica de balance: dos series de barras (ventas y cobros
    // de mantenimiento) por fecha y los ejes X con etiquetas dd/MM.
    private void RebuildChart(List<(DateTime fecha, decimal ventas, decimal cobros)> balance)
    {
        Series = new ISeries[]
        {
            new ColumnSeries<ObservableValue>
            {
                Name = "Ventas",
                Values = balance.Select(x => new ObservableValue((double)x.ventas)).ToArray(),
                Fill = new SolidColorPaint(VentasColor),
                MaxBarWidth = 22
            },
            new ColumnSeries<ObservableValue>
            {
                Name = "Cobros mantenimiento",
                Values = balance.Select(x => new ObservableValue((double)x.cobros)).ToArray(),
                Fill = new SolidColorPaint(CobrosColor),
                MaxBarWidth = 22
            }
        };

        var labels = balance.Select(x => x.fecha.ToString("dd/MM")).ToArray();
        XAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                Labeler = v => labels[(int)v],
                LabelsPaint = new SolidColorPaint(AxisColor),
                TextSize = 11,
                SeparatorsPaint = new SolidColorPaint(SeparatorColor)
            }
        };
    }

    private string Fmt(decimal v) => _currency.Fmt(v);
}