// MantenimientoViewModelTests.cs — Pruebas del módulo de mantenimiento (F9): búsqueda, filtros, orden, paginación y eliminación.

using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Mantenimiento;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel de mantenimiento: carga, búsqueda, tarjetas de estado y paginación.</summary>
public class MantenimientoViewModelTests
{
    private static async Task ReloadAsync(MantenimientoViewModel vm)
    {
        vm.Refresh();
        await TestHelpers.WaitUntil(() => !vm.IsLoading);
    }

    [Fact]
    public async Task Refresh_CargaFilasYEstadisticas()
    {
        var h = new TestHarness();
        h.SeedOrder("O-1", RepairStatuses.Recibido);
        h.SeedOrder("O-2", RepairStatuses.EnReparacion);
        h.SeedOrder("O-3", RepairStatuses.Reparado);
        h.SeedOrder("O-4", RepairStatuses.ListoParaEntregar);
        h.SeedOrder("O-5", RepairStatuses.Entregado);
        var vm = h.CreateVm();

        // Las tarjetas resumen los 5 estados: total, recibidos, en repara, listos y entregados.
        await ReloadAsync(vm);

        Assert.Equal(5, vm.Rows.Count);
        Assert.Equal("5", vm.Cards[0].Value);
        Assert.Equal("1", vm.Cards[1].Value);
        Assert.Equal("1", vm.Cards[2].Value);
        Assert.Equal("2", vm.Cards[3].Value);
        Assert.Equal("1", vm.Cards[4].Value);
    }

    [Fact]
    public async Task Busqueda_CoincidePorNombreYImei()
    {
        var h = new TestHarness();
        h.SeedOrder("O-1", RepairStatuses.Recibido, customer: "Ana Gomez");
        h.SeedOrder("O-2", RepairStatuses.Recibido, customer: "Luis Perez", imei: "356938035643809");
        var vm = h.CreateVm();
        await ReloadAsync(vm);

        vm.SearchText = "gomez";
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1 && !vm.IsLoading);
        Assert.Equal("O-1", vm.Rows[0].OrderNumber);

        vm.SearchText = "035643809";
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1 && !vm.IsLoading);
        Assert.Equal("O-2", vm.Rows[0].OrderNumber);
    }

    [Fact]
    public async Task CardListos_FiltraSoloReparadoYListoParaEntregar()
    {
        var h = new TestHarness();
        h.SeedOrder("O-1", RepairStatuses.Recibido);
        h.SeedOrder("O-2", RepairStatuses.Reparado);
        h.SeedOrder("O-3", RepairStatuses.ListoParaEntregar);
        h.SeedOrder("O-4", RepairStatuses.EnReparacion);
        var vm = h.CreateVm();
        await ReloadAsync(vm);

        vm.Cards[3].SelectCommand.Execute(null);
        await TestHelpers.WaitUntil(() => !vm.IsLoading);

        Assert.Equal(2, vm.Rows.Count);
        Assert.All(vm.Rows, r => Assert.True(r.Status is RepairStatuses.Reparado or RepairStatuses.ListoParaEntregar));
        Assert.True(vm.Cards[3].IsSelected);
        Assert.Equal(StatusFilters.Listos, vm.SelectedStatus!.Value);
    }

    [Fact]
    public async Task OrdenPredeterminado_PorIngresoDescendente()
    {
        var h = new TestHarness();
        var t1 = new DateTime(2026, 1, 1, 9, 0, 0);
        var t2 = new DateTime(2026, 2, 1, 9, 0, 0);
        var t3 = new DateTime(2026, 3, 1, 9, 0, 0);
        h.SeedOrder("O-1", RepairStatuses.Recibido, receivedAt: t1);
        h.SeedOrder("O-2", RepairStatuses.Recibido, receivedAt: t2);
        h.SeedOrder("O-3", RepairStatuses.Recibido, receivedAt: t3);
        var vm = h.CreateVm();

        Assert.Equal(SortColumn.Ingreso, vm.SortColumn);
        Assert.Contains("▼", vm.ArrowIngreso); // descendente por defecto

        await ReloadAsync(vm);

        Assert.Equal("O-3", vm.Rows[0].OrderNumber);
        Assert.Equal("O-1", vm.Rows[2].OrderNumber);
    }

    [Fact]
    public async Task SortPorCliente_AlternaAscendenteYDescendente()
    {
        var h = new TestHarness();
        h.SeedOrder("O-B", RepairStatuses.Recibido, customer: "Beta");
        h.SeedOrder("O-A", RepairStatuses.Recibido, customer: "Alpha");
        h.SeedOrder("O-C", RepairStatuses.Recibido, customer: "Charlie");
        var vm = h.CreateVm();
        await ReloadAsync(vm);

        vm.SortByColumnCommand.Execute(SortColumn.Cliente);
        Assert.True(vm.SortAscending);
        Assert.Equal("Alpha", vm.Rows[0].Cliente);
        Assert.Contains("▲", vm.ArrowCliente);

        vm.SortByColumnCommand.Execute(SortColumn.Cliente);
        Assert.False(vm.SortAscending);
        Assert.Equal("Charlie", vm.Rows[0].Cliente);
        Assert.Contains("▼", vm.ArrowCliente);
    }

    [Fact]
    public async Task Paginacion_25OrdenesEnPaginasDe10()
    {
        var h = new TestHarness();
        for (var i = 1; i <= 25; i++)
            h.SeedOrder($"O-{i:00}", RepairStatuses.Recibido, receivedAt: DateTime.Today.AddMinutes(i));
        var vm = h.CreateVm();
        await ReloadAsync(vm);

        Assert.Equal(25, vm.TotalRows);
        Assert.Equal(3, vm.TotalPages);
        Assert.Equal(10, vm.Rows.Count);
        Assert.True(vm.HasPrev == false && vm.HasNext);

        vm.NextPageCommand.Execute(null);
        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal("O-15", vm.Rows[0].OrderNumber);
        Assert.Equal(10, vm.Rows.Count);

        vm.PrevPageCommand.Execute(null);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal("O-25", vm.Rows[0].OrderNumber);

        vm.GoToPageCommand.Execute(3);
        Assert.Equal(3, vm.CurrentPage);
        Assert.Equal(5, vm.Rows.Count);
    }

    [Fact]
    public async Task Eliminar_Confirmado_BorraYRecarga()
    {
        var h = new TestHarness();
        h.SeedOrder("O-1", RepairStatuses.Recibido);
        h.SeedOrder("O-2", RepairStatuses.Recibido);
        h.SeedOrder("O-3", RepairStatuses.Recibido);
        var vm = h.CreateVm();
        await ReloadAsync(vm);

        vm.EliminarCommand.Execute(vm.Rows[0]);
        await TestHelpers.WaitUntil(() => !vm.IsLoading);

        Assert.Equal(2, vm.Rows.Count);
        Assert.DoesNotContain(vm.Rows, r => r.OrderNumber == "O-3");
    }

    [Fact]
    public async Task Eliminar_NoConfirmado_MantieneFilas()
    {
        var h = new TestHarness { Dialogs = { ConfirmResult = false } };
        h.SeedOrder("O-1", RepairStatuses.Recibido);
        h.SeedOrder("O-2", RepairStatuses.Recibido);
        var vm = h.CreateVm();
        await ReloadAsync(vm);

        vm.EliminarCommand.Execute(vm.Rows[0]);
        await TestHelpers.WaitUntil(() => !vm.IsLoading);

        Assert.Equal(2, vm.Rows.Count);
    }
}