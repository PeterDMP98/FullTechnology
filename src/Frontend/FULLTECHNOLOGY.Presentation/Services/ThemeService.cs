// ThemeService.cs — Aplica el tema claro/oscuro del negocio (preferencia persistida en settings.ini vía BusinessSettingsService) cambiando el ThemeVariant de la app.
using Avalonia.Styling;
using FULLTECHNOLOGY.Application.Services;
using AvaloniaApp = Avalonia.Application;

namespace FULLTECHNOLOGY.Presentation.Services;

// ============================================================
// Tema claro/oscuro. La preferencia se persiste en settings.ini
// vía BusinessSettingsService. Se aplica cambiando el
// ThemeVariant: los tokens de diseño (DarkTheme/LightTheme) son
// ThemeDictionaries de App, así que Fluent y los tokens cambian
// en tiempo real sin recargar nada.
// ============================================================
public class ThemeService
{
    private readonly BusinessSettingsService _settings;

    public ThemeService(BusinessSettingsService settings)
    {
        _settings = settings;
    }

    public bool IsDark => _settings.Theme == "oscuro";

    public void Apply()
    {
        var app = AvaloniaApp.Current;
        if (app is null) return;
        app.RequestedThemeVariant = IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    public void Toggle()
    {
        _settings.SetTheme(IsDark ? "claro" : "oscuro");
        Apply();
    }
}