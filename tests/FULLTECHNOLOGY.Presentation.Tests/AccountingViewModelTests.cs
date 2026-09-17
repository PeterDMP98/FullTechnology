// AccountingViewModelTests.cs — Pruebas del módulo contable: vistas de movimientos, tipo de ingreso y por método, balance, filtros y exportación.

using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel contable: resúmenes por fecha, métodos, balance y exportación.</summary>
public class AccountingViewModelTests
{
    private readonly TestHarness _harness = new();

    private async Task<AccountingViewModel> RecargadoAsync(AccountingViewModel vm)
    {
        await TestHelpers.WaitUntil(() => !vm.IsLoading && vm.ColumnCount > 0);
        return vm;
    }

    [Fact]
    public async Task Ctor_SeCarga_ConDataVacia()
    {
        var vm = _harness.CreateAccountingVm();
        await RecargadoAsync(vm);

        Assert.True(vm.IsEmpty);
        Assert.Equal(_harness.CurrencySvc.Fmt(0), vm.VentasText);
        Assert.Equal(_harness.CurrencySvc.Fmt(0), vm.CobrosText);
        Assert.Equal(_harness.CurrencySvc.Fmt(0), vm.TotalText);
        Assert.Empty(vm.Rows);
        Assert.Equal(7, vm.ColumnCount);
    }

    [Fact]
    public async Task VistaMovimientos_ListaVentasYCobros_OrdenadosPorFecha()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy.AddHours(9), "Efectivo", 5000, "Ana", ("Funda iPhone", 1, 3000), ("Pelicula", 1, 2000));
        _harness.SeedVenta(hoy.AddHours(10), "Nequi", 3000, "Beto", ("Cargador", 1, 3000));
        _harness.SeedCobro(hoy.AddHours(11), "Transferencia", 2000, "OS-100", cliente: "Carla");

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());

        Assert.False(vm.IsEmpty);
        Assert.Equal(3, vm.Rows.Count);
        Assert.Equal(7, vm.ColumnCount);
        Assert.Equal(_harness.CurrencySvc.Fmt(8000), vm.VentasText);
        Assert.Equal(_harness.CurrencySvc.Fmt(2000), vm.CobrosText);
        Assert.Equal(_harness.CurrencySvc.Fmt(10000), vm.TotalText);
        Assert.Equal("2 accesorios · 1 mantenimiento", vm.FacturasText);
        Assert.Contains(vm.ExportRows, r => r[0].StartsWith("TOTAL INGRESOS"));
        Assert.False(vm.Rows[0].EsTotal); // primero el movimiento más reciente
    }

    [Fact]
    public async Task VistaTipoIngreso_ResumeCategorias_YResaltaTotal()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));
        _harness.SeedVenta(hoy, "Daviplata", 1000, "Beto", ("Cable", 1, 1000));
        _harness.SeedCobro(hoy, "Nequi", 2000, "OS-1");

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());
        vm.VistaIndex = 1;
        await RecargadoAsync(vm);

        Assert.Equal(new[] { "Tipo de ingreso", "Efectivo", "Nequi", "Otros", "Total" }, vm.Columns);
        Assert.Equal(3, vm.Rows.Count);
        var total = vm.Rows[^1];
        Assert.True(total.EsTotal);
        Assert.Equal("TOTAL DEL PERIODO", total.Cells[0].Value);
    }

    [Fact]
    public async Task VistaPorMetodo_AgrupaPorMedioPago()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));
        _harness.SeedVenta(hoy, "Nequi", 3000, "Beto", ("Cable", 1, 3000));
        _harness.SeedCobro(hoy, "Efectivo", 1000, "OS-1");

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());
        vm.VistaIndex = 2;
        await RecargadoAsync(vm);

        Assert.Equal(4, vm.ColumnCount);
        Assert.Contains(vm.Rows, r => r.Cells[0].Value == "Efectivo");
        Assert.Contains(vm.Rows, r => r.Cells[0].Value == "Nequi");
        Assert.Contains(vm.Rows, r => r.Cells[0].Value == "TOTAL");
        Assert.True(vm.Rows[^1].EsTotal);
    }

    [Fact]
    public async Task FiltroMetodo_SoloCuentaEseMedio()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));
        _harness.SeedVenta(hoy, "Nequi", 3000, "Beto", ("Cable", 1, 3000));

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());
        vm.MetodoIndex = 1; // Efectivo
        await RecargadoAsync(vm);

        Assert.Equal(_harness.CurrencySvc.Fmt(5000), vm.VentasText);
        Assert.Equal(_harness.CurrencySvc.Fmt(0), vm.CobrosText);
        Assert.Single(vm.Rows);
    }

    [Fact]
    public async Task Busqueda_FiltraPorClienteYProducto()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana Lopez", ("Funda iPhone", 1, 5000));
        _harness.SeedVenta(hoy, "Efectivo", 3000, "Beto Torres", ("Cargador", 1, 3000));

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());
        vm.SearchText = "ana";
        await RecargadoAsync(vm);

        Assert.Equal(_harness.CurrencySvc.Fmt(5000), vm.TotalText);
        Assert.Single(vm.Rows);

        vm.SearchText = "cargador";
        await RecargadoAsync(vm);
        Assert.Equal(_harness.CurrencySvc.Fmt(3000), vm.TotalText);
    }

    [Fact]
    public async Task RangoFechas_FiltraElPeriodo()
    {
        _harness.SeedVenta(DateTime.Today, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));
        _harness.SeedVenta(DateTime.Today.AddDays(-10), "Efectivo", 9000, "Vieja", ("Cable", 1, 9000));

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());
        Assert.Equal(_harness.CurrencySvc.Fmt(5000), vm.VentasText);

        vm.FechaDesde = DateTime.Today.AddDays(-1);
        vm.FechaHasta = DateTime.Today.AddDays(1);
        await RecargadoAsync(vm);
        Assert.Equal(_harness.CurrencySvc.Fmt(5000), vm.VentasText);

        vm.FechaDesde = DateTime.Today.AddDays(-11);
        vm.FechaHasta = DateTime.Today.AddDays(-5);
        await RecargadoAsync(vm);
        Assert.Equal(_harness.CurrencySvc.Fmt(9000), vm.VentasText);
    }

    [Fact]
    public async Task Balance_ConstruyeSeries_ConRangoFechas()
    {
        var d = DateTime.Today;
        _harness.AccountingRepo.BalanceData = new List<(DateTime, decimal, decimal)>
        {
            (d.AddDays(-1), 100, 50),
            (d, 5000, 2000)
        };

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());
        vm.FechaDesde = d.AddDays(-2);
        vm.FechaHasta = d;
        await RecargadoAsync(vm);

        Assert.Equal(2, vm.Series.Length);
        Assert.Single(vm.XAxes);
        Assert.Equal(2, vm.XAxes[0].Labels!.Count);
        Assert.Equal(3, _harness.AccountingRepo.BalanceSeriesCallCount);
    }

    [Fact]
    public async Task Export_PobladoParaDescarga()
    {
        var hoy = DateTime.Today;
        _harness.SeedVenta(hoy, "Efectivo", 5000, "Ana", ("Funda", 1, 5000));

        var vm = await RecargadoAsync(_harness.CreateAccountingVm());

        Assert.Equal("CIERRE CONTABLE", vm.ExportTitle);
        Assert.True(vm.ExportHeaders.Length > 0);
        Assert.True(vm.ExportWidths.Length > 0);
        Assert.NotEmpty(vm.ExportRows);
        Assert.StartsWith("Cierre_", vm.FileNameBase);
        Assert.NotEmpty(vm.PeriodoText);
    }
}