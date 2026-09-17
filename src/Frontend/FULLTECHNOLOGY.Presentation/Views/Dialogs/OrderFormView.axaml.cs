// OrderFormView.axaml.cs — Diálogo de crear/editar orden de mantenimiento: el VM guarda y la vista cierra solo si fue exitoso.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class OrderFormView : Window
{
    public OrderFormView()
    {
        InitializeComponent();
    }

    // El VM valida y persiste; solo si SaveAsync devuelve true se cierra con éxito.
    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OrderFormViewModel vm && await vm.SaveAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}