// HistorialVentaRowViewModel.cs — Fila del historial de ventas: columnas expuestas planas con montos formateados y comando para ver el detalle de la factura.
using System.Windows.Input;
using FULLTECHNOLOGY.Application.Services;
using Sale = FULLTECHNOLOGY.Domain.Entities.Venta;

namespace FULLTECHNOLOGY.Presentation.ViewModels.HistorialVenta;

// ============================================================
// Fila del historial de ventas. Las columnas monetarias salen
// ya formateadas con CurrencyService; VerVentaCommand muestra
// el detalle de la factura (paridad del MessageBox del v2).
// ============================================================
public class HistorialVentaRowViewModel : ViewModelBase
{
    private readonly CurrencyService _currency;

    public Sale Venta { get; }
    public ICommand VerCommand { get; }

    // Columnas expuestas planas para el binding de la tabla.
    public string Factura => Venta.VentaNumber;
    public string Fecha => Venta.Fecha.ToString("dd/MM/yyyy HH:mm");
    public string Cliente => Venta.ClienteNombre;
    public string Subtotal => _currency.Fmt(Venta.Subtotal);
    public string Descuento => _currency.Fmt(Venta.Descuento);
    public string Total => _currency.Fmt(Venta.Total);
    public string Medio => Venta.MetodoPago;

    // Texto del detalle que muestra VerCommand (paridad con el MessageBox del v2).
    public string DetalleTexto =>
        $"Factura: {Venta.VentaNumber}\n" +
        $"Fecha: {Venta.Fecha:dd/MM/yyyy HH:mm}\n" +
        $"Cliente: {Venta.ClienteNombre}\n\n" +
        $"Subtotal: {Subtotal}\n" +
        $"Descuento: {Descuento}\n" +
        $"TOTAL: {Total}\n" +
        $"Medio: {Medio}";

    public HistorialVentaRowViewModel(Sale venta, CurrencyService currency, ICommand verCommand)
    {
        Venta = venta;
        _currency = currency;
        VerCommand = verCommand;
    }
}