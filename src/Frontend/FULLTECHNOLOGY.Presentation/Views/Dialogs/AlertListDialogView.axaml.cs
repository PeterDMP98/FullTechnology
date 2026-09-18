// AlertListDialogView.axaml.cs — Ventana flotante de detalle de alertas: cierra con el botón o Esc (sin lógica de negocio).
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class AlertListDialogView : Window
{
    public AlertListDialogView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}