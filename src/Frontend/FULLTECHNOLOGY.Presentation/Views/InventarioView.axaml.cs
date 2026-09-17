// InventarioView.axaml.cs — Vista del inventario: construye en code-behind el menú "⋯" de acciones por fila.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Inventario;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class InventarioView : UserControl
{
    public InventarioView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Menú contextual de acciones por fila (Editar / Eliminar). Los comandos
    /// viven en la fila (ProductoRowViewModel) y ya capturan el producto.
    /// </summary>
    private void OnOptionsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ProductoRowViewModel row) return;

        var menu = new MenuFlyout();
        menu.Items.Add(new MenuItem { Header = "Editar", Command = row.EditarCommand });
        menu.Items.Add(new MenuItem { Header = "Eliminar", Command = row.EliminarCommand });
        menu.ShowAt(btn);
    }
}