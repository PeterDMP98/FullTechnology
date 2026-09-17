// ClientesView.axaml.cs — Vista de clientes: construye en code-behind el menú "⋯" de acciones por fila.
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Clientes;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class ClientesView : UserControl
{
    public ClientesView()
    {
        InitializeComponent();
    }

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