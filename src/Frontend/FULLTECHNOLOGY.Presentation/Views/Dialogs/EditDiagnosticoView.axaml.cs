// EditDiagnosticoView.axaml.cs — Diálogo de edición de diagnóstico: el VM guarda los cambios y determina si se cierra con éxito.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class EditDiagnosticoView : Window
{
    public EditDiagnosticoView()
    {
        InitializeComponent();
    }

    // El VM valida y guarda; solo si SaveAsync devuelve true se cierra con éxito.
    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditDiagnosticoViewModel vm && await vm.SaveAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}