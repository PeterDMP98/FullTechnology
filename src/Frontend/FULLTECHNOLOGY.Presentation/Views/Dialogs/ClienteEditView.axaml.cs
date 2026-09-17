// ClienteEditView.axaml.cs — Diálogo de alta de cliente/comprador de una orden: guarda vía VM y cierra solo si fue exitoso.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class ClienteEditView : Window
{
    public ClienteEditView()
    {
        InitializeComponent();
    }

    // El VM valida y persiste; si devuelve true se cierra con éxito.
    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ClienteEditViewModel vm && await vm.SaveAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}