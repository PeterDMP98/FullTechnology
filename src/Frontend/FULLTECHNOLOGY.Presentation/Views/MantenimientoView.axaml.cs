// MantenimientoView.axaml.cs — Vista de órdenes de mantenimiento: menú "⋯" por fila construido en code-behind con los comandos del VM de la página.
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Mantenimiento;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class MantenimientoView : UserControl
{
    private ColumnResizer? _resizer;

    public MantenimientoView()
    {
        InitializeComponent();
        _resizer = ColumnResizer.Enable(HeaderGrid, new double[] { 80, 100, 80, 80, 90, 90, 80, 80, 80, 70 });
    }

    private void OnRowLoaded(object? sender, RoutedEventArgs e) => _resizer?.AddRow(sender as Grid);

    private void OnRowUnloaded(object? sender, RoutedEventArgs e) => _resizer?.RemoveRow(sender as Grid);

    /// <summary>
    /// Menú contextual de acciones por fila (Editar / Editar diagnóstico /
    /// Cobrar / Eliminar). Se construye en código porque los ítems deben
    /// resolver comandos de la página (DataContext de la vista) y dispararse
    /// con la fila como parámetro.
    /// </summary>
    private void OnOptionsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (DataContext is not MantenimientoViewModel vm || btn.DataContext is not OrderRowViewModel row) return;

        var menu = new MenuFlyout();
        menu.Items.Add(MenuItem("Editar", vm.EditarOrdenCommand, row));
        menu.Items.Add(MenuItem("Editar diagnóstico", vm.EditarDiagnosticoCommand, row));
        var cobrar = MenuItem("Cobrar", vm.CobrarCommand, row);
        cobrar.IsEnabled = row.CanCobrar;
        menu.Items.Add(cobrar);
        menu.Items.Add(MenuItem("Eliminar", vm.EliminarCommand, row));
        menu.ShowAt(btn);
    }

    private static MenuItem MenuItem(string header, ICommand command, object parameter) =>
        new() { Header = header, Command = command, CommandParameter = parameter };
}