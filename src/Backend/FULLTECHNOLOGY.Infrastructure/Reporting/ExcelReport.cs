// ExcelReport.cs — Generación de reportes Excel (.xlsx) con ClosedXML replicando el contenido del cierre en la v2.x.
using ClosedXML.Excel;

namespace FULLTECHNOLOGY.Infrastructure.Reporting;

// ============================================================
// EXCEL - Genera un libro .xlsx con ClosedXML y el mismo
// contenido que el PDF: título, período, encabezados en
// negrita y las filas de totales resaltadas (negrita + fondo).
// (Ruta idéntica de lógica a la v2.x Services/Excel.cs)
// ============================================================

/// <summary>
/// Exporta tablas de reporte a Excel (.xlsx) con ClosedXML, como alternativa al
/// PDF del cierre. Replica el layout de la v2.x (Services/Excel.cs): título y
/// período en las dos primeras filas, encabezados en negrita con fondo gris,
/// y filas de totales resaltadas. Los valores numéricos se escriben como celdas
/// numéricas reales (no texto) para que Excel pueda sumarlos/filtrarlos.
/// </summary>
public static class ExcelReport
{
    /// <summary>
    /// Vuelca una tabla arbitraria a un libro: fila 1 = título (combinada),
    /// fila 2 = sublínea/período (combinada, en gris), fila 3 = encabezados,
    /// y desde la fila 4 los datos. Detecta automatáticamente las filas de
    /// totales (IsSummaryRow) y las resalta. Se congela la fila de encabezados
    /// para que al desplazarse permanezca visible.
    /// </summary>
    public static void WriteTable(string path, string titulo, string sublinea,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        int n = headers.Count;
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cierre");

        // Título y período
        ws.Cell(1, 1).Value = titulo;
        ws.Range(1, 1, 1, n).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Row(1).Height = 24;

        ws.Cell(2, 1).Value = sublinea;
        ws.Range(2, 1, 2, n).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(2, 1).Style.Font.FontSize = 11;

        // Encabezados
        const int headRow = 3;
        for (int c = 0; c < n; c++)
        {
            var cell = ws.Cell(headRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E0E0");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Datos + filas de totales
        int r = headRow + 1;
        foreach (var row in rows)
        {
            // Una fila es "de total" cuando su primera etiqueta coincide con los
            // rótulos de resumen de la v2.x (TOTAL, TOTAL DEL PERIODO, …); esas
            // filas se pintan en negrita y con fondo para separarlas visualmente.
            bool summary = IsSummaryRow(row.Count > 0 ? row[0] ?? "" : "");
            for (int c = 0; c < n; c++)
            {
                var val = c < row.Count ? (row[c] ?? "") : "";
                var cell = ws.Cell(r, c + 1);
                // Si el dato es numérico se escribe como número (parseo con
                // cultura invariante para no depender del idioma del SO); si no,
                // se deja como texto. Esto permite sumar las celdas en Excel.
                if (decimal.TryParse(val, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var num))
                    cell.Value = num;
                else
                    cell.Value = val;

                if (summary)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE");
                }
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            r++;
        }

        // Anchos de columna según el contenido
        for (int c = 1; c <= n; c++) ws.Column(c).AdjustToContents(4, r - 1);

        ws.SheetView.FreezeRows(headRow);
        wb.SaveAs(path);
    }

    /// <summary>
    /// Detecta filas de totales por la etiqueta de su primera columna. La lista
    /// de rótulos es la MISMA que usaba la v2.x para los cierres: totales
    /// genéricos, totales de período/facturas/ingresos y los conceptos fijos de
    /// ingresos (Ventas de accesorios / Cobros de mantenimiento). Si ninguna
    /// etiqueta coincide, la fila se trata como dato normal.
    /// </summary>
    static bool IsSummaryRow(string label)
    {
        if (string.IsNullOrEmpty(label)) return false;
        var l = label.Trim();
        return l.Equals("TOTAL", StringComparison.OrdinalIgnoreCase)
            || l.Equals("TOTAL DEL PERIODO", StringComparison.OrdinalIgnoreCase)
            || l.Equals("TOTAL DE FACTURAS", StringComparison.OrdinalIgnoreCase)
            || l.Equals("TOTAL INGRESOS DEL PERIODO", StringComparison.OrdinalIgnoreCase)
            || l.StartsWith("FACTURAS", StringComparison.OrdinalIgnoreCase)
            || l.Equals("Ventas (accesorios)", StringComparison.OrdinalIgnoreCase)
            || l.Equals("Cobros de mantenimiento", StringComparison.OrdinalIgnoreCase);
    }
}