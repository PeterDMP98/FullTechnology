// Fakes.cs — Fakes en memoria de los repositorios SQL reales usados por las pruebas de la capa de aplicación.

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Application.Tests.Fakes;

/// <summary>Fake del repo de órdenes: filtra (LIKE por cliente/nº) y asigna IDs autoincrementales.</summary>
public class FakeOrdersRepository : IServiceOrderRepository
{
    public long NextId = 1;
    public List<ServiceOrder> Items { get; } = new();

    public List<ServiceOrder> GetAll(string? search = null, string? status = null)
    {
        var q = Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(o => o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) || o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);
        return q.ToList();
    }

    public List<ServiceOrder> GetAllPendingBalance() => Items.Where(o => o.Balance > 0).ToList();

    public ServiceOrder? Get(long id) => Items.FirstOrDefault(o => o.Id == id);

    public long Save(ServiceOrder order)
    {
        if (order.Id == 0)
            order.Id = NextId++;
        Items.RemoveAll(o => o.Id == order.Id);
        Items.Add(order);
        return order.Id;
    }

    public void Delete(long id) => Items.RemoveAll(o => o.Id == id);
}

/// <summary>Fake del repo de pagos: registra pagos y avisa para recalcular el depósito de la orden.</summary>
public class FakePagoRepository : IPagoRepository
{
    public long NextId = 1;
    public List<Pago> Items { get; } = new();

    public List<Pago> GetPagos(long serviceOrderId) => Items.Where(p => p.ServiceOrderId == serviceOrderId).ToList();

    public long AddPago(Pago pago)
    {
        pago.Id = NextId++;
        Items.Add(pago);
        RecalcDeposit(serviceOrderId: pago.ServiceOrderId);
        return pago.Id;
    }

    public void DeletePago(long pagoId, long serviceOrderId) => Items.RemoveAll(p => p.Id == pagoId);

    public void RecalcDeposit(long serviceOrderId)
    {
        // El fake no persiste ServiceOrder; el servicio recalcula el depósito desde aquí.
    }
}

/// <summary>Fake del repo de clientes: LIKE por nombre/documento y búsqueda de comprador por doc o celular.</summary>
public class FakeClienteRepository : IClienteRepository
{
    public long NextId = 1;
    public List<Cliente> Items { get; } = new();

    public List<Cliente> GetClientes(string? tipo = null, string? search = null)
    {
        var q = Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(c => c.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(c => c.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase) || c.Documento.Contains(search, StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public Cliente? GetClienteById(long id) => Items.FirstOrDefault(c => c.Id == id);

    public Cliente? FindClienteComprador(string docOrCel) =>
        Items.FirstOrDefault(c => c.Tipo == "Cliente comprador" && (c.Documento == docOrCel || c.Celular == docOrCel));

    public long SaveCliente(Cliente cliente)
    {
        if (cliente.Id == 0) cliente.Id = NextId++;
        Items.RemoveAll(c => c.Id == cliente.Id);
        Items.Add(cliente);
        return cliente.Id;
    }

    public void DeleteCliente(long id) => Items.RemoveAll(c => c.Id == id);

    public bool ClienteEnUso(long id) => false;
}

/// <summary>Fake del repo de productos: LIKE por nombre/código y actualización directa de stock.</summary>
public class FakeProductoRepository : IProductoRepository
{
    public long NextId = 1;
    public List<Producto> Items { get; } = new();

    public List<Producto> GetProductos(string? tipo = null, string? search = null)
    {
        var q = Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(p => p.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase) || p.Codigo.Contains(search, StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public Producto? GetProducto(long id) => Items.FirstOrDefault(p => p.Id == id);

    public long SaveProducto(Producto producto)
    {
        if (producto.Id == 0) producto.Id = NextId++;
        Items.RemoveAll(p => p.Id == producto.Id);
        Items.Add(producto);
        return producto.Id;
    }

    public void DeleteProducto(long id) => Items.RemoveAll(p => p.Id == id);

    public void UpdateProductoStock(long id, int newStock)
    {
        var p = GetProducto(id);
        if (p != null) p.Stock = newStock;
    }
}

/// <summary>Fake del repo de ventas: genera folios FV-{n} y consulta por rango, método y búsqueda.</summary>
public class FakeVentaRepository : IVentaRepository
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
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(v => v.VentaNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public List<(Venta, VentaDetalle)> GetVentasConLineas(DateTime from, DateTime to, string? metodo = null)
    {
        return Ventas.Where(v => v.Fecha.Date >= from.Date && v.Fecha.Date <= to.Date)
            .Where(v => string.IsNullOrWhiteSpace(metodo) || v.MetodoPago == metodo)
            .SelectMany(v => Detalles.Where(d => d.VentaId == v.Id).Select(d => (v, d)))
            .ToList();
    }
}

/// <summary>Fake del repo contable: cobros en memoria y conteo de llamadas a BalanceSeries.</summary>
public class FakeAccountingRepository : IAccountingRepository
{
    public List<(Pago, ServiceOrder)> Cobros { get; } = new();
    public int BalanceSeriesCallCount;

    public List<(Pago, ServiceOrder)> GetCobrosConOrden(DateTime from, DateTime to, string? metodo = null)
        => Cobros.Where(x => x.Item1.Fecha.Date >= from.Date && x.Item1.Fecha.Date <= to.Date)
            .Where(x => string.IsNullOrWhiteSpace(metodo) || x.Item1.Metodo == metodo).ToList();

    public (decimal, decimal, decimal) VentasPorMetodo(DateTime from, DateTime to) => (0, 0, 0);
    public (decimal, decimal, decimal) CobrosPorMetodo(DateTime from, DateTime to) => (0, 0, 0);

    public List<(DateTime, decimal, decimal)> BalanceSeries(DateTime from, DateTime to)
    {
        BalanceSeriesCallCount++;
        return new List<(DateTime, decimal, decimal)>();
    }

    public decimal TotalVentasPeriodo(DateTime from, DateTime to) => 0;
    public decimal TotalCobrosPeriodo(DateTime from, DateTime to) => 0;
}

/// <summary>Fake del almacén de moneda: guarda el valor en memoria.</summary>
public class FakeCurrencyStore : ICurrencyStore
{
    public string Currency { get; set; } = "COP";

    public string GetCurrency() => Currency;

    public void SetCurrency(string currency) => Currency = currency;
}

/// <summary>Fake del almacén de preferencias: guarda pares clave/valor en memoria.</summary>
public class FakeSettingsStore : ISettingsStore
{
    public Dictionary<string, string> Values { get; } = new();

    public string? Get(string key) => Values.TryGetValue(key, out var v) ? v : null;

    public void Set(string key, string value) => Values[key] = value;
}