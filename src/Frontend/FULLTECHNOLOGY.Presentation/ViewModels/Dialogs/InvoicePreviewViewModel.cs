// InvoicePreviewViewModel.cs — Previsualización de la factura recién generada al terminar la compra, antes de imprimirla.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Infrastructure.Reporting;
using FULLTECHNOLOGY.Presentation.Services;
// Alias con nombre propio: el namespace ...ViewModels.Venta gana la búsqueda del nombre 'Venta'.
using VentaFactura = FULLTECHNOLOGY.Domain.Entities.Venta;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// PREVISUALIZACIÓN DE FACTURA - Muestra el resumen de la venta
// (datos, ítems y totales) junto con la ruta del PDF generado,
// y ofrece "Imprimir factura" (selección de impresora vía
// InvoicePrinter) o "Cancelar". La vista cierra con el valor de
// Aceptada según se haya impreso o no.
// ============================================================
public partial class InvoicePreviewViewModel : ViewModelBase
{
    private readonly VentaFactura _venta;
    private readonly IReadOnlyList<InvoiceReport.Linea> _lineas;
    private readonly CurrencyService _currency;

    public string Title => $"Factura {_venta.VentaNumber}";
    public string ClienteText => string.IsNullOrWhiteSpace(_venta.ClienteNombre) ? "Cliente general" : _venta.ClienteNombre;
    public string FechaText => _venta.Fecha.ToString("dd/MM/yyyy HH:mm");
    public string MedioText => _venta.MetodoPago;
    public string SubtotalText => _currency.Fmt(_venta.Subtotal);
    public string DescuentoText => _currency.Fmt(_venta.Descuento);
    public string TotalText => _currency.Fmt(_venta.Total);
    public string PdfText { get; }

    public ObservableCollection<InvoiceLineaViewModel> Lineas { get; } = new();

    /// <summary>Indica si la impresión se confirmó (la vista cierra con true).</summary>
    public bool Aceptada { get; private set; }

    public InvoicePreviewViewModel(VentaFactura venta, IReadOnlyList<InvoiceReport.Linea> lineas, CurrencyService currency, string pdfPath)
    {
        _venta = venta;
        _lineas = lineas;
        _currency = currency;
        PdfText = pdfPath;
        foreach (var l in lineas)
            Lineas.Add(new InvoiceLineaViewModel(l, currency));
    }

    /// <summary>
    /// Imprime la factura con la impresora elegida en el diálogo de Windows.
    /// Si el usuario cancela la selección (o no imprime) Aceptada queda en false.
    /// </summary>
    [RelayCommand]
    public void Imprimir()
    {
        Aceptada = InvoicePrinter.Print(_venta, _lineas, _currency);
    }
}

/// <summary>Fila del detalle de la factura (descripción, cantidad, precio y total de línea).</summary>
public class InvoiceLineaViewModel : ViewModelBase
{
    public string Nombre { get; }
    public string Cantidad { get; }
    public string Precio { get; }
    public string Total { get; }

    public InvoiceLineaViewModel(InvoiceReport.Linea linea, CurrencyService currency)
    {
        Nombre = linea.Nombre;
        Cantidad = linea.Cantidad.ToString();
        Precio = currency.Fmt(linea.Precio);
        Total = currency.Fmt(linea.Cantidad * linea.Precio);
    }
}