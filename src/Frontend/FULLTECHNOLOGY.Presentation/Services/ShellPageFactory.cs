// ShellPageFactory.cs — Fabrica que resuelve el ViewModel de cada pantalla del shell a partir de su clave de navegación.
using Microsoft.Extensions.DependencyInjection;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Services;

/// <summary>
/// Contrato para crear el ViewModel raíz de una pantalla a partir de la clave
/// de navegación (ver <see cref="NavKeys"/>).
/// </summary>
public interface IShellPageFactory
{
    /// <summary>
    /// Devuelve el ViewModel correspondiente a la clave o <c>null</c> si no se conoce.
    /// </summary>
    ViewModelBase? Create(string key);
}

/// <summary>
/// Resuelve el ViewModel de cada pantalla por clave de navegación.
/// En F6 todas las pantallas son PlaceholderViewModel; los módulos
/// reales (F9/F10) ampliarán este switch usando IServiceProvider.
/// </summary>
public class ShellPageFactory : IShellPageFactory
{
    private readonly IServiceProvider _services;

    public ShellPageFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ViewModelBase? Create(string key) => key switch
    {
        // "Inicio" es la única pantalla puramente estática: siempre placeholder.
        NavKeys.Inicio => new PlaceholderViewModel("Inicio", "Dashboard con métricas de mantenimiento."),
        // El resto delegan en los módulos reales resueltos por DI.
        NavKeys.Mantenimiento => CrearMantenimiento(),
        NavKeys.Ventas => CrearVenta(),
        NavKeys.Clientes => CrearClientes(),
        NavKeys.Inventario => CrearInventario(),
        NavKeys.Historial => CrearHistorialVenta(),
        NavKeys.Contable => CrearContable(),
        NavKeys.Configuracion => CrearConfiguracion(),
        _ => null
    };

    /// <summary>
    /// Módulo real (F9). Si no hay contenedor de DI (p. ej. en pruebas sin
    /// proveedor), cae en un placeholder para no romper la navegación.
    /// </summary>
    private ViewModelBase CrearMantenimiento()
    {
        var vm = _services?.GetService<MantenimientoViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Mantenimiento", "Órdenes de servicio: ingreso, seguimiento, cobros y entrega.");
    }

    private ViewModelBase CrearClientes()
    {
        // Patrón típico de todos los módulos: resolver por DI; si no hay
        // proveedor registrado, degradar a placeholder para no romper la navegación.
        var vm = _services?.GetService<ClientesViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Clientes", "Almacén de clientes y proveedores.");
    }

    private ViewModelBase CrearInventario()
    {
        var vm = _services?.GetService<InventarioViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Inventario", "Productos, stock y costos.");
    }

    private ViewModelBase CrearVenta()
    {
        var vm = _services?.GetService<VentaViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Ventas", "Venta de accesorios y facturación.");
    }

    private ViewModelBase CrearHistorialVenta()
    {
        var vm = _services?.GetService<HistorialVentaViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Historial", "Registro de todas las ventas con filtros.");
    }

    private ViewModelBase CrearContable()
    {
        var vm = _services?.GetService<AccountingViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Contable", "Cierre por periodo, balance y exportación.");
    }

    private ViewModelBase CrearConfiguracion()
    {
        var vm = _services?.GetService<ConfigurationViewModel>();
        return vm is not null
            ? vm
            : new PlaceholderViewModel("Configuración", "Configuración del negocio, moneda y tema.");
    }
}