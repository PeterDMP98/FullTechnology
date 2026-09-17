// OrderHistoryView.axaml.cs — Diálogo informativo de una orden (solo lectura): se cierra con Ok y su VM se inicializa antes de mostrarla.
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class OrderHistoryView : Window
{
    public OrderHistoryView()
    {
        InitializeComponent();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close(true);
}