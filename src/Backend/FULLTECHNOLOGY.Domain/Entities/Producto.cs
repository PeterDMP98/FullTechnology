// Producto.cs — Producto del inventario (repuesto o accesorio) con stock controlado.

namespace FULLTECHNOLOGY.Domain.Entities;

using FULLTECHNOLOGY.Domain.Exceptions;

/// <summary>Artículo vendible; el stock es la única fuente de verdad para las ventas.</summary>
public class Producto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    // Default el tipo más común; el form del v2 lo deja en Accesorio.
    public string Tipo { get; set; } = "Accesorio";
    // Costo y precio se guardan por separado: el reporte de rentabilidad necesita ambos.
    public decimal Costo { get; set; }
    public decimal PrecioVenta { get; set; }
    public string? Proveedor { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string? Garantia { get; set; }
    // "Ubicado" es la referencia física (gaveta/oferta); opcional.
    public string? Ubicado { get; set; }
    public int Stock { get; set; }

    // Etiqueta "Nombre · Código"; sin código se muestra solo el nombre (productos pre-numeración).
    public string NombreCompleto => string.IsNullOrWhiteSpace(Codigo) ? Nombre : $"{Nombre} · {Codigo}";

    /// <summary>Reglas de negocio de un producto del inventario.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            throw new ValidationException("El nombre del producto es obligatorio.");
        // El tipo se limita al catálogo (mismo criterio que el CHECK del esquema).
        if (Tipo != ProductTypes.Repuesto && Tipo != ProductTypes.Accesorio)
            throw new ValidationException($"El tipo debe ser '{ProductTypes.Repuesto}' o '{ProductTypes.Accesorio}'.");
        // Físicamente no puede haber menos de cero unidades, ni siquiera tras un ajuste manual.
        if (Stock < 0)
            throw new ValidationException("El stock no puede ser negativo.");
        // Precios negativos romperían rentabilidad y totales; se rechazan antes de persistir.
        if (Costo < 0 || PrecioVenta < 0)
            throw new ValidationException("Los valores de costo y precio no pueden ser negativos.");
    }

    /// <summary>Valida que se pueda vender la cantidad pedida (regla central de stock).</summary>
    public void EnsureStock(int cantidad)
    {
        // Ventas de cero o negativas no tienen sentido; se descartan aquí aunque la UI las filtre.
        if (cantidad <= 0)
            throw new ValidationException($"Cantidad inválida para '{Nombre}'.");
        // Se valida contra el catálogo actual (no contra lo que digitó el cajero) antes de descontar.
        if (Stock < cantidad)
            throw new InsufficientStockException(Nombre, Stock, cantidad);
    }
}