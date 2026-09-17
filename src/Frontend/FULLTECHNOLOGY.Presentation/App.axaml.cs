// App.axaml.cs — Punto de arranque de la app Avalonia: expone el contenedor de DI global (CompositionRoot), configura LiveCharts2 y el tema (ThemeService) y abre la ventana principal (o la demo del Design System con `--design`), liberando servicios al cerrar.
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.Views;

namespace FULLTECHNOLOGY.Presentation;

public partial class App : Avalonia.Application
{
    // Contenedor de DI global (CompositionRoot). Los ViewModels se resuelven desde aquí
    // (no hay new de servicios en las vistas); Services se expone también para cosas
    // como LiveCharts y los diálogos que necesitan el IServiceProvider.
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Configuración de LiveCharts2 (gráficas de Gestión Contable, ERR-001).
        // Hace falta antes de crear cualquier serie; lo exige la librería.
        LiveCharts.Configure(c => c.UseDefaults());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = CompositionRoot.Build();
        Services.GetRequiredService<ThemeService>().Apply();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // `--design` abre la ventana de demostración del Design System (validación F7).
            var isDesignDemo = Environment.GetCommandLineArgs().Contains("--design");
            desktop.MainWindow = isDesignDemo
                ? new DesignSystemWindow()
                : Services.GetRequiredService<MainWindow>();

            desktop.ShutdownRequested += (s, e) =>
            {
                // Los servicios (singleton stores/repos) se liberan al cerrar.
                (Services as IDisposable)?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}