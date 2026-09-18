// InvoicePrinter.cs — Impresión de la factura en Windows: selección de impresora y envío al documento.
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Infrastructure.Reporting;

namespace FULLTECHNOLOGY.Presentation.Services;

// ============================================================
// IMPRIMIR FACTURA - La app es Windows (WinExe), así que la
// impresión usa WinForms (System.Drawing.Printing): un PrintDialog
// elige la impresora y un PrintDocument pinta el reporte en texto
// monoespaciado, paginando si se pasa de una hoja.
// ============================================================
public static class InvoicePrinter
{
    /// <summary>
    /// Muestra el diálogo de impresión de Windows y, al confirmar, imprime la
    /// factura como texto monoespaciado (una línea por campo). Devuelve false
    /// si el usuario cancela o no hay impresoras disponibles.
    /// </summary>
    public static bool Print(Venta venta, IReadOnlyList<InvoiceReport.Linea> lineas, CurrencyService currency)
    {
        using var dialog = new PrintDialog();
        if (dialog.ShowDialog() != DialogResult.OK) return false;

        var lines = BuildLines(venta, lineas, currency);
        using var doc = new PrintDocument
        {
            PrinterSettings = dialog.PrinterSettings,
            DocumentName = $"Factura {venta.VentaNumber}"
        };

        var printed = 0;
        doc.PrintPage += (sender, e) =>
        {
            var g = e.Graphics ?? throw new InvalidOperationException("No hay contexto gráfico de impresión.");
            using var font = new Font("Courier New", 9f);
            var lineHeight = font.GetHeight(g);
            var perPage = Math.Max(1, (int)Math.Floor(e.MarginBounds.Height / lineHeight) - 1);

            var y = (float)e.MarginBounds.Top;
            var count = 0;
            while (printed + count < lines.Length && count < perPage)
            {
                g.DrawString(lines[printed + count], font, Brushes.Black, e.MarginBounds.Left, y);
                y += lineHeight;
                count++;
            }
            printed += count;
            e.HasMorePages = printed < lines.Length;
        };

        try
        {
            doc.Print();
            return true;
        }
        catch (System.Drawing.Printing.InvalidPrinterException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Reporte plano que luego se pinta: cabecera, datos, ítems y totales.
    private static string[] BuildLines(Venta venta, IReadOnlyList<InvoiceReport.Linea> lineas, CurrencyService currency)
    {
        var list = new List<string>
        {
            "               FULLTECHNOLOGY",
            "       Reparacion y mantenimiento de dispositivos",
            "==================================================",
            $"  FACTURA  {venta.VentaNumber}",
            $"  Fecha: {venta.Fecha:dd/MM/yyyy HH:mm}",
            $"  Cliente: {(string.IsNullOrWhiteSpace(venta.ClienteNombre) ? "Cliente general" : venta.ClienteNombre)}",
            $"  Medio: {venta.MetodoPago}",
            "--------------------------------------------------"
        };

        foreach (var l in lineas)
        {
            var nombre = l.Nombre.Length > 46 ? l.Nombre[..45] + "…" : l.Nombre;
            var precio = currency.Fmt(l.Precio);
            list.Add($"{nombre.PadRight(36)}{l.Cantidad.ToString().PadLeft(3)}  {precio.PadLeft(14)}  {currency.Fmt(l.Cantidad * l.Precio).PadLeft(12)}");
        }

        list.Add("--------------------------------------------------");
        list.Add($"  Subtotal: {currency.Fmt(venta.Subtotal).PadLeft(21)}");
        if (venta.Descuento > 0)
            list.Add($"  Descuento: {currency.Fmt(venta.Descuento).PadLeft(19)}");
        list.Add($"  TOTAL: {currency.Fmt(venta.Total).PadLeft(24)}");
        list.Add("");
        list.Add("        Gracias por su compra.");
        return list.ToArray();
    }
}