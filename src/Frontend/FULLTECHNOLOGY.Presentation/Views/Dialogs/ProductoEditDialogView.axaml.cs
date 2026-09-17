// ProductoEditDialogView.axaml.cs — Diálogo de producto: guarda vía VM y, en code-behind, el menú Buscar/Crear proveedor con este diálogo como owner.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class ProductoEditDialogView : Window
{
    public ProductoEditDialogView()
    {
        InitializeComponent();
    }

    // El VM valida y persiste; solo si SaveAsync devuelve true se cierra con éxito.
    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductoEditDialogViewModel vm && await vm.SaveAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    /// <summary>
    /// Botón "Proveedor": desplegable con Buscar/Crear proveedor. Se ejecuta
    /// desde la vista porque el proveedor (dialéctica BuscarCliente/ClienteEdit)
    /// debe abrirse con ESTE diálogo como owner, no con la MainWindow.
    /// </summary>
    private void OnProveedorClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (DataContext is not ProductoEditDialogViewModel vm) return;

        var menu = new MenuFlyout();
        var buscar = new MenuItem { Header = "Buscar proveedor" };
        buscar.Click += async (s, args) => await vm.BuscarProveedorAsync(this);
        var crear = new MenuItem { Header = "Crear proveedor" };
        crear.Click += async (s, args) => await vm.CrearProveedorAsync(this);
        menu.Items.Add(buscar);
        menu.Items.Add(crear);
        menu.ShowAt(btn);
    }
}