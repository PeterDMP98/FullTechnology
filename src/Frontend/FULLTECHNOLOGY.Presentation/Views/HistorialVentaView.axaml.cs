// HistorialVentaView.axaml.cs — Vista del historial de ventas: menú "⋯" por fila construido en code-behind con los comandos de la fila (ver factura).
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.HistorialVenta;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class HistorialVentaView : UserControl
{
    private ColumnResizer? _resizer;

    public HistorialVentaView()
    {
        InitializeComponent();
        _resizer = ColumnResizer.Enable(HeaderGrid, new double[] { 80, 90, 100, 80, 80, 80, 90, 60 });
    }

    private void OnRowLoaded(object? sender, RoutedEventArgs e) => _resizer?.AddRow(sender as Grid);

    private void OnRowUnloaded(object? sender, RoutedEventArgs e) => _resizer?.RemoveRow(sender as Grid);

    /// <summary>
    /// Menú contextual de acciones por fila (Ver factura). Se construye en código
    /// porque los ítems deben resolver el comando de la fila (VerCommand) y dispararse
    /// con la propia fila como parámetro.
    /// </summary>
    private void OnOptionsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not HistorialVentaRowViewModel row) return;

        var menu = new MenuFlyout();
        menu.Items.Add(MenuItem("Ver factura", row.VerCommand, row));
        menu.Items.Add(MenuItem("Imprimir factura", row.ImprimirCommand, row));
        menu.ShowAt(btn);
    }

    private static MenuItem MenuItem(string header, ICommand command, object parameter) =>
        new() { Header = header, Command = command, CommandParameter = parameter };
}