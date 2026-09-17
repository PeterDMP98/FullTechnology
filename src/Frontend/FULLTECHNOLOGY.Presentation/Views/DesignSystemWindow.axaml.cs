// DesignSystemWindow.axaml.cs — Ventana de muestra del sistema de diseño: crea su propio VM de demostración.
using Avalonia.Controls;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class DesignSystemWindow : Window
{
    public DesignSystemWindow()
    {
        InitializeComponent();
        // Ventana autocontenida: genera su DataContext sin DI (solo datos estáticos de demo).
        DataContext = new DesignSystemViewModel();
    }
}