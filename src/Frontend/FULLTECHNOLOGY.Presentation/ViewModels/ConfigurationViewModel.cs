// ConfigurationViewModel.cs — Ajustes del negocio (nombre, moneda), tema claro/oscuro y backup/importación de la base de datos.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// CONFIGURACIÓN (F10). Replica el menú de ajustes de MainForm
// v2 (ContextMenuStrip): nombre del negocio, moneda, tema y
// backup/importación de la base de datos. Los selectores de
// archivo (SaveFilePicker/OpenFilePicker) los resuelve la vista
// y delegan a CopiaSeguridad/ImportarBD (testeables).
// ============================================================
public partial class ConfigurationViewModel : ViewModelBase
{
    private readonly BusinessSettingsService _settings;
    private readonly ThemeService _theme;
    private readonly IDialogService _dialogs;
    private readonly IDatabaseBackupService _backup;
    private readonly AlertsSettingsService _alerts;

    public string Subtitle => "Nombre del negocio, moneda, tema y base de datos.";

    public IReadOnlyList<string> Currencies { get; } = CurrencyService.KnownCurrencies;

    // Valores editables: nombre del negocio, moneda activa y etiqueta del tema.
    [ObservableProperty]
    public partial string BusinessName { get; set; }

    [ObservableProperty]
    public partial string Currency { get; set; }

    [ObservableProperty]
    public partial string ThemeLabel { get; set; }

    // Umbrales de las alertas del Inicio (se editan y guardan en bloque).
    [ObservableProperty]
    public partial int StockAccesorioUmbral { get; set; }

    [ObservableProperty]
    public partial int StockRepuestoUmbral { get; set; }

    [ObservableProperty]
    public partial int DiasRecibidoAListo { get; set; }

    [ObservableProperty]
    public partial int DiasListoAEntregado { get; set; }

    public ConfigurationViewModel(
        BusinessSettingsService settings,
        ThemeService theme,
        IDialogService dialogs,
        IDatabaseBackupService backup,
        AlertsSettingsService alerts)
    {
        _settings = settings;
        _theme = theme;
        _dialogs = dialogs;
        _backup = backup;
        _alerts = alerts;
        BusinessName = settings.BusinessName;
        Currency = settings.Currency;
        ThemeLabel = ThemeName();
        StockAccesorioUmbral = alerts.StockAccesorioUmbral;
        StockRepuestoUmbral = alerts.StockRepuestoUmbral;
        DiasRecibidoAListo = alerts.DiasRecibidoAListo;
        DiasListoAEntregado = alerts.DiasListoAEntregado;
        // Si el nombre cambia (p. ej. desde otro diálogo), se refleja aquí.
        _settings.BusinessNameChanged += () => BusinessName = _settings.BusinessName;
    }

    // Pide el nuevo nombre por diálogo (prompt); lo persiste si es válido y distinto.
    [RelayCommand]
    private async Task CambiarNombreAsync()
    {
        var actual = _settings.BusinessName;
        var input = await _dialogs.PromptAsync(
            "Nombre del negocio",
            "Nombre del negocio (se mostrará en Sidebar, Header y documentos):",
            actual);
        if (string.IsNullOrWhiteSpace(input) || input.Trim() == actual) return;
        _settings.SetBusinessName(input.Trim());
        BusinessName = _settings.BusinessName;
        await _dialogs.ShowMessageAsync("Nombre del negocio", $"Nombre del negocio actualizado a \"{_settings.BusinessName}\".");
    }

    // El parámetro es el código de moneda elegido en la vista (p. ej. "USD").
    [RelayCommand]
    private Task CambiarMonedaAsync(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency == _settings.Currency) return Task.CompletedTask;
        _settings.SetCurrency(currency);
        Currency = _settings.Currency;
        return _dialogs.ShowMessageAsync("Moneda", $"Moneda cambiada a {currency}.");
    }

    [RelayCommand]
    private void ToggleTema()
    {
        _theme.Toggle();
        ThemeLabel = ThemeName();
    }

    // Copia los umbrales editados al servicio y los persiste en settings.ini;
    // se releen luego para normalizar valores inválidos (menores a 1).
    [RelayCommand]
    private async Task GuardarAlertasAsync()
    {
        _alerts.StockAccesorioUmbral = StockAccesorioUmbral;
        _alerts.StockRepuestoUmbral = StockRepuestoUmbral;
        _alerts.DiasRecibidoAListo = DiasRecibidoAListo;
        _alerts.DiasListoAEntregado = DiasListoAEntregado;
        _alerts.Save();

        StockAccesorioUmbral = _alerts.StockAccesorioUmbral;
        StockRepuestoUmbral = _alerts.StockRepuestoUmbral;
        DiasRecibidoAListo = _alerts.DiasRecibidoAListo;
        DiasListoAEntregado = _alerts.DiasListoAEntregado;
        await _dialogs.ShowMessageAsync("Alertas del Inicio", "Umbrales de alerta guardados correctamente.");
    }

    // La ruta del archivo la entrega la vista (SaveFilePickerAsync en code-behind).
    [RelayCommand]
    private async Task CopiaSeguridadAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            _backup.Backup(path);
            await _dialogs.ShowMessageAsync("Copia de seguridad", $"Copia de seguridad creada correctamente.\n\n{path}");
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"No se pudo crear la copia: {ex.Message}");
        }
    }

    // La ruta del archivo a importar la entrega la vista (OpenFilePickerAsync).
    // Antes de reemplazar la BD se pide confirmación explícita al usuario.
    [RelayCommand]
    private async Task ImportarBDAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var ok = await _dialogs.ConfirmAsync("Importar base de datos",
            "Se reemplazará la base de datos actual por la seleccionada. Se creará un respaldo previo automático. ¿Continuar?");
        if (!ok) return;
        try
        {
            var importado = _backup.Import(path);
            await _dialogs.ShowMessageAsync("Importar base de datos",
                importado ? "Base de datos importada correctamente." : "No se pudo importar el archivo.");
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Error", $"No se pudo importar el archivo: {ex.Message}");
        }
    }

    private string ThemeName() => _theme.IsDark ? "Oscuro" : "Claro";
}