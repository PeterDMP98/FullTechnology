// INavigationService.cs — Contrato de navegación que consume el Shell: expone la página actual, un evento de cambio y NavigateTo(key)/GoBack(). Lo implementa NavigationService (cache + IRefreshable).
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Services;

/// <summary>
/// Navegación de módulos dentro del shell. El Shell solo conoce esta interfaz
/// (no depende de NavigationService concreto), así los diálogos y vistas pueden
/// navegar sin acoplarse a la implementación.
/// </summary>
public interface INavigationService
{
    /// <summary>ViewModel de la página actualmente visible (null antes de la primera navegación).</summary>
    ViewModelBase? Current { get; }

    /// <summary>Se dispara cuando cambia la página activa (el Shell se suscribe para actualizar la UI).</summary>
    event Action? CurrentChanged;

    /// <summary>Navega a la página identificada por su clave (ver NavKeys).</summary>
    void NavigateTo(string key);
}