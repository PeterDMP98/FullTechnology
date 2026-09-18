// InvoicePreviewDialogView.axaml.cs — Diálogo de previsualización de la factura: Imprimir ejecuta el comando del VM (elige impresora) y cierra con true; Cancelar cierra sin imprimir.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class InvoicePreviewDialogView : Window
{
    public InvoicePreviewDialogView()
    {
        InitializeComponent();
    }

    // Imprimir: delega en el VM (abre el diálogo de impresora de Windows y
    // cambia Aceptada si realmente se imprimió); solo entonces se cierra con true.
    private void OnImprimir(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InvoicePreviewViewModel vm) return;
        vm.ImprimirCommand.Execute(null);
        Close(vm.Aceptada);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}