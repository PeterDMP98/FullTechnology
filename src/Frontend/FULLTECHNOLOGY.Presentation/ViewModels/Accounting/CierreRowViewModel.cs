// CierreRowViewModel.cs — Fila del catálogo de cierre contable con celdas ya formateadas y resaltado de filas de totales.
namespace FULLTECHNOLOGY.Presentation.ViewModels.Accounting;

/// <summary>Celda de una fila de cierre (texto ya formateado).</summary>
public sealed class CierreCellViewModel
{
    public string Value { get; }

    public CierreCellViewModel(string value) => Value = value;
}

// ============================================================
// Fila del catálogo de cierre. Las celdas llegan ya formateadas
// desde AccountingService (conversión de moneda incluida); se
// resaltan las filas de totales del periodo.
// ============================================================
public sealed class CierreRowViewModel
{
    public IReadOnlyList<CierreCellViewModel> Cells { get; }

    // Indica si la fila es un TOTAL o FACTURAS (se pinta resaltada en la tabla).
    public bool EsTotal { get; }

    public CierreRowViewModel(string[] cells)
    {
        Cells = cells.Select(c => new CierreCellViewModel(c)).ToArray();
        var label = cells.Length > 0 ? (cells[0] ?? "").Trim() : "";
        EsTotal = label.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("FACTURAS", StringComparison.OrdinalIgnoreCase);
    }
}