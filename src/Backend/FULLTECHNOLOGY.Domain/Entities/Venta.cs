// Venta.cs — Cabecera de una venta de accesorios y sus líneas de detalle (factura).

namespace FULLTECHNOLOGY.Domain.Entities;

/// <summary>Cabecera de venta; los totales deben cuadrar siempre con la suma de sus líneas.</summary>
public class Venta
{
    public long Id { get; set; }
    public string VentaNumber { get; set; } = "";
    public long? ClienteId { get; set; }
    // Nombre denormalizado: la factura conserva el nombre aunque luego se edite el catálogo de clientes.
    public string ClienteNombre { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = "";
    public string? Nota { get; set; }
}

/// <summary>Línea de venta; congela la foto del producto (precio/nombre) al momento de facturar.</summary>
public class VentaDetalle
{
    public long Id { get; set; }
    public long VentaId { get; set; }
    public long ProductoId { get; set; }
    public string ProductoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    // Precio congelado × cantidad: las facturas viejas no cambian si el catálogo sube después.
    public decimal SubtotalLinea => Cantidad * PrecioUnitario;
}