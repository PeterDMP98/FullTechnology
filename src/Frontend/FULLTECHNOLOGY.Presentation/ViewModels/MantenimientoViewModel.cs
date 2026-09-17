// MantenimientoViewModel.cs — Órdenes de servicio: búsqueda, filtros por estado (cards/combo), orden por columnas, paginación y acciones de fila vía diálogos.
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using FULLTECHNOLOGY.Presentation.ViewModels.Mantenimiento;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// Módulo MANTENIMIENTO (F9). Replica MantenimientoView v2 sobre
// Application/Services: búsqueda + filtro de estado + cards con
// filtro por click, orden por columnas, paginación 10/25/50 y
// estados vacío/cargando. Los diálogos (sin ventanas por módulo)
// se abren como ventanas modales reutilizando el owner del shell.
// ============================================================
public partial class MantenimientoViewModel : ViewModelBase, IRefreshable
{
    private readonly OrdersService _orders;
    private readonly PaymentsService _payments;
    private readonly CustomersService _customers;
    private readonly CurrencyService _currency;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    // Copia local de las órdenes cargadas y sus estadísticas (se filtran/sortean
    // en memoria en ApplyView); _version implementa la recarga versionada.
    private IReadOnlyList<ServiceOrder> _loaded = Array.Empty<ServiceOrder>();
    private OrderStats _stats = new(0, 0, 0, 0, 0);
    private int _version;

    public string Subtitle => "Órdenes de servicio: ingreso, seguimiento, cobros y entrega.";

    public ObservableCollection<OrderRowViewModel> Rows { get; } = new();
    public ObservableCollection<StatCardViewModel> Cards { get; } = new();
    public ObservableCollection<PageItem> PageNumbers { get; } = new();

    public IReadOnlyList<StatusOption> StatusOptions { get; }
    public IReadOnlyList<int> PageSizes { get; } = new[] { 10, 25, 50 };

    // ---- Filtros y vista ----
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial string StatusFilter { get; set; } = StatusFilters.Todos;

    /// <summary>Objeto seleccionado en el combo de estado (sincronizado con StatusFilter).</summary>
    [ObservableProperty]
    public partial StatusOption? SelectedStatus { get; set; }

    [ObservableProperty]
    public partial SortColumn SortColumn { get; set; } = SortColumn.Ingreso;

    /// <summary>Orden inicial: ingreso descendente (más recientes primero), como la BD v2.</summary>
    [ObservableProperty]
    public partial bool SortAscending { get; set; }

    // Paginación: tamaño de página y página actual (1-based).
    [ObservableProperty]
    public partial int PageSize { get; set; } = 10;

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalRows { get; set; }

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial bool HasPrev { get; set; }

    [ObservableProperty]
    public partial bool HasNext { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    // Título de columna con la divisa activa (p. ej. "$").
    [ObservableProperty]
    public partial string CurrencyText { get; set; } = "";

    // Flechas de la columna activa en el header (▲/▼ según dirección).
    public string ArrowOrderNumber => Arrow(SortColumn.OrderNumber);
    public string ArrowCliente => Arrow(SortColumn.Cliente);
    public string ArrowDocumento => Arrow(SortColumn.Documento);
    public string ArrowCelular => Arrow(SortColumn.Celular);
    public string ArrowEquipo => Arrow(SortColumn.Equipo);
    public string ArrowEstado => Arrow(SortColumn.Estado);
    public string ArrowIngreso => Arrow(SortColumn.Ingreso);
    public string ArrowTotal => Arrow(SortColumn.Total);
    public string ArrowSaldo => Arrow(SortColumn.Saldo);

    public MantenimientoViewModel(
        OrdersService orders,
        PaymentsService payments,
        CustomersService customers,
        CurrencyService currency,
        IDialogService dialogs,
        IServiceProvider services)
    {
        _orders = orders;
        _payments = payments;
        _customers = customers;
        _currency = currency;
        _dialogs = dialogs;
        _services = services;

        StatusOptions = BuildStatusOptions();
        CurrencyText = _currency.Symbol;

        // Cards de filtro rápido (click = filtrar por ese estado). Los glifos son emojis acordes
        // al estado: 📊 total, 📥 recibido, 🔧 en reparación, ✅ listo y 📤 entregado.
        Cards.Add(new StatCardViewModel("Total", "\uD83D\uDCCA", StatusFilters.Todos, "AccentPrimary", SelectCard));
        Cards.Add(new StatCardViewModel("Recibido", "\uD83D\uDCE5", RepairStatuses.Recibido, "StatusSuccess", SelectCard));
        Cards.Add(new StatCardViewModel("En reparación", "\uD83D\uDD27", RepairStatuses.EnReparacion, "StatusWarning", SelectCard));
        Cards.Add(new StatCardViewModel("Listos", "\u2705", StatusFilters.Listos, "StatusPurple", SelectCard));
        Cards.Add(new StatCardViewModel("Entregados", "\uD83D\uDCE4", RepairStatuses.Entregado, "StatusTeal", SelectCard));
        Cards[0].IsSelected = true; // filtro inicial: Todos
        SelectedStatus = StatusOptions[0];
    }

    // Construye el combo de estados: "Todos", el virtual "Listos" y luego los
    // estados reales en el orden de display de RepairStatuses.
    private static IReadOnlyList<StatusOption> BuildStatusOptions()
    {
        var list = new List<StatusOption>
        {
            new("Todos", StatusFilters.Todos),
            new(StatusFilters.ListosLabel, StatusFilters.Listos)
        };
        list.AddRange(RepairStatuses.DisplayOrder.Select(s => new StatusOption(s, s)));
        return list;
    }

    // Devuelve la flecha ▲/▼ para la columna sortable activa (vacío si no lo es).
    private string Arrow(SortColumn col) => SortColumn == col ? (SortAscending ? "\u25B2" : "\u25BC") : "";

    // ---- Carga / filtros ----
    public void Refresh() => Reload();

    partial void OnSearchTextChanged(string value) => Reload();

    // Al cambiar el filtro de estado se sincronizan las cards, el combo y la lista.
    partial void OnStatusFilterChanged(string value)
    {
        foreach (var c in Cards) c.IsSelected = c.Filter == value;
        if (SelectedStatus?.Value != value)
            SelectedStatus = StatusOptions.FirstOrDefault(o => o.Value == value);
        Reload();
    }

    // El combo también puede cambiar el filtro (doble fuente de verdad).
    partial void OnSelectedStatusChanged(StatusOption? value)
    {
        if (value is not null && value.Value != StatusFilter)
            StatusFilter = value.Value;
    }

    // Cambiar el tamaño de página reinicia a la primera página y reordena la vista.
    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        ApplyView();
    }

    // Ordenar en memoria no necesita recargar de la BD: solo renotificar flechas.
    partial void OnSortColumnChanged(SortColumn value) => NotifyArrows();
    partial void OnSortAscendingChanged(bool value) => NotifyArrows();

    // Notifica a la vista el cambio de flechas (▲/▼) de los encabezados.
    private void NotifyArrows()
    {
        OnPropertyChanged(nameof(ArrowOrderNumber));
        OnPropertyChanged(nameof(ArrowCliente));
        OnPropertyChanged(nameof(ArrowDocumento));
        OnPropertyChanged(nameof(ArrowCelular));
        OnPropertyChanged(nameof(ArrowEquipo));
        OnPropertyChanged(nameof(ArrowEstado));
        OnPropertyChanged(nameof(ArrowIngreso));
        OnPropertyChanged(nameof(ArrowTotal));
        OnPropertyChanged(nameof(ArrowSaldo));
    }

    // Click en una card: cambia el filtro de estado y recarga.
    private void SelectCard(string filter)
    {
        StatusFilter = filter;
    }

    // Recarga versionada: incrementa la versión y lanza la carga en segundo plano.
    private void Reload()
    {
        var version = Interlocked.Increment(ref _version);
        IsLoading = true;
        IsEmpty = false;
        IsError = false;
        _ = LoadAsync(version);
    }

    private async Task LoadAsync(int version)
    {
        try
        {
            // Los filtros "Todos"/"Listos" son virtuales: no existen como estado real.
            var (rows, stats) = await Task.Run(() =>
            {
                var status = StatusFilter is StatusFilters.Todos or StatusFilters.Listos ? null : StatusFilter;
                return (_orders.Search(SearchText, status), _orders.GetStats());
            });
            if (version != _version) return; // sobrevino una recarga más nueva

            _loaded = rows;
            _stats = stats;
            ApplyView();
            IsLoading = false;
        }
        catch (Exception ex)
        {
            if (version != _version) return;
            Rows.Clear();
            IsLoading = false;
            IsEmpty = false;
            IsError = true;
            ErrorMessage = $"No se pudieron cargar las órdenes. Detalle: {ex.Message}";
        }
    }

    // Aplica en memoria el filtro "Listos", el orden de columna y la paginación
    // sobre las órdenes ya cargadas (_loaded).
    private void ApplyView()
    {
        var filtered = _loaded;
        if (StatusFilter == StatusFilters.Listos)
            filtered = filtered.Where(o => o.Status is RepairStatuses.Reparado or RepairStatuses.ListoParaEntregar).ToList();

        var sorted = Apply(filtered);
        TotalRows = sorted.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRows / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        var page = sorted.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

        Rows.Clear();
        foreach (var o in page)
            Rows.Add(new OrderRowViewModel(o, _currency, VerOrdenCommand));

        IsEmpty = Rows.Count == 0;
        HasPrev = CurrentPage > 1;
        HasNext = CurrentPage < TotalPages;
        BuildPageNumbers();
        UpdateCards();
        NotifyPagination();
    }

    // Elige la clave de orden según la columna activa.
    private List<ServiceOrder> Apply(IEnumerable<ServiceOrder> src) => SortColumn switch
    {
        SortColumn.OrderNumber => Ordered(src, o => o.OrderNumber),
        SortColumn.Cliente => Ordered(src, o => o.CustomerName),
        SortColumn.Documento => Ordered(src, o => o.CustomerDoc),
        SortColumn.Celular => Ordered(src, o => o.CustomerPhone),
        SortColumn.Equipo => Ordered(src, o => $"{o.Brand} {o.Model}".Trim()),
        SortColumn.Estado => Ordered(src, o => o.Status),
        SortColumn.Ingreso => Ordered(src, o => o.ReceivedAt),
        SortColumn.Total => Ordered(src, o => o.Total),
        SortColumn.Saldo => Ordered(src, o => o.Balance),
        _ => src.ToList()
    };

    // Ordena la lista asc/desc según SortAscending usando una clave comparable.
    private List<ServiceOrder> Ordered(IEnumerable<ServiceOrder> src, Func<ServiceOrder, IComparable> key) =>
        SortAscending ? src.OrderBy(key).ToList() : src.OrderByDescending(key).ToList();

    // Reconstruye los botones numéricos de página (máx. 5, empiezan en 1).
    private void BuildPageNumbers()
    {
        PageNumbers.Clear();
        for (int i = 1; i <= Math.Min(TotalPages, 5); i++)
            PageNumbers.Add(new PageItem(i, i == CurrentPage, GoToPage));
    }

    // Actualiza los contadores de las cards según el resumen del servicio.
    private void UpdateCards()
    {
        Cards[0].Value = _stats.Total.ToString();
        Cards[1].Value = _stats.Recibido.ToString();
        Cards[2].Value = _stats.EnReparacion.ToString();
        Cards[3].Value = _stats.ListosParaEntregar.ToString();
        Cards[4].Value = _stats.Entregados.ToString();
    }

    private void NotifyPagination()
    {
        OnPropertyChanged(nameof(TotalRows));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(HasPrev));
        OnPropertyChanged(nameof(HasNext));
    }

    // ---- Orden ----
    // [RelayCommand] genera SortByColumnCommand: click en una columna cambia la
    // clave de orden (o invierte la dirección si ya estaba activa).
    [RelayCommand]
    private void SortByColumn(object? param)
    {
        if (param is not SortColumn col) return;
        if (SortColumn == col)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = col;
            SortAscending = true;
        }
        ApplyView();
    }

    // ---- Paginación ----
    [RelayCommand]
    private void GoToPage(int page)
    {
        CurrentPage = Math.Clamp(page, 1, TotalPages);
        ApplyView();
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (!HasPrev) return;
        CurrentPage--;
        ApplyView();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (!HasNext) return;
        CurrentPage++;
        ApplyView();
    }

    // ---- Acciones de fila / módulo ----
    // Alta de orden nueva; tras guardar se recarga la lista.
    [RelayCommand]
    private async Task NuevoIngresoAsync()
    {
        if (await OpenOrderAsync(null)) Reload();
    }

    [RelayCommand]
    private async Task EditarOrdenAsync(OrderRowViewModel? row)
    {
        if (row is null) return;
        if (await OpenOrderAsync(row.Order)) Reload();
    }

    // Abre el diálogo de edición de diagnóstico (VM por DI, vista local).
    [RelayCommand]
    private async Task EditarDiagnosticoAsync(OrderRowViewModel? row)
    {
        if (row is null) return;
        var vm = _services.GetRequiredService<EditDiagnosticoViewModel>();
        vm.Initialize(row.Order);
        var win = new EditDiagnosticoView { DataContext = vm };
        if (await ShowDialogAsync(win)) Reload();
    }

    // Abre el diálogo de cobro (si el saldo ya está en 0 se avisa y no se abre).
    [RelayCommand]
    private async Task CobrarAsync(OrderRowViewModel? row)
    {
        if (row is null) return;
        if (row.Order.Balance <= 0)
        {
            await _dialogs.ShowMessageAsync("Cobro", "Esta orden no tiene saldo pendiente.");
            return;
        }
        var vm = _services.GetRequiredService<CobroViewModel>();
        vm.Initialize(row.Order);
        var win = new CobroView { DataContext = vm };
        if (await ShowDialogAsync(win)) Reload();
    }

    // Muestra el historial de la orden (ingreso, movimientos/abonos y entregas).
    [RelayCommand]
    private async Task VerOrdenAsync(OrderRowViewModel? row)
    {
        if (row is null) return;
        var vm = _services.GetRequiredService<OrderHistoryViewModel>();
        vm.Initialize(row.Order);
        var win = new OrderHistoryView { DataContext = vm };
        await ShowDialogAsync(win);
    }

    // Elimina la orden tras confirmación del usuario.
    [RelayCommand]
    private async Task EliminarAsync(OrderRowViewModel? row)
    {
        if (row is null) return;
        if (!await _dialogs.ConfirmAsync("Confirmar", $"¿Eliminar {row.Order.OrderNumber}? Esta acción no se puede deshacer."))
            return;
        _orders.Delete(row.Order.Id);
        Reload();
    }

    // Abre el formulario de ingreso/edición de orden. El VM se resuelve por DI;
    // la vista es un Window creado aquí que el VM guarda y la propia vista cierra.
    private async Task<bool> OpenOrderAsync(ServiceOrder? existing)
    {
        var vm = _services.GetRequiredService<OrderFormViewModel>();
        vm.Initialize(existing, existing is null);
        var win = new OrderFormView { DataContext = vm };
        return await ShowDialogAsync(win);
    }

    // Todos los diálogos de mantenimiento se abren modales con el owner del shell.
    private Task<bool> ShowDialogAsync(Window window) => window.ShowDialog<bool>(DialogService.OwnerWindow!);
}