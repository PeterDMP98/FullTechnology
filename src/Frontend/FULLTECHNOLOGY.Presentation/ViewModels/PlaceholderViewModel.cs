// PlaceholderViewModel.cs — Pantalla provisional con título y descripción para los módulos en construcción o sin servicio de DI.
namespace FULLTECHNOLOGY.Presentation.ViewModels;

/// <summary>Pantalla provisional mientras llegan los módulos reales (F9/F10).</summary>
public partial class PlaceholderViewModel : ViewModelBase
{
    public string Title { get; }
    public string Description { get; }

    public PlaceholderViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }
}