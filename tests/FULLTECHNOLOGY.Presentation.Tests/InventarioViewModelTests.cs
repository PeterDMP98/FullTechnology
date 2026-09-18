// InventarioViewModelTests.cs — Pruebas del módulo de inventario: catálogo, filtros, búsqueda por nombre/código/proveedor y alta/edición.

using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel de inventario: listado, filtros, búsqueda y gestión de productos.</summary>
public class InventarioViewModelTests
{
    private readonly TestHarness _h = new();

    /// <summary>Crea un VM (con filtros opcionales) y espera la carga inicial.</summary>
    private async Task<InventarioViewModel> BuildVmAsync(string? tipo = null, string search = "")
    {
        var vm = _h.CreateInventarioVm();
        if (tipo is not null) vm.SelectedTipo = tipo;
        if (search.Length > 0) vm.SearchText = search;
        vm.Refresh();
        await TestHelpers.WaitUntil(() => !vm.IsLoading);
        return vm;
    }

    [Fact]
    public async Task SinFiltros_MuestraTodosLosProductos()
    {
        _h.SeedProducto("Bateria A", codigo: "PRD-A");
        _h.SeedProducto("Cable USB", codigo: "PRD-B");

        var vm = await BuildVmAsync();

        Assert.Equal(2, vm.Rows.Count);
        Assert.Equal(2, vm.TotalRows);
        Assert.False(vm.IsEmpty);
        Assert.Equal("PRD-A", vm.Rows[0].Codigo);
    }

    [Fact]
    public async Task FiltroPorTipo_DevuelveSoloEseTipo()
    {
        _h.SeedProducto("Bateria A", ProductTypes.Repuesto);
        _h.SeedProducto("Protector", ProductTypes.Accesorio);

        var vm = await BuildVmAsync(ProductTypes.Repuesto);

        var row = Assert.Single(vm.Rows);
        Assert.Equal("Bateria A", row.Nombre);
        Assert.Equal(ProductTypes.Repuesto, row.Tipo);
    }

    [Fact]
    public async Task BuscarPorNombre_ReduceResultados()
    {
        _h.SeedProducto("Bateria Sprint", ProductTypes.Repuesto);
        _h.SeedProducto("Cargador Sprint", ProductTypes.Accesorio);
        _h.SeedProducto("Correa de reloj", ProductTypes.Accesorio);

        var vm = await BuildVmAsync(search: "Sprint");

        Assert.Equal(2, vm.Rows.Count);
    }

    [Fact]
    public async Task BuscarPorCodigo_EncuentraUno()
    {
        _h.SeedProducto("Bateria Red", codigo: "PRD-2026-000001");
        _h.SeedProducto("Correa Red", codigo: "PRD-2026-000002");

        var vm = await BuildVmAsync(search: "000002");

        var row = Assert.Single(vm.Rows);
        Assert.Equal("Correa Red", row.Nombre);
    }

    [Fact]
    public async Task BuscarPorProveedor_Encuentra()
    {
        _h.SeedProducto("Aro", proveedor: "Taller Garcia");
        _h.SeedProducto("Tapa", proveedor: "Elias Import");

        var vm = await BuildVmAsync(search: "Garcia");

        var row = Assert.Single(vm.Rows);
        Assert.Equal("Aro", row.Nombre);
    }

    [Fact]
    public async Task SinResultados_MarcaVacio()
    {
        _h.SeedProducto("Bateria A");

        var vm = await BuildVmAsync(search: "inexistente");

        Assert.Empty(vm.Rows);
        Assert.True(vm.IsEmpty);
        Assert.Equal(0, vm.TotalRows);
    }

    [Fact]
    public async Task Eliminar_ConfirmaYRefresca()
    {
        var p = _h.SeedProducto("Bateria A");

        var vm = await BuildVmAsync();
        var row = vm.Rows.Single(r => r.Producto.Id == p.Id);
        row.EliminarCommand.Execute(null);

        await TestHelpers.WaitUntil(() => vm.Rows.Count == 0);
        Assert.Null(_h.InventorySvc.Get(p.Id));
    }

    [Fact]
    public async Task Eliminar_CanceladoMantieneProducto()
    {
        var p = _h.SeedProducto("Bateria A");
        _h.Dialogs.ConfirmResult = false;

        var vm = await BuildVmAsync();
        vm.Rows.Single().EliminarCommand.Execute(null);

        await Task.Delay(50);
        Assert.Single(vm.Rows);
        Assert.NotNull(_h.InventorySvc.Get(p.Id));
    }

    [Fact]
    public async Task GuardarNuevo_GeneraCodigoAutomatico()
    {
        var dialog = _h.Services.GetRequiredService<ProductoEditDialogViewModel>();
        dialog.Initialize(null);
        dialog.Nombre = "Cable USB";
        dialog.SelectedTipo = ProductTypes.Accesorio;
        dialog.Costo = 10m;
        dialog.PrecioVenta = 25m;
        dialog.Stock = 3;

        var result = await dialog.SaveAsync();

        // El código PRD se genera automáticamente si no se indicó (replica el SQL real).
        Assert.True(result);
        Assert.NotNull(dialog.Saved);
        Assert.StartsWith("PRD-", dialog.Saved.Codigo);
        Assert.Single(_h.InventorySvc.Search());
    }

    [Fact]
    public async Task Guardar_SinNombreFallaYNoPersiste()
    {
        var dialog = _h.Services.GetRequiredService<ProductoEditDialogViewModel>();
        dialog.Initialize(null);
        dialog.Nombre = "  ";

        var result = await dialog.SaveAsync();

        Assert.False(result);
        Assert.Empty(_h.InventorySvc.Search());
        Assert.Contains(_h.Dialogs.Messages, m => m.Contains("obligatorio"));
    }

    [Fact]
    public async Task EditarExistente_CambiaYPersiste()
    {
        var p = _h.SeedProducto("Bateria Vieja", ProductTypes.Repuesto, codigo: "PRD-K", stock: 5);

        var dialog = _h.Services.GetRequiredService<ProductoEditDialogViewModel>();
        dialog.Initialize(p);
        dialog.Nombre = "Bateria Pro Max";
        dialog.Stock = 7;

        var result = await dialog.SaveAsync();

        Assert.True(result);
        var updated = _h.InventorySvc.Get(p.Id)!;
        Assert.Equal("Bateria Pro Max", updated.Nombre);
        Assert.Equal(7, updated.Stock);
        Assert.Equal("PRD-K", updated.Codigo);
    }

    [Fact]
    public async Task AplicarImportacion_AltaNuevaYActualizaExistente()
    {
        _h.SeedProducto("Bateria A", codigo: "PRD-A", stock: 5);
        var vm = await BuildVmAsync();

        var import = new[]
        {
            new Producto { Codigo = "PRD-B", Nombre = "Cargador USB", Tipo = ProductTypes.Accesorio, Costo = 8m, PrecioVenta = 20m, Stock = 4, FechaIngreso = DateTime.Today },
            new Producto { Codigo = "PRD-A", Nombre = "Bateria Pro", Tipo = ProductTypes.Repuesto, Costo = 15m, PrecioVenta = 40m, Stock = 9, FechaIngreso = DateTime.Today }
        };

        var (nuevos, actualizados) = vm.AplicarImportacion(import);
        await TestHelpers.WaitUntil(() => !vm.IsLoading);

        // El código existente se actualiza y el nuevo se da de alta.
        Assert.Equal(1, nuevos);
        Assert.Equal(1, actualizados);
        Assert.Equal(2, _h.InventorySvc.Search().Count);
        var up = _h.InventorySvc.Search().Single(x => x.Codigo == "PRD-A");
        Assert.Equal("Bateria Pro", up.Nombre);
        Assert.Equal(9, up.Stock);
        Assert.Equal(40m, up.PrecioVenta);
    }

    [Fact]
    public async Task AplicarImportacion_SaneaNegativosYTipoDesconocido()
    {
        var vm = await BuildVmAsync();

        var import = new[]
        {
            new Producto { Nombre = "Correa Rara", Tipo = "IMEI", Costo = -5m, PrecioVenta = -2m, Stock = -3, FechaIngreso = DateTime.Today }
        };

        var (nuevos, actualizados) = vm.AplicarImportacion(import);
        await TestHelpers.WaitUntil(() => !vm.IsLoading);

        Assert.Equal(1, nuevos);
        Assert.Equal(0, actualizados);
        var saved = _h.InventorySvc.Search().Single(x => x.Nombre == "Correa Rara");
        Assert.Equal(ProductTypes.Accesorio, saved.Tipo);
        Assert.Equal(0, saved.Costo);
        Assert.Equal(0, saved.PrecioVenta);
        Assert.Equal(0, saved.Stock);
    }

    [Fact]
    public async Task Guardar_CamposNumericosVaciosTratanComoCero()
    {
        // Al limpiar un NumericUpDown el binding entrega null; el diálogo debe
        // tratarlo como 0 al guardar (antes reventaba con "(null) a System.Decimal").
        var dialog = _h.Services.GetRequiredService<ProductoEditDialogViewModel>();
        dialog.Initialize(null);
        dialog.Nombre = "Adaptador C";
        dialog.SelectedTipo = ProductTypes.Accesorio;
        dialog.Costo = null;
        dialog.PrecioVenta = null;
        dialog.Stock = null;

        var result = await dialog.SaveAsync();

        Assert.True(result);
        Assert.NotNull(dialog.Saved);
        Assert.Equal(0m, dialog.Saved.Costo);
        Assert.Equal(0m, dialog.Saved.PrecioVenta);
        Assert.Equal(0, dialog.Saved.Stock);
    }
}