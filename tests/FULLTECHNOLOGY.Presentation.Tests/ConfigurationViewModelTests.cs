// ConfigurationViewModelTests.cs — Pruebas de configuración (F10): nombre, moneda, tema, copias de seguridad e importación de BD.

using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del viewmodel de configuración: ajustes del negocio, tema, moneda y respaldos.</summary>
public class ConfigurationViewModelTests
{
    private readonly TestHarness _harness = new();
    private readonly ConfigurationViewModel _vm;

    public ConfigurationViewModelTests()
    {
        _vm = _harness.CreateConfigurationVm();
    }

    [Fact]
    public void Ctor_ReflejaValoresPersistidos()
    {
        Assert.Equal("DECO TECHNOLOGY", _vm.BusinessName);
        Assert.Equal("COP", _vm.Currency);
        Assert.Equal("Claro", _vm.ThemeLabel);
        Assert.Equal("DECO TECHNOLOGY", _harness.Business.BusinessName);
    }

    [Fact]
    public async Task CambiarNombre_Persiste_NotificaYActualiza()
    {
        _harness.Dialogs.PromptResult = "Mi Negocio";
        var cambios = 0;
        _harness.SettingsSvc.BusinessNameChanged += () => cambios++;

        await _vm.CambiarNombreCommand.ExecuteAsync(null);

        Assert.Equal("Mi Negocio", _harness.SettingsSvc.BusinessName);
        Assert.Equal("Mi Negocio", _vm.BusinessName);
        Assert.Equal(1, cambios);
        Assert.Contains(_harness.Dialogs.Messages, m => m.Contains("Mi Negocio"));
    }

    [Fact]
    public async Task CambiarNombre_SinCambio_NoPersisteNiMensaje()
    {
        _harness.Dialogs.PromptResult = "DECO TECHNOLOGY";

        await _vm.CambiarNombreCommand.ExecuteAsync(null);

        Assert.Equal("DECO TECHNOLOGY", _harness.Business.BusinessName);
        Assert.Empty(_harness.Dialogs.Messages);
    }

    [Fact]
    public async Task CambiarNombre_Cancelado_NoPersiste()
    {
        _harness.Dialogs.PromptResult = null;

        await _vm.CambiarNombreCommand.ExecuteAsync(null);

        Assert.Equal("DECO TECHNOLOGY", _harness.Business.BusinessName);
        Assert.Empty(_harness.Dialogs.Messages);
    }

    [Fact]
    public async Task CambiarMoneda_PersisteYActualiza()
    {
        await _vm.CambiarMonedaCommand.ExecuteAsync("USD");

        Assert.Equal("USD", _harness.SettingsSvc.Currency);
        Assert.Equal("USD", _vm.Currency);
        Assert.Contains(_harness.Dialogs.Messages, m => m.Contains("USD"));
    }

    [Fact]
    public async Task CambiarMoneda_Misma_NoMensaje()
    {
        await _vm.CambiarMonedaCommand.ExecuteAsync("COP");

        Assert.Equal("COP", _harness.SettingsSvc.Currency);
        Assert.Empty(_harness.Dialogs.Messages);
    }

    [Fact]
    public async Task ToggleTema_CambiaEtiquetaYPersiste()
    {
        Assert.Equal("Claro", _vm.ThemeLabel);

        _vm.ToggleTemaCommand.Execute(null);

        Assert.Equal("Oscuro", _vm.ThemeLabel);
        Assert.Equal("oscuro", _harness.SettingsSvc.Theme);
    }

    [Fact]
    public async Task CopiaSeguridad_RutaValida_CopiaYNotifica()
    {
        const string path = @"C:\carpeta\DecoTechnology_Backup_20260914_0815.db";

        await _vm.CopiaSeguridadCommand.ExecuteAsync(path);

        Assert.Equal(new[] { path }, _harness.Backup.Backups);
        Assert.Contains(_harness.Dialogs.Messages, m => m.Contains("correctamente"));
    }

    [Fact]
    public async Task CopiaSeguridad_SinRuta_NoHaceNada()
    {
        await _vm.CopiaSeguridadCommand.ExecuteAsync(null);

        Assert.Empty(_harness.Backup.Backups);
        Assert.Empty(_harness.Dialogs.Messages);
    }

    [Fact]
    public async Task ImportarBD_Confirmado_ImportaYNotifica()
    {
        await _vm.ImportarBDCommand.ExecuteAsync(@"C:\respaldos\x.db");

        Assert.Equal(new[] { @"C:\respaldos\x.db" }, _harness.Backup.Imports);
        Assert.Contains(_harness.Dialogs.Messages, m => m.Contains("importada correctamente"));
    }

    [Fact]
    public async Task ImportarBD_Rechazado_NoImporta()
    {
        _harness.Dialogs.ConfirmResult = false;

        await _vm.ImportarBDCommand.ExecuteAsync(@"C:\respaldos\x.db");

        Assert.Empty(_harness.Backup.Imports);
        Assert.Empty(_harness.Dialogs.Messages);
    }

    [Fact]
    public async Task ImportarBD_Falla_NotificaError()
    {
        _harness.Backup.ImportResult = false;

        await _vm.ImportarBDCommand.ExecuteAsync(@"C:\respaldos\x.db");

        Assert.Equal(new[] { @"C:\respaldos\x.db" }, _harness.Backup.Imports);
        Assert.Contains(_harness.Dialogs.Messages, m => m.Contains("No se pudo importar"));
    }
}