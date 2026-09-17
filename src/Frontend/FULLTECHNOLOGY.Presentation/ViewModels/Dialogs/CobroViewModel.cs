// CobroViewModel.cs — Diálogo de cobro de una orden de mantenimiento: monto, medio de pago, entrega opcional e historial de pagos.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Diálogo de COBRO (CobroForm v2): muestra total/pagado/saldo,
// monto a cobrar (botón "Cobrar completo"), medio de pago, opción
// de entregar y el historial de pagos de la orden. Usa PaymentsService.
// ============================================================
public partial class CobroViewModel : ViewModelBase
{
    private readonly PaymentsService _payments;
    private readonly CurrencyService _currency;
    private readonly IDialogService _dialogs;

    private ServiceOrder _order = new();
    private long _orderId;
    private decimal _balance;

    public string Title { get; private set; } = "";

    // Resumen del estado financiero de la orden.
    [ObservableProperty]
    public partial string ClienteText { get; set; } = "";

    [ObservableProperty]
    public partial string TotalText { get; set; } = "";

    [ObservableProperty]
    public partial string PagadoText { get; set; } = "";

    [ObservableProperty]
    public partial string SaldoText { get; set; } = "";

    // Monto a cobrar (prellenado con el saldo) y medio de pago elegido.
    [ObservableProperty]
    public partial decimal Monto { get; set; }

    [ObservableProperty]
    public partial string Metodo { get; set; } = PaymentMethods.Efectivo;

    // Al pagar el saldo completo se puede marcar la orden como Entregada.
    [ObservableProperty]
    public partial bool MarcarEntregado { get; set; }

    [ObservableProperty]
    public partial bool CanMarcarEntregado { get; set; }

    // Medios de pago disponibles (los mismos del dominio).
    public IReadOnlyList<string> Metodos { get; } = new[]
    {
        PaymentMethods.Efectivo, PaymentMethods.Nequi, PaymentMethods.Transferencia,
        PaymentMethods.Tarjeta, PaymentMethods.Daviplata, PaymentMethods.Otro
    };

    // Historial de pagos ya registrados en la orden.
    public ObservableCollection<PagoRow> Historial { get; } = new();

    public CobroViewModel(PaymentsService payments, CurrencyService currency, IDialogService dialogs)
    {
        _payments = payments;
        _currency = currency;
        _dialogs = dialogs;
    }

    // Prepara el diálogo con los datos financieros y el historial de la orden.
    public void Initialize(ServiceOrder order)
    {
        _order = order;
        _orderId = order.Id;
        _balance = order.Balance;
        Title = $"Cobrar – {order.OrderNumber}";
        ClienteText = $"Cliente: {order.CustomerName}";
        TotalText = _currency.Fmt(order.Total);
        PagadoText = _currency.Fmt(order.Deposit);
        SaldoText = _currency.Fmt(_balance);
        Monto = Math.Max(0, _balance);
        MarcarEntregado = false;
        CanMarcarEntregado = order.Status != RepairStatuses.Entregado;

        Historial.Clear();
        foreach (var p in _payments.GetOrderPayments(_orderId))
            Historial.Add(new PagoRow(
                p.Fecha.ToString("dd/MM/yyyy HH:mm"),
                _currency.Fmt(p.Monto),
                p.Metodo,
                p.Concepto,
                _currency.Fmt(p.SaldoRestante)));
    }

    // El botón "Cobrar completo" llena el monto con todo el saldo pendiente.
    [RelayCommand]
    private void CobrarCompleto() => Monto = Math.Min(Math.Max(0, _balance), _balance);

    /// <summary>Registra el cobro (validando monto/saldo como v2) y cierra con OK.</summary>
    public async Task<bool> ConfirmarAsync()
    {
        if (Monto <= 0)
        {
            await _dialogs.ShowMessageAsync("Cobro", "El monto a cobrar debe ser mayor a cero.");
            return false;
        }
        if (Monto > _balance)
        {
            await _dialogs.ShowMessageAsync("Cobro", $"El monto no puede superar el saldo pendiente ({_currency.Fmt(_balance)}).");
            return false;
        }

        bool willFinish = Monto >= _balance;
        bool shouldMark = MarcarEntregado;

        // Si quedó totalmente pagada y no se marcó la casilla, se pregunta (v2).
        if (willFinish && _order.Status != RepairStatuses.Entregado && !MarcarEntregado)
        {
            shouldMark = await _dialogs.ConfirmAsync("Entrega", "La orden quedó totalmente pagada. ¿Desea marcarla como 'Entregado'?");
        }

        try
        {
            var r = _payments.Charge(_orderId, Monto, Metodo, shouldMark);
            await _dialogs.ShowMessageAsync(
                "Confirmación",
                $"Cobro registrado correctamente.\n\nMonto: {_currency.Fmt(Monto)}\nMedio: {Metodo}\nNuevo saldo: {_currency.Fmt(r.NuevoSaldo)}");
            if (r.MarcaEntregado)
                await _dialogs.ShowMessageAsync("Confirmación", "La orden ha sido marcada como Entregada.");
            return true;
        }
        catch (ValidationException ex)
        {
            await _dialogs.ShowMessageAsync("Cobro", ex.Message);
            return false;
        }
        catch (PaymentExceedsBalanceException ex)
        {
            await _dialogs.ShowMessageAsync("Cobro", ex.Message);
            return false;
        }
    }
}