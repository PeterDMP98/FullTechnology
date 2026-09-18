// InvoiceReport.cs — Generación de la factura de venta en PDF (QuestPDF) y guardado automático en Documentos\Facturas.
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FULLTECHNOLOGY.Infrastructure.Reporting;

// ============================================================
// FACTURA - Genera el PDF de una venta (QuestPDF, licencia
// Community) y lo deja guardado en Mis Documentos\Facturas.
// El guardado automático se hace al cerrar la compra; el dialogo
// de previsualización (Presentation) permite imprimirla.
// ============================================================
public static class InvoiceReport
{
    /// <summary>Línea de la factura (nombre congelado, cantidad y precio unitario).</summary>
    public sealed record Linea(string Nombre, int Cantidad, decimal Precio);

    /// <summary>Habilita la licencia Community; sin esto QuestPDF rechaza generar documentos.</summary>
    static InvoiceReport()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Guarda la factura en Mis Documentos\Facturas con nombre FACTURA-NNNNNN.pdf
    /// (caracteres no seguros del número se sustituyen). Devuelve la ruta final.
    /// </summary>
    public static string SaveInvoicePdf(string nombreFactura, string cliente, DateTime fecha, string medio,
        IReadOnlyList<Linea> lineas, decimal subtotal, decimal descuento, decimal total)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Facturas");
        Directory.CreateDirectory(dir);
        nombreFactura ??= "";
        var safe = new string(nombreFactura.Where(char.IsLetterOrDigit).ToArray());
        if (safe.Length == 0) safe = DateTime.Now.ToString("yyyyMMddHHmmss");
        var path = Path.Combine(dir, $"FACTURA-{safe}.pdf");
        WriteInvoicePdf(path, nombreFactura, cliente, fecha, medio, lineas, subtotal, descuento, total);
        return path;
    }

    /// <summary>
    /// Genera el PDF de la factura (A4 vertical): encabezado con la casa y el
    /// número de factura, datos del cliente/medio, tabla de ítems con subtotal,
    /// descuento y total, y pie con la fecha de generación.
    /// </summary>
    public static void WriteInvoicePdf(string path, string nombreFactura, string cliente, DateTime fecha, string medio,
        IReadOnlyList<Linea> lineas, decimal subtotal, decimal descuento, decimal total)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FULLTECHNOLOGY").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().Text("Reparación y mantenimiento de dispositivos").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(190).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("FACTURA").FontSize(16).Bold();
                            c.Item().AlignRight().Text(nombreFactura).FontSize(11).Bold();
                            c.Item().AlignRight().Text($"Fecha: {fecha:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(10).Column(data =>
                    {
                        data.Spacing(3);
                        data.Item().Text($"Cliente: {(string.IsNullOrWhiteSpace(cliente) ? "Cliente general" : cliente)}").FontSize(10);
                        data.Item().Text($"Medio de pago: {medio}").FontSize(10);
                    });

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(50);
                            cols.ConstantColumn(85);
                            cols.ConstantColumn(85);
                        });

                        foreach (var h in new[] { "Descripción", "Cant", "Precio", "Total" })
                            table.Cell()
                                .Background(Colors.Grey.Lighten3)
                                .BorderColor(Colors.Grey.Lighten1).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text(h).FontSize(9).Bold();

                        foreach (var l in lineas)
                        {
                            table.Cell().BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text(l.Nombre).FontSize(9);
                            table.Cell().BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text(l.Cantidad.ToString()).FontSize(9).AlignCenter();
                            table.Cell().BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text(Mon(l.Precio)).FontSize(9).AlignRight();
                            table.Cell().BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text(Mon(l.Cantidad * l.Precio)).FontSize(9).AlignRight();
                        }

                        table.Cell().ColumnSpan(3).BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                            .Padding(4).Text("Subtotal").FontSize(9);
                        table.Cell().BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                            .Padding(4).Text(Mon(subtotal)).FontSize(9).AlignRight();

                        if (descuento > 0)
                        {
                            table.Cell().ColumnSpan(3).BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text("Descuento").FontSize(9);
                            table.Cell().BorderColor(Colors.Grey.Lighten2).BorderHorizontal(0.5f).BorderVertical(0.5f)
                                .Padding(4).Text(Mon(descuento)).FontSize(9).AlignRight();
                        }

                        table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten2)
                            .BorderColor(Colors.Grey.Lighten1).BorderHorizontal(0.5f).BorderVertical(0.5f)
                            .Padding(4).Text("TOTAL").FontSize(11).Bold();
                        table.Cell().Background(Colors.Grey.Lighten2)
                            .BorderColor(Colors.Grey.Lighten1).BorderHorizontal(0.5f).BorderVertical(0.5f)
                            .Padding(4).Text(Mon(total)).FontSize(11).Bold().AlignRight();
                    });

                    col.Item().PaddingTop(16).Text("¡Gracias por su compra!").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignRight()
                    .Text($"Impreso el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf(path);
    }

    private static string Mon(decimal v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
}