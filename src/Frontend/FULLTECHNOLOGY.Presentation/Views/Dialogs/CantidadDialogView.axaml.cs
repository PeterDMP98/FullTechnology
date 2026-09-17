// CantidadDialogView.axaml.cs — Diálogo de cantidad al agregar al carrito: el VM valida y guarda; la vista solo confirma o cancela.
using Avalonia.Controls;
using Avalonia.Interactivity;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class CantidadDialogView : Window
{
    public CantidadDialogView()
    {
        InitializeComponent();
    }

    // El VM ya validó la cantidad digitada; aquí solo se cierra con el resultado.
    private void OnAceptar(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}