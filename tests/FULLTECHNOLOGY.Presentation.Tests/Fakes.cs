// Fakes.cs — Fakes en memoria, TestHarness (contenedor DI) y TestHelpers para las pruebas de presentación.

using System.Collections.ObjectModel;
using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.Tests;

// ============================================================
// Fakes en memoria para el módulo Mantenimiento (F9). Replican
// la semántica observable de los repositorios SQL reales:
//   - GetAll: filtro LIKE sobre varios campos + Status exacto +
//     orden por Id descendente.
// ============================================================

/// <summary>Fake del repo de órdenes: LIKE sobre varios campos, status exacto y orden por Id descendente.</summary>
public sealed class FakeServiceOrderRepository : IServiceOrderRepository
{
    private readonly Dictionary<long, ServiceOrder> _store = new();
    private long _nextId = 1;

    public ServiceOrder Add(ServiceOrder order)
    {
        var clone = Clone(order);
        clone.Id = _nextId++;
        _store[clone.Id] = clone;
        return clone;
    }

    public List<ServiceOrder> GetAll(string? search = null, string? status = null)
    {
        IEnumerable<ServiceOrder> q = _store.Values;
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(o =>
                o.OrderNumber.ToLowerInvariant().Contains(s) ||
                o.CustomerName.ToLowerInvariant().Contains(s) ||
                o.CustomerPhone.ToLowerInvariant().Contains(s) ||
                o.SerialImei.ToLowerInvariant().Contains(s) ||
                o.Brand.ToLowerInvariant().Contains(s) ||
                o.Model.ToLowerInvariant().Contains(s));
        }
        return q.OrderByDescending(o => o.Id).ToList();
    }

    public List<ServiceOrder> GetAllPendingBalance() =>
        _store.Values.Where(o => o.Balance > 0).ToList();

    public ServiceOrder? Get(long id) => _store.TryGetValue(id, out var o) ? Clone(o) : null;

    public long Save(ServiceOrder order)
    {
        if (order.Id == 0)
        {
            order.Id = _nextId++;
            _store[order.Id] = Clone(order);
        }
        else
        {
            _store[order.Id] = Clone(order);
        }
        return order.Id;
    }

    public void Delete(long id) => _store.Remove(id);

    private static ServiceOrder Clone(ServiceOrder src) => new()
    {
        Id = src.Id,
        OrderNumber = src.OrderNumber,
        ClienteId = src.ClienteId,
        CustomerName = src.CustomerName,
        CustomerDoc = src.CustomerDoc,
        CustomerPhone = src.CustomerPhone,
        DeviceType = src.DeviceType,
        Brand = src.Brand,
        Model = src.Model,
        SerialImei = src.SerialImei,
        Color = src.Color,
        PhysicalCondition = src.PhysicalCondition,
        Accessories = src.Accessories,
        ReportedFault = src.ReportedFault,
        Diagnosis = src.Diagnosis,
        RepairDetails = src.RepairDetails,
        Status = src.Status,
        DiagnosisCost = src.DiagnosisCost,
        PartsCost = src.PartsCost,
        LaborCost = src.LaborCost,
        Discount = src.Discount,
        Deposit = src.Deposit,
        PaymentMethod = src.PaymentMethod,
        ReceivedAt = src.ReceivedAt,
        DeliveredAt = src.DeliveredAt,
        Notes = src.Notes
    };
}

/// <summary>Fake mínimo del repo de pagos: solo cuenta las llamadas a RecalcDeposit.</summary>
public sealed class FakePagoRepository : IPagoRepository
{
    public int RecalcCalls { get; private set; }

    public List<Pago> GetPagos(long serviceOrderId) => new();

    public long AddPago(Pago pago) => 1;

    public void DeletePago(long pagoId, long serviceOrderId)
    {
    }

    public void RecalcDeposit(long serviceOrderId) => RecalcCalls++;
}

/// <summary>Fake del repo de clientes: LIKE sobre varios campos, orden alfabético y fallo de BD simulable.</summary>
public sealed class FakeClienteRepository : IClienteRepository
{
    private readonly Dictionary<long, Cliente> _store = new();
    private long _nextId = 1;

    public bool EnUsoResult { get; set; } = true;

    /// <summary>Simula fallo de BD en la carga (estado de error de F12).</summary>
    public bool ThrowOnSearch { get; set; }

    public Cliente Add(Cliente cliente)
    {
        cliente.Id = _nextId++;
        _store[cliente.Id] = cliente;
        return cliente;
    }

    /// <summary>
    /// Replica la semántica SQL real: LIKE sobre Nombre, Documento y Celular.
    /// </summary>
    public List<Cliente> GetClientes(string? tipo = null, string? search = null)
    {
        if (ThrowOnSearch) throw new InvalidOperationException("BD no disponible (prueba)");
        IEnumerable<Cliente> q = _store.Values;
        if (!string.IsNullOrWhiteSpace(tipo))
            q = q.Where(c => c.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c =>
                c.Nombre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.Documento.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.Celular.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        return q.OrderBy(c => c.Nombre).ToList();
    }

    public Cliente? GetClienteById(long id) => _store.TryGetValue(id, out var c) ? c : null;

    public Cliente? FindClienteComprador(string docOrCel) =>
        _store.Values.FirstOrDefault(c =>
            c.Documento.Equals(docOrCel, StringComparison.OrdinalIgnoreCase) ||
            c.Celular.Equals(docOrCel, StringComparison.OrdinalIgnoreCase));

    public long SaveCliente(Cliente cliente)
    {
        if (cliente.Id == 0) cliente.Id = _nextId++;
        _store[cliente.Id] = cliente;
        return cliente.Id;
    }

    public void DeleteCliente(long id) => _store.Remove(id);

    public bool ClienteEnUso(long id) => EnUsoResult;
}

/// <summary>Fake del repo de productos: LIKE por nombre/código/proveedor y código PRD auto-generado.</summary>
public sealed class FakeProductoRepository : IProductoRepository
{
    private readonly Dictionary<long, Producto> _store = new();
    private long _nextId = 1;

    /// <summary>
    /// Replica la semántica SQL real: LIKE sobre Nombre, Codigo y Proveedor.
    /// </summary>
    public List<Producto> GetProductos(string? tipo = null, string? search = null)
    {
        IEnumerable<Producto> q = _store.Values;
        if (!string.IsNullOrWhiteSpace(tipo))
            q = q.Where(p => p.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p =>
                p.Nombre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                p.Codigo.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (p.Proveedor ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        return q.OrderBy(p => p.Id).ToList();
    }

    public Producto? GetProducto(long id) => _store.TryGetValue(id, out var p) ? p : null;

    /// <summary>Replica la auto-generación de código PRD-{año}-{n:00000} del SQL real.</summary>
    public long SaveProducto(Producto p)
    {
        if (string.IsNullOrWhiteSpace(p.Codigo))
            p.Codigo = $"PRD-{p.FechaIngreso:yyyy}-{_store.Count + 1:00000}";
        if (p.Id == 0) p.Id = _nextId++;
        _store[p.Id] = p;
        return p.Id;
    }

    public void DeleteProducto(long id) => _store.Remove(id);

    public void UpdateProductoStock(long id, int newStock)
    {
        if (_store.TryGetValue(id, out var p)) p.Stock = newStock;
    }
}

/// <summary>Fake del repo de ventas: folios FV-{n} y orden por fecha descendente (replica el SQL real).</summary>
public sealed class FakeVentaRepository : IVentaRepository
{
    public long NextId = 1;
    public long NextFolio = 1000;
    public List<Venta> Ventas { get; } = new();
    public List<VentaDetalle> Detalles { get; } = new();

    public long SaveVenta(Venta venta, List<VentaDetalle> detalles)
    {
        venta.Id = NextId++;
        if (string.IsNullOrWhiteSpace(venta.VentaNumber)) venta.VentaNumber = $"FV-{NextFolio++}";
        Ventas.Add(venta);
        foreach (var d in detalles)
        {
            d.Id = Detalles.Count + 1;
            d.VentaId = venta.Id;
            Detalles.Add(d);
        }
        return venta.Id;
    }

    public string NextVentaNumber() => $"FV-{NextFolio}";

    public List<Venta> GetVentas(DateTime? from = null, DateTime? to = null, string? metodo = null, string? search = null)
    {
        var q = Ventas.AsEnumerable();
        if (from.HasValue) q = q.Where(v => v.Fecha >= from);
        if (to.HasValue) q = q.Where(v => v.Fecha <= to.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(metodo)) q = q.Where(v => v.MetodoPago == metodo);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(v => v.VentaNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                             v.ClienteNombre.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        return q.OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id).ToList();
    }

    public List<(Venta, VentaDetalle)> GetVentasConLineas(DateTime from, DateTime to, string? metodo = null)
    {
        return Ventas.Where(v => v.Fecha.Date >= from.Date && v.Fecha.Date <= to.Date)
            .Where(v => string.IsNullOrWhiteSpace(metodo) || v.MetodoPago == metodo)
            .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id)
            .SelectMany(v => Detalles.Where(d => d.VentaId == v.Id).Select(d => (v, d)))
            .ToList();
    }

    public List<VentaDetalle> GetVentaConLineas(long ventaId) =>
        Detalles.Where(d => d.VentaId == ventaId).OrderBy(d => d.Id).ToList();
}

/// <summary>Fake del repo contable: cobros y series de balance en memoria para la gráfica.</summary>
public sealed class FakeAccountingRepository : IAccountingRepository
{
    public List<(Pago Pago, ServiceOrder Orden)> Cobros { get; } = new();
    public List<(DateTime Fecha, decimal Ventas, decimal Cobros)> BalanceData { get; set; } = new();
    public int BalanceSeriesCallCount { get; private set; }

    public List<(Pago, ServiceOrder)> GetCobrosConOrden(DateTime from, DateTime to, string? metodo = null)
    {
        var q = Cobros.Where(x => x.Pago.Fecha.Date >= from.Date && x.Pago.Fecha.Date <= to.Date);
        if (!string.IsNullOrWhiteSpace(metodo))
            q = q.Where(x => x.Pago.Metodo.Equals(metodo, StringComparison.OrdinalIgnoreCase));
        return q.Select(x => (x.Pago, x.Orden)).ToList();
    }

    public (decimal, decimal, decimal) VentasPorMetodo(DateTime from, DateTime to) => (0, 0, 0);
    public (decimal, decimal, decimal) CobrosPorMetodo(DateTime from, DateTime to) => (0, 0, 0);

    public List<(DateTime, decimal, decimal)> BalanceSeries(DateTime from, DateTime to)
    {
        BalanceSeriesCallCount++;
        return BalanceData.Where(x => x.Fecha.Date >= from.Date && x.Fecha.Date <= to.Date)
            .Select(x => (x.Fecha, x.Ventas, x.Cobros)).ToList();
    }

    public decimal TotalVentasPeriodo(DateTime from, DateTime to) => 0;
    public decimal TotalCobrosPeriodo(DateTime from, DateTime to) => 0;
}

/// <summary>Fake del almacén de moneda y de preferencias (pares clave/valor en memoria).</summary>
public sealed class FakeCurrencyStore : ICurrencyStore
{
    public string Currency { get; private set; } = "COP";
    public string GetCurrency() => Currency;
    public void SetCurrency(string currency) => Currency = currency;
}

public sealed class FakeSettingsStore : ISettingsStore
{
    private readonly Dictionary<string, string> _values = new();
    public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;
    public void Set(string key, string value) => _values[key] = value;
}

public sealed class FakeBusinessInfo : IBusinessInfoStore
{
    public string BusinessName { get; set; } = "DECO TECHNOLOGY";
    public bool PinHabilitado { get; set; }
    public bool EmailDiarioHabilitado { get; set; }
    string IBusinessInfoStore.GetBusinessName() => BusinessName;
    void IBusinessInfoStore.SetBusinessName(string name) => BusinessName = string.IsNullOrWhiteSpace(name) ? "DECO TECHNOLOGY" : name.Trim();
}

/// <summary>Fake del servicio de respaldos: registra rutas de copia e importación.</summary>
public sealed class FakeDatabaseBackupService : IDatabaseBackupService
{
    public bool ImportResult { get; set; } = true;
    public List<string> Backups { get; } = new();
    public List<string> Imports { get; } = new();
    public void Backup(string destination) => Backups.Add(destination);
    public bool Import(string sourcePath)
    {
        Imports.Add(sourcePath);
        return ImportResult;
    }
}

/// <summary>Stub de diálogos: devuelve respuestas configurables y acumula los mensajes mostrados.</summary>
public sealed class StubDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public string? PromptResult { get; set; }
    public List<string> Messages { get; } = new();
    public List<string> Lists { get; } = new();

    public Task ShowMessageAsync(string title, string message)
    {
        Messages.Add($"{title}: {message}");
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(ConfirmResult);

    public Task<string?> PromptAsync(string title, string message, string initial = "") =>
        Task.FromResult<string?>(PromptResult ?? initial);

    public Task ShowListAsync(string title, string subtitle, IReadOnlyList<AlertRowItem> rows)
    {
        Lists.Add($"{title} ({rows.Count}): {subtitle}");
        return Task.CompletedTask;
    }
}

/// <summary>Harness reutilizable: fakes, servicios reales y provider de DI.</summary>
public sealed class TestHarness
{
    public FakeServiceOrderRepository Orders { get; } = new();
    public FakePagoRepository Pagos { get; } = new();
    public FakeClienteRepository Clientes { get; } = new();
    public FakeProductoRepository Productos { get; } = new();
    public FakeVentaRepository Ventas { get; } = new();
    public FakeAccountingRepository AccountingRepo { get; } = new();
    public FakeCurrencyStore Currency { get; } = new();
    public FakeSettingsStore Settings { get; } = new();
    public FakeBusinessInfo Business { get; } = new();
    public FakeDatabaseBackupService Backup { get; } = new();
    public StubDialogService Dialogs { get; } = new();

    public OrdersService OrdersSvc { get; }
    public PaymentsService PaymentsSvc { get; }
    public CustomersService CustomersSvc { get; }
    public InventoryService InventorySvc { get; }
    public SalesService SalesSvc { get; }
    public AccountingService AccountingSvc { get; }
    public CurrencyService CurrencySvc { get; }
    public BusinessSettingsService SettingsSvc { get; }
    public AlertsSettingsService AlertsSvc { get; }
    public DashboardService DashboardSvc { get; }
    public ThemeService ThemeSvc { get; }

    public ServiceProvider Services { get; }

    public TestHarness()
    {
        OrdersSvc = new OrdersService(Orders, Pagos, Clientes);
        PaymentsSvc = new PaymentsService(Pagos, Orders);
        CustomersSvc = new CustomersService(Clientes);
        InventorySvc = new InventoryService(Productos);
        SalesSvc = new SalesService(Ventas, Productos);
        CurrencySvc = new CurrencyService(Currency);
        AccountingSvc = new AccountingService(Ventas, AccountingRepo, CurrencySvc);
        SettingsSvc = new BusinessSettingsService(Business, Settings, CurrencySvc);
        AlertsSvc = new AlertsSettingsService(Settings);
        DashboardSvc = new DashboardService(Productos, Orders, AlertsSvc);
        ThemeSvc = new ThemeService(SettingsSvc);
        Services = new ServiceCollection()
            .AddSingleton(OrdersSvc)
            .AddSingleton(PaymentsSvc)
            .AddSingleton(CustomersSvc)
            .AddSingleton(InventorySvc)
            .AddSingleton(SalesSvc)
            .AddSingleton(AccountingSvc)
            .AddSingleton(CurrencySvc)
            .AddSingleton(SettingsSvc)
            .AddSingleton(AlertsSvc)
            .AddSingleton(DashboardSvc)
            .AddSingleton(ThemeSvc)
            .AddSingleton<IDialogService>(Dialogs)
            .AddSingleton<IDatabaseBackupService>(Backup)
            .AddTransient<MantenimientoViewModel>()
            .AddTransient<ClientesViewModel>()
            .AddTransient<InventarioViewModel>()
            .AddTransient<VentaViewModel>()
            .AddTransient<AccountingViewModel>()
            .AddTransient<HistorialVentaViewModel>()
            .AddTransient<ConfigurationViewModel>()
            .AddTransient<ClienteEditDialogViewModel>()
            .AddTransient<ProductoEditDialogViewModel>()
            .AddTransient<CantidadDialogViewModel>()
            .AddTransient<PagoVentaDialogViewModel>()
            .BuildServiceProvider();
    }

    public MantenimientoViewModel CreateVm() => new(OrdersSvc, PaymentsSvc, CustomersSvc, CurrencySvc, Dialogs, Services);

    public ClientesViewModel CreateClientesVm() => new(CustomersSvc, Dialogs, Services);

    public InventarioViewModel CreateInventarioVm() => new(InventorySvc, CurrencySvc, Dialogs, Services);

    public VentaViewModel CreateVentaVm() => new(InventorySvc, SalesSvc, CurrencySvc, Dialogs, Services);

    public AccountingViewModel CreateAccountingVm() => new(AccountingSvc, CurrencySvc);

    public HistorialVentaViewModel CreateHistorialVentaVm() =>
        new(SalesSvc, CurrencySvc, Dialogs);

    public ConfigurationViewModel CreateConfigurationVm() =>
        new(SettingsSvc, ThemeSvc, Dialogs, Backup, AlertsSvc);

    public Cliente SeedCliente(string nombre, string tipo = CustomerTypes.Comprador, string doc = "", string cel = "", string dir = "", string web = "", string red = "") =>
        Clientes.Add(new Cliente
        {
            Tipo = tipo, Nombre = nombre, Documento = doc, Celular = cel, Direccion = dir, Web = web, RedSocial = red
        });

    public Producto SeedProducto(string nombre, string tipo = ProductTypes.Accesorio, string codigo = "", int stock = 0, decimal costo = 0m, string proveedor = "")
    {
        var p = Productos.SaveProducto(new Producto
        {
            Nombre = nombre, Tipo = tipo, Codigo = codigo, Stock = stock,
            Costo = costo, Proveedor = string.IsNullOrWhiteSpace(proveedor) ? null : proveedor,
            FechaIngreso = DateTime.Today
        });
        return Productos.GetProducto(p)!;
    }

    public ServiceOrder SeedOrder(
        string number,
        string status,
        string customer = "Cliente",
        string phone = "",
        string imei = "",
        decimal total = 100m,
        decimal deposit = 0m,
        DateTime? receivedAt = null)
    {
        var o = Orders.Add(new ServiceOrder
        {
            OrderNumber = number,
            CustomerName = customer,
            CustomerPhone = phone,
            SerialImei = imei,
            Brand = "Samsung",
            Model = "A50",
            Status = status,
            PartsCost = total,
            Deposit = deposit,
            ReceivedAt = receivedAt ?? DateTime.Today
        });
        return o;
    }

    public Venta SeedVenta(DateTime fecha, string metodo, decimal total, string cliente = "Cliente",
        params (string Producto, int Cantidad, decimal Precio)[] lineas)
    {
        var v = new Venta
        {
            Fecha = fecha,
            ClienteNombre = cliente,
            MetodoPago = metodo,
            Total = total
        };
        var detalles = lineas.Select(l => new VentaDetalle
        {
            ProductoNombre = l.Producto, Cantidad = l.Cantidad, PrecioUnitario = l.Precio
        }).ToList();
        Ventas.SaveVenta(v, detalles);
        return v;
    }

    public void SeedCobro(DateTime fecha, string metodo, decimal monto, string orden, string concepto = "Abono",
        string cliente = "Cliente")
    {
        var o = SeedOrder(orden, RepairStatuses.Entregado, cliente, total: monto);
        AccountingRepo.Cobros.Add((new Pago
        {
            Fecha = fecha, Metodo = metodo, Monto = monto, Concepto = concepto
        }, o));
    }
}

public static class TestHelpers
{
    /// <summary>Espera hasta que una condición se cumpla (recargar es async fire-and-forget).</summary>
    public static async Task WaitUntil(Func<bool> condition, int timeoutMs = 3000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            await Task.Delay(10);
    }
}