// BuscarClienteView.axaml.cs — Diálogo de búsqueda de clientes/proveedores: acepta la fila seleccionada y cierra con true/false.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class BuscarClienteView : Window
{
    public BuscarClienteView()
    {
        InitializeComponent();
    }

    // Doble-tap en una fila equivale a aceptar la selección.
    private void OnListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ResultsList.SelectedItem is not null)
            Accept();
    }

    private void OnAccept(object? sender, RoutedEventArgs e) => Accept();

    private void Accept()
    {
        if (DataContext is not BuscarClienteViewModel vm) return;
        if (ResultsList.SelectedItem is ClienteRow row)
        {
            // Ejecuta la selección en el VM (valida tipo cliente/proveedor).
            vm.SeleccionarCommand.Execute(row);
            if (vm.Selected is not null)
            {
                Close(true);
                return;
            }
        }
        // Sin selección: el diálogo permanece abierto.
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}