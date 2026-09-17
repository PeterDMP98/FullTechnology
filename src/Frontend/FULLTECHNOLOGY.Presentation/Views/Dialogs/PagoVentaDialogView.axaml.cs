// PagoVentaDialogView.axaml.cs — Diálogo de pago de venta de accesorios: captura el medio elegido y cierra; el VM de venta lee la selección.
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class PagoVentaDialogView : Window
{
    public PagoVentaDialogView()
    {
        InitializeComponent();
    }

    // Aceptar cierra con true; el llamante lee SelectedMetodo del DataContext (VM).
    private void OnAceptar(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}