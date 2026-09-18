// OrderFormView.axaml.cs — Diálogo de crear/editar orden de mantenimiento: el VM guarda y la vista cierra solo si fue exitoso;
// al abrir ajusta el alto al tamaño de la pantalla restando un margen para que nunca sobresalga.
using System;
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

    // La ventana se abre a 820px por diseño; si la pantalla es más baja se reduce el alto
    // (WorkingArea en px físicos → DIPs con el Scaling) dejando siempre un margen de 40.
    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;
        var scale = screen.Scaling > 0 ? screen.Scaling : 1;
        var usableHeight = screen.WorkingArea.Height / scale;
        if (Height > usableHeight - 40) Height = usableHeight - 40;
    }

    // El VM valida y persiste; solo si SaveAsync devuelve true se cierra con éxito.
    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OrderFormViewModel vm && await vm.SaveAsync())
            Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}