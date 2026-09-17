// MainWindow.axaml.cs — Ventana principal con titlebar personalizado: arrastre, doble-tap para maximizar y botones minimizar/maximizar/cerrar.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class MainWindow : Window
{
    // Fecha del último tap, para detectar doble-tap en el titlebar.
    private DateTime _lastTap = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        WireWindowState();
    }

    // Ctor con DI: el shell de navegación llega como DataContext y sirve de breakpoint
    // de ancho (modo compacto) cuando la ventana se redimensiona.
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        SizeChanged += (_, e) => viewModel.UpdateLayout(e.NewSize.Width);
        WireWindowState();
    }

    // Escucha el estado de la ventana para mantener el glifo del botón
    // maximizar/restaurar (▢ ↔ ▣) y su tooltip sincronizados.
    private void WireWindowState()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                UpdateMaximizeGlyph();
        };
        UpdateMaximizeGlyph();
    }

    private void UpdateMaximizeGlyph()
    {
        var maximized = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximized ? "\u25A3" : "\u25A2";
        ToolTip.SetTip(MaximizeButton, maximized ? "Restaurar" : "Maximizar / restaurar");
    }

    // ============ Titlebar custom (spec §5.1) ============

    // Arrastra la ventana desde el titlebar (salvo que el origen sea un botón).
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (IsInteractiveSource(e)) return;
        BeginMoveDrag(e);
    }

    // Doble-tap (menos de 350 ms entre taps) maximiza o restaura.
    private void OnTitleBarTapped(object? sender, TappedEventArgs e)
    {
        if (IsInteractiveSource(e)) return;
        var now = DateTime.UtcNow;
        var isDoubleTap = (now - _lastTap).TotalMilliseconds < 350;
        _lastTap = now;
        if (isDoubleTap) ToggleMaximize();
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        ToggleMaximize();

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    /// <summary>Evita arrastrar/doble-clic cuando la pulsación nace en un botón.</summary>
    private static bool IsInteractiveSource(RoutedEventArgs e) =>
        e.Source is Control c && c.FindAncestorOfType<Button>() is not null;
}