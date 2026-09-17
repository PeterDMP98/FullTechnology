// HistorialVentaViewModelTests.cs — Pruebas del historial de ventas: periodos, medios de pago, búsqueda y detalle de factura.

using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel de historial de ventas: carga, filtros y detalle de factura.</summary>
public class HistorialVentaViewModelTests
{
    private readonly TestHarness _harness = new();

    private async Task<HistorialVentaViewModel> RecargadoAsync(HistorialVentaViewModel vm)
    {
        await TestHelpers.WaitUntil(() => !vm.IsLoading && vm.ResumenText.Length > 0);
        return vm;
    }

    [Fact]
    public async Task Ctor_SeCarga_ConDataVacia()
    {
        var vm = await RecargadoAsync(_harness.CreateHistorialVentaVm());

        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.Rows);
        Assert.Equal($"Total: {_harness.CurrencySvc.Fmt(0)}", vm.TotalText);
        Assert.NotEmpty(vm.ResumenText);
    }

    [Fact]
    public async Task ListaVentas_ConTotalesDelPeriodo()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy.AddHours(9), "Efectivo", 5000, "Ana Lopez", ("Funda", 1, 6000));
        _harness.SeedVenta(hoy.AddHours(10), "Nequi", 3000, "Beto Torres", ("Cargador", 1, 3000));

        var vm = await RecargadoAsync(_harness.CreateHistorialVentaVm());

        Assert.False(vm.IsEmpty);
        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal($"Total: {_harness.CurrencySvc.Fmt(8000)}", vm.TotalText);
        Assert.Equal("Nequi", vm.Rows[0].Medio); // más reciente primero
        Assert.Equal(_harness.CurrencySvc.Fmt(3000), vm.Rows[0].Total);
        Assert.Equal("Efectivo", vm.Rows[1].Medio);
        Assert.Equal(_harness.CurrencySvc.Fmt(5000), vm.Rows[1].Total);
        Assert.Equal("FV-1000", vm.Rows[1].Factura);
    }

    [Fact]
    public async Task FiltroMedio_SoloCuentaEseMedio()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));
        _harness.SeedVenta(hoy, "Nequi", 3000, "Beto", ("Cable", 1, 3000));

        var vm = await RecargadoAsync(_harness.CreateHistorialVentaVm());
        vm.MetodoIndex = 2; // Nequi
        await RecargadoAsync(vm);

        var row = Assert.Single(vm.Rows);
        Assert.Equal("Nequi", row.Medio);
        Assert.Equal($"Total: {_harness.CurrencySvc.Fmt(3000)}", vm.TotalText);
    }

    [Fact]
    public async Task Busqueda_PorClienteYFactura()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana Lopez", ("Funda", 1, 5000));
        _harness.SeedVenta(hoy, "Efectivo", 3000, "Beto Torres", ("Cargador", 1, 3000));

        var vm = await RecargadoAsync(_harness.CreateHistorialVentaVm());
        vm.SearchText = "ana";
        await RecargadoAsync(vm);

        Assert.Single(vm.Rows);
        Assert.Equal("Ana Lopez", vm.Rows[0].Cliente);

        vm.SearchText = "FV-1000";
        await RecargadoAsync(vm);
        Assert.Single(vm.Rows);
        Assert.Equal("FV-1000", vm.Rows[0].Factura);
    }

    [Fact]
    public async Task RangoFechas_FiltraElPeriodo()
    {
        _harness.SeedVenta(DateTime.Today, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));
        _harness.SeedVenta(DateTime.Today.AddDays(-10), "Efectivo", 9000, "Vieja", ("Cable", 1, 9000));

        var vm = await RecargadoAsync(_harness.CreateHistorialVentaVm());
        Assert.Equal(2, vm.Rows.Count);

        vm.FechaDesde = DateTime.Today.AddDays(-3);
        vm.FechaHasta = DateTime.Today;
        await RecargadoAsync(vm);

        Assert.Single(vm.Rows);
        Assert.Equal($"Total: {_harness.CurrencySvc.Fmt(5000)}", vm.TotalText);
    }

    [Fact]
    public async Task Ver_MuestraDetalleDeFactura()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana Lopez", ("Funda", 1, 6000));

        var vm = await RecargadoAsync(_harness.CreateHistorialVentaVm());
        var row = Assert.Single(vm.Rows);

        vm.VerCommand.Execute(row);

        var msg = Assert.Single(_harness.Dialogs.Messages);
        Assert.StartsWith("Detalle de venta:", msg);
        Assert.Contains("Factura: FV-1000", msg);
        Assert.Contains("TOTAL:", msg);
        Assert.Contains("Medio: Efectivo", msg);
    }
}