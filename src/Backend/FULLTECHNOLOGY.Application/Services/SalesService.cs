// SalesService.cs — Punto de venta de accesorios (réplica de VentaForm v2).

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Venta de accesorios / punto de venta (VentaForm v2): valida
// stock antes de guardar, calcula subtotal/descuento/total y
// persiste venta + detalle en transacción (el repositorio
// descuenta el stock). El precio se congela del producto al
// momento de la venta.
// ============================================================
public class SalesService
{
    private readonly IVentaRepository _ventas;
    private readonly IProductoRepository _productos;

    public SalesService(IVentaRepository ventas, IProductoRepository productos)
    {
        _ventas = ventas;
        _productos = productos;
    }

    public string NextVentaNumber() => _ventas.NextVentaNumber();

    public List<Venta> Search(DateTime? from = null, DateTime? to = null, string? metodo = null, string? search = null) =>
        _ventas.GetVentas(from, to, metodo, search);

    // Aplanado venta+línea para el cierre contable (una fila de detalle por entrada del catálogo).
    public List<(Venta venta, VentaDetalle linea)> SearchWithLines(DateTime from, DateTime to, string? metodo = null) =>
        _ventas.GetVentasConLineas(from, to, metodo);

    // Líneas de una venta concreta (reimpresión/exportación de una factura del historial).
    public List<VentaDetalle> GetVentaLineas(long ventaId) =>
        _ventas.GetVentaConLineas(ventaId);

    public Venta CreateSale(IEnumerable<SaleItem> items, decimal descuento, string metodoPago, long? clienteId = null, string? clienteNombre = null)
    {
        // Se descartan nulos por seguridad y se exige al menos un ítem antes de tocar la BD.
        var lista = items?.Where(i => i != null).ToList() ?? new List<SaleItem>();
        if (lista.Count == 0)
            throw new ValidationException("El carrito está vacío.");
        // Un descuento negativo inflaría el total; se rechaza como entrada inválida.
        if (descuento < 0)
            throw new ValidationException("El descuento no puede ser negativo.");

        var detalles = new List<VentaDetalle>();
        decimal subtotal = 0;
        foreach (var item in lista)
        {
            // Se revalida contra el catálogo actual: niega faltantes y stock insuficiente en el acto.
            var p = _productos.GetProducto(item.ProductoId)
                ?? throw new EntityNotFoundException($"El producto {item.ProductoId} ya no existe.");
            p.EnsureStock(item.Cantidad);

            subtotal += p.PrecioVenta * item.Cantidad;
            detalles.Add(new VentaDetalle
            {
                ProductoId = p.Id,
                ProductoNombre = p.Nombre,
                // El precio se congela aquí: una subida posterior del catálogo no altera facturas viejas.
                PrecioUnitario = p.PrecioVenta,
                Cantidad = item.Cantidad
            });
        }

        // Total nunca negativo: un descuento mayor que el subtotal se trunca a cero.
        var total = Math.Max(0, subtotal - descuento);
        var venta = new Venta
        {
            VentaNumber = _ventas.NextVentaNumber(),
            Fecha = DateTime.Now,
            Subtotal = subtotal,
            Descuento = descuento,
            Total = total,
            MetodoPago = metodoPago,
            ClienteId = clienteId,
            ClienteNombre = clienteNombre ?? ""
        };

        // El repositorio persiste cabecera + líneas en una sola transacción (y descuenta el stock).
        _ventas.SaveVenta(venta, detalles);
        return venta;
    }
}