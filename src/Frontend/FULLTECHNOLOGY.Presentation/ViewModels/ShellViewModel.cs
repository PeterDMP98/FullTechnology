// ShellViewModel.cs — Cáscara de la app: navegación entre módulos, marca del negocio, tema, sidebar colapsable y chrome de página.
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// VM raíz que envuelve todo el shell: sidebar, topbar, host de contenido y
// navegación. Escucha a INavigationService y actualiza el chrome de página
// (padding/encabezado) según el módulo visible.
public partial class ShellViewModel : ViewModelBase
{
    // Dimensiones del sidebar (ancho expandido/colapsado y duración de la animación).
    private const double SidebarExpandedWidth = 236;
    private const double SidebarCollapsedWidth = 72;
    private const double SidebarAnimationMs = 180;

    private readonly INavigationService _nav;
    private readonly ThemeService _theme;
    private readonly BusinessSettingsService _settings;

    // Clave del último módulo navegado (se conserva porque el servicio guarda el Current).
    private string _lastKey = NavKeys.Inicio;

    // Token para interrumpir la animación del sidebar si se lanza otra al vuelo.
    private CancellationTokenSource? _collapseCts;

    public IReadOnlyList<NavigationItem> Items { get; }

    /// <summary>Nombre del negocio (settings.ini), sin hardcodear nada en la UI.</summary>
    [ObservableProperty]
    public partial string BusinessName { get; set; } = "";

    /// <summary>Primera palabra del nombre (blanco) para la marca del topbar.</summary>
    [ObservableProperty]
    public partial string BrandNamePart { get; set; } = "";

    /// <summary>Resto del nombre (acento) para la marca del topbar.</summary>
    [ObservableProperty]
    public partial string BrandRestPart { get; set; } = "";

    [ObservableProperty]
    public partial ViewModelBase? Current { get; set; }

    [ObservableProperty]
    public partial string CurrentKey { get; set; } = NavKeys.Inicio;

    [ObservableProperty]
    public partial string CurrentGlyph { get; set; } = NavKeys.GlyphFor(NavKeys.Inicio);

    [ObservableProperty]
    public partial string Title { get; set; } = "Inicio";

    [ObservableProperty]
    public partial string Subtitle { get; set; } = "";

    [ObservableProperty]
    public partial NavigationItem? SelectedItem { get; set; }

    [ObservableProperty]
    public partial double SidebarWidth { get; set; } = SidebarExpandedWidth;

    [ObservableProperty]
    public partial bool IsSidebarCollapsed { get; set; }

    /// <summary>Texto del footer del sidebar (se oculta cuando está colapsado).</summary>
    [ObservableProperty]
    public partial bool IsLabelsVisible { get; set; } = true;

    /// <summary>Padding del host de contenido (la Venta usa 0 a la derecha y arriba para que el carrito quede pegado como el sidebar).</summary>
    [ObservableProperty]
    public partial Thickness ContentPadding { get; set; } = new(40, 32, 40, 32);

    /// <summary>Oculta el encabezado de página cuando la vista lo cubre todo (Venta).</summary>
    [ObservableProperty]
    public partial bool IsPageHeaderVisible { get; set; } = true;

    /// <summary>Margen del host de vistas (0 cuando no hay encabezado de página).</summary>
    [ObservableProperty]
    public partial Thickness ContentHostMargin { get; set; } = new(0, 28, 0, 0);

    /// <summary>Modo compacto (ventana angosta): reduce paddings y oculta textos secundarios.</summary>
    [ObservableProperty]
    public partial bool IsCompact { get; set; }

    /// <summary>Oculta el subtítulo de marca del topbar cuando el espacio es compacto.</summary>
    [ObservableProperty]
    public partial bool IsBrandSubtitleVisible { get; set; } = true;

    /// <summary>Oculta la etiqueta "Administrador" del topbar cuando el espacio es compacto.</summary>
    [ObservableProperty]
    public partial bool IsAdminCaptionVisible { get; set; } = true;

    // La vista actual decide si el host se dedica a pantalla completa (Venta) o al chrome estándar.
    private bool _isVenta;

    public ShellViewModel(INavigationService nav, ThemeService theme, BusinessSettingsService settings)
    {
        _nav = nav;
        _theme = theme;
        _settings = settings;
        _settings.BusinessNameChanged += RefreshBrand;
        RefreshBrand();

        // Barra lateral con todos los módulos (la bottom bar se retiró; el sidebar navega todo).
        Items = NavKeys.All.Select(k => new NavigationItem(k)).ToList();
        _nav.CurrentChanged += OnNavigated;
        _nav.NavigateTo(NavKeys.Inicio);
    }

    // Divide el nombre del negocio en "marca" (primera palabra) y "resto" para el topbar.
    private void RefreshBrand()
    {
        BusinessName = _settings.BusinessName;
        var parts = BusinessName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        BrandNamePart = parts.Length > 0 ? parts[0] : "";
        BrandRestPart = parts.Length > 1 ? parts[1] : "";
    }

    // Se dispara con cada navegación: actualiza el VM actual, el título y glifo
    // de la página, el subtítulo, el ítem seleccionado y el chrome (Venta especial).
    private void OnNavigated()
    {
        Current = _nav.Current;
        if (!string.IsNullOrEmpty(_lastKey) && NavKeys.All.Contains(_lastKey))
        {
            CurrentKey = _lastKey;
            Title = _lastKey;
            CurrentGlyph = NavKeys.GlyphFor(_lastKey);
        }
        Subtitle = Current switch
        {
            PlaceholderViewModel p => p.Description,
            InicioViewModel i => i.Subtitle,
            MantenimientoViewModel m => m.Subtitle,
            ConfigurationViewModel c => c.Subtitle,
            _ => ""
        };
        SelectedItem = Items.FirstOrDefault(i => i.Key == CurrentKey);
        foreach (var item in Items)
            item.IsActive = item.Key == CurrentKey;

        UpdatePageChrome(Current);
    }

    // El módulo de Venta ocupa todo el host (sin encabezado, sin padding extra);
    // el resto usa el chrome estándar. En modo compacto los paddings bajan a 24.
    private void UpdatePageChrome(ViewModelBase? current)
    {
        _isVenta = current is VentaViewModel;
        ApplyPageChrome();
    }

    private void ApplyPageChrome()
    {
        var pad = IsCompact ? 24d : 40d;
        ContentPadding = _isVenta
            ? new Thickness(pad, 0, 0, 0)
            : new Thickness(pad, IsCompact ? 24 : 32, pad, IsCompact ? 24 : 32);
        IsPageHeaderVisible = !_isVenta;
        ContentHostMargin = _isVenta ? new Thickness(0, 0, 0, 0) : new Thickness(0, IsCompact ? 16 : 28, 0, 0);
        IsBrandSubtitleVisible = !IsCompact;
        IsAdminCaptionVisible = !IsCompact;
    }

    /// <summary>Actualiza el modo compacto según el ancho de la ventana (breakpoint 1120px).</summary>
    public void UpdateLayout(double width) => IsCompact = width < 1120;

    // Devuelve la página visible si la clave es válida.
    [RelayCommand]
    private void Navigate(string key)
    {
        if (!NavKeys.All.Contains(key)) return;
        _lastKey = key;
        SelectedItem = Items.FirstOrDefault(i => i.Key == key);
        _nav.NavigateTo(key);
    }

    /// <summary>El sidebar navega por selección de ListBoxItem (fila 44px completa).</summary>
    partial void OnSelectedItemChanged(NavigationItem? value)
    {
        if (value is not null && value.Key != CurrentKey)
            Navigate(value.Key);
    }

    [RelayCommand]
    private void SelectItem(NavigationItem item) => Navigate(item.Key);

    [RelayCommand]
    private void ToggleTheme() => _theme.Toggle();

    /// <summary>
    /// Colapsa/expande el sidebar animando el ancho (236 ↔ 72) en el hilo de UI.
    /// </summary>
    [RelayCommand]
    private async Task ToggleSidebar()
    {
        _collapseCts?.Cancel();
        IsSidebarCollapsed = !IsSidebarCollapsed;
        IsLabelsVisible = !IsSidebarCollapsed;
        var target = IsSidebarCollapsed ? SidebarCollapsedWidth : SidebarExpandedWidth;
        var start = SidebarWidth;
        foreach (var item in Items)
            item.IsExpanded = !IsSidebarCollapsed;

        var cts = _collapseCts = new CancellationTokenSource();
        var t0 = DateTime.UtcNow;
        try
        {
            while (true)
            {
                var t = (DateTime.UtcNow - t0).TotalMilliseconds / SidebarAnimationMs;
                if (t >= 1.0) break;
                SidebarWidth = start + (target - start) * (1d - Math.Pow(1d - t, 3));
                await Task.Delay(16, cts.Token);
            }
            SidebarWidth = target;
        }
        catch (OperationCanceledException)
        {
            // Nueva animación en curso; la otra toma el control.
        }
    }
}