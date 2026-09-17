// VentaViewModelTests.cs — Pruebas del módulo de venta (F8): catálogo, carrito, cantidades, descuento y compra.

using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Venta;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel de venta: catálogo de accesorios, carrito, stock y cálculo de totales.</summary>
public class VentaViewModelTests
{
    private readonly TestHarness _h = new();

    private async Task<VentaViewModel> BuildVmAsync(string search = "")
    {
        var vm = _h.CreateVentaVm();
        if (search.Length > 0) vm.SearchText = search;
        vm.Refresh();
        await TestHelpers.WaitUntil(() => !vm.IsLoading);
        return vm;
    }

    [Fact]
    public async Task Catalogo_MuestraSoloAccesorios()
    {
        _h.SeedProducto("Protector", ProductTypes.Accesorio, stock: 5, costo: 5m);
        _h.SeedProducto("Cable USB", ProductTypes.Accesorio, stock: 3);
        _h.SeedProducto("Bateria", ProductTypes.Repuesto, stock: 2);

        var vm = await BuildVmAsync();

        Assert.Equal(2, vm.ProductsCount);
        Assert.All(vm.Rows, r => Assert.Equal(ProductTypes.Accesorio, r.Producto.Tipo));
    }

    [Fact]
    public async Task Busqueda_FiltraCatalogo()
    {
        _h.SeedProducto("Protector Vidrio", ProductTypes.Accesorio, stock: 5);
        _h.SeedProducto("Cargador Rapido", ProductTypes.Accesorio, stock: 3);

        var vm = await BuildVmAsync("vidrio");

        Assert.Single(vm.Rows);
        Assert.Equal("Protector Vidrio", vm.Rows[0].Nombre);
    }

    [Fact]
    public void SinBusquedaYConStock_CatalogoVacio_MarcaVacio()
    {
        // Sin productos sembrados ni búsqueda: el carrito arranca vacío y el total en 0 COP.
        var vm = _h.CreateVentaVm();
        Assert.False(vm.HasCartItems);
        Assert.Equal("0 COP", vm.TotalText);
    }

    [Fact]
    public async Task AgregarProducto_RecalculaTotales()
    {
        var p = _h.SeedProducto("Protector", ProductTypes.Accesorio, stock: 3, costo: 5m);
        p.PrecioVenta = 1000m;

        var vm = await BuildVmAsync();

        Assert.Equal("1.000 COP", _h.CurrencySvc.Fmt(1000m));
        Assert.True(vm.TryAgregar(p, 2));
        Assert.True(vm.HasCartItems);
        Assert.Equal("Ítems: 1", vm.ItemsText);
        Assert.Equal("2.000 COP", vm.SubtotalText);
        Assert.Equal("2.000 COP", vm.TotalText);

        Assert.True(vm.TryAgregar(p, 1));
        Assert.Equal(3, vm.CartItems.Single().Cantidad);
        Assert.Equal("3.000 COP", vm.SubtotalText);
    }

    [Fact]
    public async Task AgregarProducto_RespetaStockMaximo()
    {
        var p = _h.SeedProducto("Cable", ProductTypes.Accesorio, stock: 2);
        p.PrecioVenta = 500m;

        var vm = await BuildVmAsync();
        Assert.True(vm.TryAgregar(p, 2));

        var extra = vm.TryAgregar(p, 1);

        Assert.False(extra);
        Assert.Equal(2, vm.CartItems.Single().Cantidad);
        Assert.Contains(_h.Dialogs.Messages, m => m.Contains("máxima del stock"));
    }

    [Fact]
    public async Task CambiarCantidad_MasRespetaStock()
    {
        var p = _h.SeedProducto("Tapa", ProductTypes.Accesorio, stock: 2);
        p.PrecioVenta = 300m;

        var vm = await BuildVmAsync();
        vm.TryAgregar(p, 1);

        vm.ChangeCartQuantity(p, +1);
        vm.ChangeCartQuantity(p, +1);

        Assert.Equal(2, vm.CartItems.Single().Cantidad);
    }

    [Fact]
    public async Task CambiarCantidad_MenosNoBajaDeUno()
    {
        var p = _h.SeedProducto("Tapa", ProductTypes.Accesorio, stock: 5);
        p.PrecioVenta = 300m;

        var vm = await BuildVmAsync();
        vm.TryAgregar(p, 1);

        vm.ChangeCartQuantity(p, -1);
        vm.ChangeCartQuantity(p, -1);

        Assert.Equal(1, vm.CartItems.Single().Cantidad);
    }

    [Fact]
    public async Task QuitarItem_VaciaElCarrito()
    {
        var p = _h.SeedProducto("Tapa", ProductTypes.Accesorio, stock: 5);
        p.PrecioVenta = 300m;

        var vm = await BuildVmAsync();
        vm.TryAgregar(p, 2);

        vm.RemoveCartItem(p.Id);

        Assert.Empty(vm.CartItems);
        Assert.False(vm.HasCartItems);
        Assert.Equal("Ítems: 0", vm.ItemsText);
    }

    [Fact]
    public async Task DescuentoActivo_ReduceElTotal()
    {
        var p = _h.SeedProducto("Protector", ProductTypes.Accesorio, stock: 5);
        p.PrecioVenta = 1000m;

        var vm = await BuildVmAsync();
        vm.TryAgregar(p, 3);

        vm.DescuentoActivo = true;
        vm.DescuentoValue = 500m;

        Assert.Equal("3.000 COP", vm.SubtotalText);
        Assert.Equal("500 COP", vm.DescuentoText);
        Assert.Equal("2.500 COP", vm.TotalText);

        vm.DescuentoActivo = false;
        Assert.Equal(0m, vm.DescuentoValue);
        Assert.Equal("3.000 COP", vm.TotalText);
    }

    [Fact]
    public async Task Comprar_ConCarritoVacio_MuestraMensaje()
    {
        var vm = await BuildVmAsync();
        vm.ComprarCommand.Execute(null);

        await TestHelpers.WaitUntil(() => _h.Dialogs.Messages.Any(m => m.Contains("carrito está vacío")));
        Assert.Empty(_h.Ventas.Ventas);
    }
}