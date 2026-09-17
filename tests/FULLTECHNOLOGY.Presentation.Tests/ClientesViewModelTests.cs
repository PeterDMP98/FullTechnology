// ClientesViewModelTests.cs — Pruebas del módulo de clientes: filtros, búsqueda, alta/edición, borrado y estados de error.

using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel de clientes: filtro por tipo, búsqueda, edición y eliminación con restricciones.</summary>
public class ClientesViewModelTests
{
    private readonly TestHarness _h = new();

    /// <summary>Crea un VM y le da tiempo a cargar (Reload es async fire-and-forget).</summary>
    private async Task<ClientesViewModel> BuildVmAsync(string tipo = CustomerTypes.Comprador)
    {
        var vm = _h.CreateClientesVm();
        vm.SelectedTipo = tipo;
        vm.Refresh();
        await TestHelpers.WaitUntil(() => !vm.IsLoading);
        return vm;
    }

    [Fact]
    public async Task FiltraPorTipo_DevuelveSoloEseTipo()
    {
        _h.SeedCliente("Alice", CustomerTypes.Comprador, doc: "CC1");
        _h.SeedCliente("Bodega XYZ", CustomerTypes.Proveedor, doc: "NIT900");

        var vm = await BuildVmAsync(CustomerTypes.Comprador);

        Assert.Single(vm.Rows);
        Assert.Equal("Alice", vm.Rows[0].Nombre);
    }

    [Fact]
    public async Task BuscaPorCelular_SegunCampoSeleccionado()
    {
        _h.SeedCliente("Andres", CustomerTypes.Comprador, cel: "321456");
        _h.SeedCliente("Bogota Tech", CustomerTypes.Comprador, cel: "654321");

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 2);

        vm.SelectedFilterField = "Celular";
        vm.SearchText = "321";
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1);

        Assert.Equal("Andres", vm.Rows[0].Nombre);
    }

    [Fact]
    public async Task BuscaPorDocumento_DentroDelTipo()
    {
        _h.SeedCliente("Repuesto ABC", CustomerTypes.Proveedor, doc: "9001234");
        _h.SeedCliente("Cliente Uno", CustomerTypes.Comprador, doc: "9001234");

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 2);

        vm.SelectedTipo = CustomerTypes.Proveedor;
        await TestHelpers.WaitUntil(() => !vm.IsLoading);

        vm.SelectedFilterField = "Documento o NIT";
        vm.SearchText = "9001234";
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1);

        Assert.Equal("Repuesto ABC", vm.Rows[0].Nombre);
    }

    [Fact]
    public async Task EditarCliente_CambiaCamposYPersiste()
    {
        var c = _h.SeedCliente("Cliente Original", CustomerTypes.Comprador, doc: "D001", cel: "111");

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1);

        var dialog = _h.Services.GetRequiredService<ClienteEditDialogViewModel>();
        dialog.Initialize(c, CustomerTypes.Comprador);
        dialog.Nombre = "Cliente Renombrado";
        dialog.Documento = "D001";

        var result = await dialog.SaveAsync();

        Assert.True(result);
        Assert.Equal("Cliente Renombrado", _h.CustomersSvc.Get(c.Id)!.Nombre);
    }

    [Fact]
    public async Task NuevoComprador_ExigeDocOCelular()
    {
        var dialog = _h.Services.GetRequiredService<ClienteEditDialogViewModel>();
        dialog.Initialize(null, CustomerTypes.Comprador);
        dialog.Nombre = "Sin Docs";

        var result = await dialog.SaveAsync();

        Assert.False(result);
        Assert.Contains(_h.Dialogs.Messages, m => m.Contains("debe llenar el documento"));
    }

    [Fact]
    public async Task DuplicadoDocumento_Rechaza()
    {
        _h.SeedCliente("Existente", CustomerTypes.Comprador, doc: "DUP1", cel: "999");

        var dialog = _h.Services.GetRequiredService<ClienteEditDialogViewModel>();
        dialog.Initialize(null, CustomerTypes.Comprador);
        dialog.Nombre = "Nuevo Duplicado";
        dialog.Documento = "DUP1";
        dialog.Celular = "888";

        var result = await dialog.SaveAsync();

        Assert.False(result);
        Assert.Contains(_h.Dialogs.Messages, m => m.Contains("documento"));
    }

    [Fact]
    public async Task EliminarCliente_NoPermitidoSiEnUso()
    {
        var c = _h.SeedCliente("En uso", CustomerTypes.Comprador);
        _h.Clientes.EnUsoResult = true;

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1);

        var row = vm.Rows[0];
        row.EliminarCommand.Execute(null);

        await TestHelpers.WaitUntil(() => _h.Dialogs.Messages.Any(m => m.Contains("mantenimientos") || m.Contains("ventas")));
        Assert.NotNull(_h.CustomersSvc.Get(c.Id));
    }

    [Fact]
    public async Task EliminarCliente_PermitidoSiNoEstaEnUso()
    {
        var c = _h.SeedCliente("Borrable", CustomerTypes.Comprador);
        _h.Clientes.EnUsoResult = false;

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.Rows.Count == 1);

        var row = vm.Rows[0];
        row.EliminarCommand.Execute(null);

        await TestHelpers.WaitUntil(() => vm.Rows.Count == 0);
        Assert.Null(_h.CustomersSvc.Get(c.Id));
    }

    [Fact]
    public async Task CambiarTipo_ActualizaCampos()
    {
        var vm = new ClientesViewModel(_h.CustomersSvc, _h.Dialogs, _h.Services);

        Assert.Contains("Documento", vm.FilterFieldOptions);
        vm.SelectedTipo = CustomerTypes.Proveedor;
        Assert.Contains("Documento o NIT", vm.FilterFieldOptions);
        Assert.Equal("Nombre", vm.SelectedFilterField);
    }

    [Fact]
    public async Task ErrorDeCarga_EntraEnEstadoDeErrorSinExcepcionCruda()
    {
        // El fake simula una BD caída; el VM muestra IsError sin lanzar excepciones crudas.
        _h.Clientes.ThrowOnSearch = true;

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.IsError);

        Assert.False(vm.IsLoading);
        Assert.False(vm.IsEmpty);
        Assert.Empty(vm.Rows);
        Assert.Contains("clientes", vm.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reintentar_RecuperaCuandoLaCargaVuelveAFuncionar()
    {
        _h.SeedCliente("Alice", CustomerTypes.Comprador, doc: "CC1");
        _h.Clientes.ThrowOnSearch = true;

        var vm = _h.CreateClientesVm();
        vm.Refresh();
        await TestHelpers.WaitUntil(() => vm.IsError);

        // La BD "vuelve"; Reintentar debe recuperar la carga normal.
        _h.Clientes.ThrowOnSearch = false;
        vm.RetryCommand.Execute(null);

        await TestHelpers.WaitUntil(() => !vm.IsError && !vm.IsLoading && vm.Rows.Count == 1);
        Assert.Equal("Alice", vm.Rows[0].Nombre);
    }
}