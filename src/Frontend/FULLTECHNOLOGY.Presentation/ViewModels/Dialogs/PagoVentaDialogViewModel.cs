// PagoVentaDialogViewModel.cs — Diálogo de pago de una venta de accesorios: muestra el total y pide el medio de pago.
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

/// <summary>
/// PagoVentaDialog v2: muestra el total a cobrar y pide el medio de pago
/// (los mismos 5 del WinForms: Efectivo, Nequi, Transferencia, Tarjeta,
/// Daviplata).
/// </summary>
public partial class PagoVentaDialogViewModel : ViewModelBase
{
    private readonly CurrencyService _currency;

    // Medios disponibles para la venta de accesorios.
    public IReadOnlyList<string> MetodoOptions { get; } = MethodosVenta;

    /// <summary>Medios de pago habilitados para la venta de accesorios.</summary>
    public static IReadOnlyList<string> MethodosVenta { get; } = new[]
    {
        PaymentMethods.Efectivo, PaymentMethods.Nequi, PaymentMethods.Transferencia,
        PaymentMethods.Tarjeta, PaymentMethods.Daviplata
    };

    // Medio elegido (lo lee el VM de venta al cerrar el diálogo).
    [ObservableProperty]
    public partial string SelectedMetodo { get; set; } = PaymentMethods.Efectivo;

    // Total a cobrar, ya formateado con la divisa activa.
    public string TotalText { get; private set; } = "";

    public PagoVentaDialogViewModel(CurrencyService currency)
    {
        _currency = currency;
    }

    public void Initialize(decimal total) => TotalText = _currency.Fmt(total);
}