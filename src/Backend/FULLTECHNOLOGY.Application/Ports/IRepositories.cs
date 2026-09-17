// IRepositories.cs — Contratos de persistencia que la capa Infrastructure implementa sobre SQLite.

using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Application.Ports;

/// <summary>Órdenes de mantenimiento; los filtros de búsqueda y estado replican el SQL del v2.</summary>
public interface IServiceOrderRepository
{
    List<ServiceOrder> GetAll(string? search = null, string? status = null);
    // Órdenes con saldo pendiente: alimentan el panel de cobros y los correos de recordatorio.
    List<ServiceOrder> GetAllPendingBalance();
    ServiceOrder? Get(long id);
    // Devuelve el Id asignado (autoincremental) para encadenar pagos o refrescos.
    long Save(ServiceOrder order);
    void Delete(long id);
}

/// <summary>Historial de pagos de las órdenes; el abono de la orden se deriva siempre de aquí.</summary>
public interface IPagoRepository
{
    List<Pago> GetPagos(long serviceOrderId);
    long AddPago(Pago pago);
    void DeletePago(long pagoId, long serviceOrderId);
    // Reconstruye el abono de la orden como la suma de sus pagos (evita incoherencias al borrar).
    void RecalcDeposit(long serviceOrderId);
}

/// <summary>Clientes/proveedores; el guard de borrado preserva el historial en mantenimientos/ventas.</summary>
public interface IClienteRepository
{
    List<Cliente> GetClientes(string? tipo = null, string? search = null);
    Cliente? GetClienteById(long id);
    // Empareja por documento o celular para autovincular un comprador en una orden nueva.
    Cliente? FindClienteComprador(string docOrCel);
    long SaveCliente(Cliente cliente);
    void DeleteCliente(long id);
    // True si el cliente tiene mantenimientos o ventas: el borrado está prohibido (integridad v2).
    bool ClienteEnUso(long id);
}

/// <summary>Catálogo de productos e inventario; el stock se muta solo por movimientos controlados.</summary>
public interface IProductoRepository
{
    List<Producto> GetProductos(string? tipo = null, string? search = null);
    Producto? GetProducto(long id);
    long SaveProducto(Producto producto);
    void DeleteProducto(long id);
    // Ajuste directo del stock (entradas/salidas); las ventas lo descuentan en la misma transacción.
    void UpdateProductoStock(long id, int newStock);
}

/// <summary>Ventas y sus líneas; guarda cabecera + detalle en transacción (con descuento de stock).</summary>
public interface IVentaRepository
{
    long SaveVenta(Venta venta, List<VentaDetalle> detalles);
    // Siguiente factura consecutiva (misma numeración que el v2).
    string NextVentaNumber();
    List<Venta> GetVentas(DateTime? from = null, DateTime? to = null, string? metodo = null, string? search = null);
    // Aplanado venta+línea para contabilidad: el cierre cuenta cada línea con su cabecera.
    List<(Venta venta, VentaDetalle linea)> GetVentasConLineas(DateTime from, DateTime to, string? metodo = null);
}

/// <summary>Consultas de contabilidad/cierre; agregan por método, día y totales del periodo en SQL.</summary>
public interface IAccountingRepository
{
    List<(Pago pago, ServiceOrder orden)> GetCobrosConOrden(DateTime from, DateTime to, string? metodo = null);
    (decimal efectivo, decimal nequi, decimal otros) VentasPorMetodo(DateTime from, DateTime to);
    (decimal efectivo, decimal nequi, decimal otros) CobrosPorMetodo(DateTime from, DateTime to);
    List<(DateTime fecha, decimal ventas, decimal cobros)> BalanceSeries(DateTime from, DateTime to);
    decimal TotalVentasPeriodo(DateTime from, DateTime to);
    decimal TotalCobrosPeriodo(DateTime from, DateTime to);
}

/// <summary>Respaldo e importación de la base de datos (equivale a Database.Backup/Import del v2).</summary>
public interface IDatabaseBackupService
{
    /// <summary>Copia el archivo de la BD actual a <paramref name="destination"/>.</summary>
    void Backup(string destination);

    /// <summary>Reemplaza la BD actual por <paramref name="sourcePath"/> dejando un respaldo previo automático.</summary>
    bool Import(string sourcePath);
}