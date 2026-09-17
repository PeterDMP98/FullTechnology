// ClienteEditDialogView.axaml.cs — Diálogo de crear/editar cliente: guarda a través del VM y cierra solo si la validación pasó.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class ClienteEditDialogView : Window
{
    public ClienteEditDialogView()
    {
        InitializeComponent();
    }

    // El VM valida y persiste; si devuelve true se cierra con éxito.
    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ClienteEditDialogViewModel vm && await vm.SaveAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}