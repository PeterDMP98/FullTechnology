// NavigationItem.cs — Modelo de un elemento de la barra de navegación del shell: clave, etiqueta, icono y estados visuales.
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// Un elemento del menú de navegación. La clave viene de NavKeys; el icono es el
// glifo asociado; IsActive marca el módulo actual y IsExpanded el modo del sidebar.
public partial class NavigationItem : ObservableObject
{
    public string Key { get; }
    public string Label { get; }
    public string Icon { get; }

    // Marca el ítem seleccionado (el módulo visible en ese momento).
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>
    /// Falso cuando el sidebar está colapsado (solo se muestran los íconos).
    /// </summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    public NavigationItem(string key, string? label = null)
    {
        Key = key;
        Label = label ?? key;
        Icon = NavKeys.GlyphFor(key);
    }
}