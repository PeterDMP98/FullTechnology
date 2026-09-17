// CobroView.axaml.cs — Diálogo de cobro: confirma el pago vía el VM (persiste y pasa de estado) y cierra si fue exitoso.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class CobroView : Window
{
    public CobroView()
    {
        InitializeComponent();
    }

    // El VM registra el pago; solo si ConfirmaAsync devuelve true se cierra con éxito.
    private async void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CobroViewModel vm && await vm.ConfirmarAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}