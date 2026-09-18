// CompositionRoot.cs — Raíz de composición (DI): registra todos los servicios de Infrastructure,
// Application (casos de uso), Presentation (VMs, navegación, tema, diálogos) y resuelve el shell.
using Microsoft.Extensions.DependencyInjection;
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Infrastructure.Persistence;
using FULLTECHNOLOGY.Infrastructure.Persistence.Repositories;
using FULLTECHNOLOGY.Infrastructure.Settings;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using FULLTECHNOLOGY.Presentation.Views;

namespace FULLTECHNOLOGY.Presentation;

// ============================================================
// Composición de dependencias de toda la app: Domain puro,
// Application (port + casos de uso), Infrastructure (stores y
// repositorios sobre la BD real) y Presentation (UI).
// ============================================================
public static class CompositionRoot
{
    public static IServiceProvider Build()
    {
        // El esquema se crea/migra ANTES de registrar nada: la BD existente en
        // %LOCALAPPDATA%\DecoTechnology\DecoTechnology.db puede ser de una versión
        // anterior y carecer de columnas nuevas (ClienteId/CustomerDoc y, en v3.5,
        // StatusChangedAt). SchemaInitializer es idempotente (CREATE IF NOT EXISTS
        // + ALTER condicional + backfill), así que arrancar contra datos reales
        // siempre queda con el esquema vigente sin tocar la información existente.
        var dbFactory = new SqliteConnectionFactory();
        SchemaInitializer.Initialize(dbFactory);

        var services = new ServiceCollection();

        // --- Infrastructure ---
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<ISettingsStore>(sp => sp.GetRequiredService<SettingsStore>());
        services.AddSingleton<ICurrencyStore>(sp => new CurrencyStore(sp.GetRequiredService<ISettingsStore>()));
        services.AddSingleton<IBusinessInfoStore>(sp => new BusinessInfoStore(sp.GetRequiredService<ISettingsStore>()));
        services.AddSingleton<SqliteConnectionFactory>(dbFactory);
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

        services.AddSingleton<IServiceOrderRepository>(sp => new ServiceOrderRepository(sp.GetRequiredService<SqliteConnectionFactory>()));
        services.AddSingleton<IPagoRepository>(sp => new PagoRepository(sp.GetRequiredService<SqliteConnectionFactory>()));
        services.AddSingleton<IClienteRepository>(sp => new ClienteRepository(sp.GetRequiredService<SqliteConnectionFactory>()));
        services.AddSingleton<IProductoRepository>(sp => new ProductoRepository(sp.GetRequiredService<SqliteConnectionFactory>()));
        services.AddSingleton<IVentaRepository>(sp => new VentaRepository(sp.GetRequiredService<SqliteConnectionFactory>()));
        services.AddSingleton<IAccountingRepository>(sp => new AccountingRepository(sp.GetRequiredService<SqliteConnectionFactory>()));

        // --- Application (casos de uso) ---
        services.AddSingleton<CurrencyService>();
        services.AddSingleton<BusinessSettingsService>();
        services.AddSingleton<OrdersService>();
        services.AddSingleton<PaymentsService>();
        services.AddSingleton<CustomersService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<SalesService>();
        services.AddSingleton<AccountingService>();
        services.AddSingleton<AlertsSettingsService>();
        services.AddSingleton<DashboardService>();

        // --- Presentation ---
        services.AddSingleton<IShellPageFactory, ShellPageFactory>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();

        // --- Módulos (F9/F10): Mantenimiento, Clientes, Inventario y diálogos de negocio ---
        services.AddTransient<InicioViewModel>();
        services.AddTransient<MantenimientoViewModel>();
        services.AddTransient<ClientesViewModel>();
        services.AddTransient<InventarioViewModel>();
        services.AddTransient<VentaViewModel>();
        services.AddTransient<AccountingViewModel>();
        services.AddTransient<HistorialVentaViewModel>();
        services.AddTransient<ConfigurationViewModel>();
        services.AddTransient<OrderFormViewModel>();
        services.AddTransient<CobroViewModel>();
        services.AddTransient<BuscarClienteViewModel>();
        services.AddTransient<OrderHistoryViewModel>();
        services.AddTransient<EditDiagnosticoViewModel>();
        services.AddTransient<ClienteEditViewModel>();
        services.AddTransient<ClienteEditDialogViewModel>();
        services.AddTransient<ProductoEditDialogViewModel>();
        services.AddTransient<CantidadDialogViewModel>();
        services.AddTransient<PagoVentaDialogViewModel>();

        return services.BuildServiceProvider();
    }
}