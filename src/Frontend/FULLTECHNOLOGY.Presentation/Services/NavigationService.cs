// NavigationService.cs — Orquesta la navegación de módulos: cachea cada VM por su clave de navegación (no se pierde el estado del formulario al cambiar de módulo), recarga los IRefreshable al volver y notifica al Shell el cambio de página actual vía ShellPageFactory.
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Services;

public class NavigationService : INavigationService
{
    private readonly IShellPageFactory _factory;
    private readonly Dictionary<string, ViewModelBase> _cache = new();
    private ViewModelBase? _current;

    public NavigationService(IShellPageFactory factory)
    {
        _factory = factory;
    }

    public ViewModelBase? Current => _current;

    public event Action? CurrentChanged;

    public void NavigateTo(string key)
    {
        if (_cache.TryGetValue(key, out var existing))
        {
            // Ya se visitó este módulo: recargar datos vivos si el VM lo soporta
            // (patrón IRefreshable: la recarga no crea una VM nueva, se reutiliza).
            (existing as IRefreshable)?.Refresh();
            if (ReferenceEquals(existing, _current)) return;
            _current = existing;
            CurrentChanged?.Invoke();
            return;
        }

        var vm = _factory.Create(key);
        if (vm is null) return; // clave no navegable
        (vm as IRefreshable)?.Refresh(); // carga inicial de datos
        _cache[key] = vm;
        _current = vm;
        CurrentChanged?.Invoke();
    }
}