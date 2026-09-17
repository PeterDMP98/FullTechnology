// Results.cs — DTOs inmutables de salida de los servicios de Application; sin SQL ni UI.

using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// DTOs que producen los servicios de la capa Application.
// Son inmutables y no exponen SQL ni UI.
// ============================================================

/// <summary>Métricas del dashboard de Mantenimiento (siempre globales).</summary>
public sealed record OrderStats(
    int Total,
    int Recibido,
    int EnReparacion,
    int ListosParaEntregar,
    int Entregados)
{
    /// <summary>La card "Listos" suma Reparado + Listo para entregar (comportamiento v2).</summary>
    public static OrderStats FromOrders(IEnumerable<ServiceOrder> all) => new(
        Total: all.Count(),
        Recibido: all.Count(o => o.Status == Domain.RepairStatuses.Recibido),
        EnReparacion: all.Count(o => o.Status == Domain.RepairStatuses.EnReparacion),
        ListosParaEntregar: all.Count(o => o.Status == Domain.RepairStatuses.Reparado || o.Status == Domain.RepairStatuses.ListoParaEntregar),
        Entregados: all.Count(o => o.Status == Domain.RepairStatuses.Entregado));
}

/// <summary>Resultado de registrar un cobro.</summary>
public sealed record ChargeResult(Pago Pago, decimal NuevoSaldo, bool MarcaEntregado);

/// <summary>Línea de un catálogo de cierre, ya formateada para exportar.</summary>
public sealed record ClosingCatalog(
    string[] Headers,
    float[] Widths,
    IReadOnlyList<string[]> Rows);

/// <summary>Totales del periodo (Ventas + Cobros), con conteo de facturas.</summary>
public sealed record ClosingTotals(decimal Ventas, decimal Cobros, int FacturasAcc, int FacturasMant)
{
    // Totales derivados que la exportación usa para las filas de resumen.
    public decimal Total => Ventas + Cobros;
    public int Facturas => FacturasAcc + FacturasMant;
}

/// <summary>Resultado completo del cierre: totales, catálogo por vista y filas listas para exportar.</summary>
public sealed record ClosingSummary(
    ClosingTotals Totals,
    ClosingCatalog Catalogo,
    IReadOnlyList<string[]> ExportRows);

/// <summary>Ítem del carrito de venta.</summary>
public sealed record SaleItem(long ProductoId, int Cantidad);