// ClientesView.axaml.cs — Vista de clientes: construye en code-behind el menú "⋯" de acciones por fila.
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Clientes;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class ClientesView : UserControl
{
    private ColumnResizer? _resizer;

    public ClientesView()
    {
        InitializeComponent();
        _resizer = ColumnResizer.Enable(HeaderGrid, new double[] { 90, 100, 100, 90, 100, 80, 90, 60 });
    }

    private void OnRowLoaded(object? sender, RoutedEventArgs e) => _resizer?.AddRow(sender as Grid);

    private void OnRowUnloaded(object? sender, RoutedEventArgs e) => _resizer?.RemoveRow(sender as Grid);

    /// <summary>
    /// Menú contextual de acciones por fila (Editar / Eliminar). Los comandos
    /// viven en la fila (ClienteRowViewModel) y ya capturan el cliente.
    /// </summary>
    private void OnOptionsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ClienteRowViewModel row) return;

        var menu = new MenuFlyout();
        menu.Items.Add(new MenuItem { Header = "Editar", Command = row.EditarCommand });
        menu.Items.Add(new MenuItem { Header = "Eliminar", Command = row.EliminarCommand });
        menu.ShowAt(btn);
    }
}