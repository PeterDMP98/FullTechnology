// NavigationServiceTests.cs — Pruebas de navegación entre módulos y de la creación de VMs desde la fábrica de páginas.

using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del servicio de navegación y de la fábrica de páginas con y sin DI.</summary>
public class NavigationServiceTests
{
    private readonly IShellPageFactory _factory;
    private readonly INavigationService _nav;

    public NavigationServiceTests()
    {
        _factory = new ShellPageFactory(null!);
        _nav = new NavigationService(_factory);
    }

    [Fact]
    public void AllNavKeys_DevuelvenVMValido()
    {
        foreach (var key in NavKeys.All)
        {
            var vm = _factory.Create(key);
            Assert.NotNull(vm);
            if (vm is PlaceholderViewModel p)
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Title));
                Assert.False(string.IsNullOrWhiteSpace(p.Description));
            }
        }
    }

    [Fact]
    public void Mantenimiento_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<MantenimientoViewModel>(factory.Create(NavKeys.Mantenimiento));
    }

    [Fact]
    public void Clientes_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<ClientesViewModel>(factory.Create(NavKeys.Clientes));
    }

    [Fact]
    public void Inventario_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<InventarioViewModel>(factory.Create(NavKeys.Inventario));
    }

    [Fact]
    public void Venta_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<VentaViewModel>(factory.Create(NavKeys.Ventas));
    }

    [Fact]
    public void Contable_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<AccountingViewModel>(factory.Create(NavKeys.Contable));
    }

    [Fact]
    public void Historial_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<HistorialVentaViewModel>(factory.Create(NavKeys.Historial));
    }

    [Fact]
    public void Configuracion_ConDI_DevuelveModuloReal()
    {
        var harness = new TestHarness();
        var factory = new ShellPageFactory(harness.Services);
        Assert.IsType<ConfigurationViewModel>(factory.Create(NavKeys.Configuracion));
    }

    [Fact]
    public void NavigateTo_Inicio_CambiaCurrent()
    {
        var changed = 0;
        _nav.CurrentChanged += () => changed++;
        _nav.NavigateTo(NavKeys.Inicio);
        Assert.NotNull(_nav.Current);
        Assert.Equal("Inicio", ((PlaceholderViewModel)_nav.Current!).Title);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void NavigateTo_MismaClave_NoRecarga()
    {
        _nav.NavigateTo(NavKeys.Inicio);
        var first = _nav.Current;
        var changed = 0;
        _nav.CurrentChanged += () => changed++;
        _nav.NavigateTo(NavKeys.Inicio);
        Assert.Same(first, _nav.Current);
        Assert.Equal(0, changed);
    }

    [Fact]
    public void NavigateTo_ClaveDesconocida_MantieneActual()
    {
        _nav.NavigateTo(NavKeys.Inicio);
        var before = _nav.Current;
        _nav.NavigateTo("NoExiste");
        Assert.Same(before, _nav.Current);
        Assert.NotNull(_nav.Current);
    }

    [Fact]
    public void NavigateTo_CambiaDeModulo()
    {
        var harness = new TestHarness();
        var nav = new NavigationService(new ShellPageFactory(harness.Services));
        nav.NavigateTo(NavKeys.Configuracion);
        Assert.IsType<ConfigurationViewModel>(nav.Current);
    }
}