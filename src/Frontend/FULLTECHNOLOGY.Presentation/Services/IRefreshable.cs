// IRefreshable.cs — Marca a los ViewModels cuya pantalla depende de datos vivos que deben recargarse al volver a navegar a ellos (lo llama NavigationService al reutilizar un módulo cacheado). Evita que un módulo cacheado muestre datos obsoletos sin recrear la VM.
namespace FULLTECHNOLOGY.Presentation.Services;

/// <summary>
/// Marca a los ViewModels cuyo contenido depende de datos vivos y que deben
/// recargarse cada vez que se navega hasta ellos (cacheados en NavigationService).
/// </summary>
public interface IRefreshable
{
    /// <summary>Recarga los datos del módulo desde Application/Services.</summary>
    void Refresh();
}