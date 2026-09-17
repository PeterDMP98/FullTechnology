// PaymentsService.cs — Cobros de mantenimiento (réplica de CobroForm v2).

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Cobros / pagos de mantenimiento. Replica CobroForm: valida el
// monto contra el saldo, elige concepto (Cobro total o Abono
// parcial) y puede marcar la orden como Entregado al saldarse.
// ============================================================
public class PaymentsService
{
    private readonly IPagoRepository _pagos;
    private readonly IServiceOrderRepository _orders;

    public PaymentsService(IPagoRepository pagos, IServiceOrderRepository orders)
    {
        _pagos = pagos;
        _orders = orders;
    }

    public List<Pago> GetOrderPayments(long serviceOrderId) => _pagos.GetPagos(serviceOrderId);

    public ChargeResult Charge(long serviceOrderId, decimal monto, string metodo, bool markDelivered = false)
    {
        // La orden debe existir: un cobro sin orden no tiene dónde reflejarse.
        var o = _orders.Get(serviceOrderId) ?? throw new EntityNotFoundException($"No existe la orden {serviceOrderId}.");
        // Guard del monto: no hay cobros vacíos ni negativos.
        if (monto <= 0)
            throw new ValidationException("El monto a cobrar debe ser mayor a cero.");
        // Guard del saldo: jamás se cobra más de lo que la orden debe (regla del v2).
        if (monto > o.Balance)
            throw new PaymentExceedsBalanceException(o.Balance);

        var nuevoSaldo = o.Balance - monto;
        // Si el monto cubre todo el saldo se etiqueta "Cobro total"; si no, "Abono parcial" (textos del v2).
        var concepto = monto >= o.Balance ? "Cobro total" : "Abono parcial";

        var pago = new Pago
        {
            ServiceOrderId = o.Id,
            Fecha = DateTime.Now,
            Monto = monto,
            Metodo = metodo,
            Concepto = concepto,
            SaldoRestante = nuevoSaldo
        };
        _pagos.AddPago(pago);
        // El depósito de la orden se incrementa en memoria con el cobro recién registrado.
        o.Deposit += monto;

        // Al quedar en cero la orden puede cerrar el ciclo automáticamente (opción marcada en el form de cobro).
        bool marcaEntregado = false;
        if (o.Status != RepairStatuses.Entregado && nuevoSaldo <= 0 && markDelivered)
        {
            o.Status = RepairStatuses.Entregado;
            o.DeliveredAt = DateTime.Now;
            marcaEntregado = true;
        }

        _orders.Save(o);
        return new ChargeResult(pago, nuevoSaldo, marcaEntregado);
    }

    public void DeletePayment(long pagoId, long serviceOrderId) => _pagos.DeletePago(pagoId, serviceOrderId);

    public void Recalculate(long serviceOrderId) => _pagos.RecalcDeposit(serviceOrderId);
}