// PdfReport.cs — Generación de reportes PDF con QuestPDF (A4 horizontal, tablas con encabezado y filas de totales).
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FULLTECHNOLOGY.Infrastructure.Reporting;

// ============================================================
// PDF - Reportes PDF con QuestPDF (licencia Community).
// Página A4 horizontal, tablas con bordes y encabezado en
// negrita; el texto envuelve dentro de la celda (medición por
// glifo real) y los saltos de página los gestiona la librería.
// (Ruta idéntica de lógica a la v2.x Services/Pdf.cs)
// ============================================================

/// <summary>
/// Exporta reportes a PDF con QuestPDF, reproduciendo el contenido del cierre
/// de la v2.x (Services/Pdf.cs). Usa página A4 horizontal: encabezado con
/// título y período, tabla con bordes (headers en negrita), alineación derecha
/// automática para columnas numéricas y filas de totales en una sola celda a
/// todo lo ancho con su etiqueta y valor en el mismo renglón.
/// </summary>
public static class PdfReport
{
    /// <summary>Habilita la licencia Community; sin esto QuestPDF rechaza generar documentos.</summary>
    static PdfReport()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // Genera el PDF del cierre contable (tabla de ingresos por tipo).
    /// <summary>
    /// Genera el PDF del cierre contable: una tabla con las columnas fijas
    /// (Tipo de ingreso, Efectivo, Nequi, Otros, Total) y los anchos del layout
    /// de la v2.x. Convierte cada fila de datos al formato genérico de WriteTable.
    /// </summary>
    public static void WriteCierre(string path, string titulo, string periodo,
        IReadOnlyList<(string concepto, string efectivo, string nequi, string otros, string total)> filas)
    {
        var headers = new[] { "Tipo de ingreso", "Efectivo", "Nequi", "Otros", "Total" };
        var widths = new[] { 170f, 84f, 84f, 84f, 88f };
        var rows = filas.Select(f => (IReadOnlyList<string>)new[] { f.concepto, f.efectivo, f.nequi, f.otros, f.total }).ToList();
        WriteTable(path, titulo, periodo, headers, widths, rows);
    }

    // Genera un PDF con cualquier tabla (mismos encabezados que la lista visible).
    /// <summary>
    /// Genera un PDF con una tabla genérica (cualquier listado del programa cuyo
    /// encabezado coincida con el que ve el usuario). Anchos de columna fijos,
    /// alineación derecha computada para columnas numéricas, y manejo de filas
    /// de totales mediante IsSummaryRow.
    /// </summary>
    public static void WriteTable(string path, string titulo, string sublinea,
        IReadOnlyList<string> headers, IReadOnlyList<float> widths, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        int n = headers.Count;
        var data = rows.Select(r =>
        {
            var a = new string[n];
            for (int i = 0; i < n; i++) a[i] = i < r.Count ? r[i] : "";
            return a;
        }).ToList();

        // Columnas numéricas para alinear a la derecha
        // Se decide por dos criterios: encabezados inequívocamente monetarios
        // (monto/total/cantidad/ventas/cobros/efectivo/nequi/otros) o —si no
        // aplica— que TODOS los valores de la columna parezcan numéricos
        // (LooksNumeric). Así las columnas de dinero salen alineadas a la
        // derecha igual que en el Excel de la v2.x.
        var right = new bool[n];
        for (int c = 0; c < n; c++)
        {
            var h = headers[c].ToLowerInvariant();
            right[c] = h.Contains("monto") || h.Contains("total") || h.Contains("cantidad")
                || h.Contains("ventas") || h.Contains("cobros") || h.Contains("efectivo")
                || h.Contains("nequi") || h.Contains("otros")
                || (data.Count > 0 && data.All(d => LooksNumeric(d[c])));
        }

        Document.Create(container =>
        {
            // Página A4 apaisada: el ancho acomoda tablas con varias columnas
            // monetarias mejor que un A4 vertical (misma elección de la v2.x).
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                // Encabezado repetido en todas las páginas que ocupe el reporte:
                // título en grande y el período/sublínea debajo, en gris.
                page.Header().Column(col =>
                {
                    col.Item().Text(titulo).FontSize(16).Bold().FontColor(Colors.Black);
                    col.Item().Text(sublinea).FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingBottom(10);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (int c = 0; c < n; c++)
                            cols.RelativeColumn(widths[c] <= 0 ? 1 : widths[c]);
                    });

                    // Fila de cabecera: fondo gris claro, bordes finos y negrita,
                    // igual que el encabezado del Excel generado en paralelo.
                    for (int c = 0; c < n; c++)
                        table.Cell()
                            .Background(Colors.Grey.Lighten3)
                            .BorderColor(Colors.Grey.Lighten1).BorderHorizontal(0.5f).BorderVertical(0.5f)
                            .Padding(4)
                            .Text(headers[c]).FontSize(9).Bold().AlignLeft();

                    foreach (var row in data)
                    {
                        if (IsSummaryRow(row[0]))
                        {
                            // Fila de totales: una sola celda de ancho completo con la
                            // etiqueta a la izquierda y el valor a la derecha (mismo renglón).
                            table.Cell()
                                .ColumnSpan((uint)n)
                                .Background(Colors.Grey.Lighten2)
                                .BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4)
                                .Row(r =>
                                {
                                    r.RelativeItem().Text(row[0]).FontSize(9).Bold();
                                    r.ConstantItem(140).AlignRight().Text(row[^1]).FontSize(9).Bold().AlignRight();
                                });
                            continue;
                        }

                        for (int c = 0; c < n; c++)
                        {
                            var container = table.Cell()
                                .BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f);
                            var t = container.Padding(4).Text(row[c]).FontSize(9);
                            if (right[c]) t.AlignRight(); else t.AlignLeft();
                        }
                    }
                });

                // Pie de página en cada hoja con la fecha/hora de generación, para poder
                // diferenciar impresiones del mismo cierre.
                page.Footer().Column(f =>
                {
                    f.Item().PaddingTop(8).AlignRight()
                        .Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf(path);
    }

    // Determina si una cadena representa un número tras quitar símbolos de moneda
    // ($, COP, €) y separadores de miles (puntos y comas) casuísticos; se usa
    // para alinear a la derecha columnas cuyo encabezado no lo deja claro.
    static bool LooksNumeric(string s)
    {
        var t = s.Replace("$", "").Replace("COP", "").Replace("€", "").Replace(" ", "")
                 .Replace(".", "").Replace(",", "").Trim();
        return t.Length > 0 && t.All(char.IsDigit);
    }

    // Detecta filas de totales por su etiqueta inicial; la lista coincide con la de
    // ExcelReport e incluye los conceptos fijos del cierre (Ventas de accesorios
    // y Cobros de mantenimiento). Estas filas se renderizan en una sola celda
    // a todo lo ancho en lugar de columna por columna.
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