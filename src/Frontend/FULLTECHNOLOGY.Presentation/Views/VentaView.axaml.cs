// VentaView.axaml.cs — Vista del módulo de ventas (carrito + productos). Sin lógica en code-behind; todo vive en su VM.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class VentaView : UserControl
{
    private ColumnResizer? _resizer;

    public VentaView()
    {
        InitializeComponent();
        _resizer = ColumnResizer.Enable(HeaderGrid, new double[] { 70, 100, 80, 70, 80, 50, 80, 70 });
    }

    private void OnRowLoaded(object? sender, RoutedEventArgs e) => _resizer?.AddRow(sender as Grid);

    private void OnRowUnloaded(object? sender, RoutedEventArgs e) => _resizer?.RemoveRow(sender as Grid);
}