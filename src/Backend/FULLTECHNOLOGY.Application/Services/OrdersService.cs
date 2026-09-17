// OrdersService.cs — Casos de uso de órdenes de mantenimiento (réplica de OrderForm v2).

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Casos de uso de órdenes de mantenimiento. La lógica de
// relación de cliente, validación y abono inicial replican el
// comportamiento de OrderForm (FULLTECHNOLOGY v2). Reglas de
// Total/Balance viven en Domain (ServiceOrder).
// ============================================================
public class OrdersService
{
    private readonly IServiceOrderRepository _orders;
    private readonly IPagoRepository _pagos;
    private readonly IClienteRepository _clientes;

    public OrdersService(IServiceOrderRepository orders, IPagoRepository pagos, IClienteRepository clientes)
    {
        _orders = orders;
        _pagos = pagos;
        _clientes = clientes;
    }

    public List<ServiceOrder> Search(string? search = null, string? status = null) =>
        _orders.GetAll(search, status);

    public ServiceOrder? Get(long id) => _orders.Get(id);

    // Panel de cobros y recordatorios: solo interesan las órdenes con saldo sin liquidar.
    public List<ServiceOrder> GetPendingBalance() => _orders.GetAllPendingBalance();

    public OrderStats GetStats() => OrderStats.FromOrders(_orders.GetAll());

    /// <summary>
    /// Valida y guarda una orden. En órdenes nuevas sin ClienteId intenta
    /// relacionar con un cliente comprador existente (documento o celular);
    /// si trae abono inicial y no hay pagos, lo registra como "Abono inicial".
    /// El depósito siempre se recalcula desde el historial de pagos.
    /// </summary>
    public ServiceOrder SaveOrder(ServiceOrder order)
    {
        order.Validate();

        // Sólo en órdenes NUEVAS sin cliente se autovincula un comprador existente (doc/celular), como v2.
        if (order.Id == 0 && order.ClienteId == null)
            LinkComprador(order);

        if (order.Id == 0)
        {
            // Las órdenes nuevas sellan la fecha de recibo y arrancan siempre en "Recibido".
            if (order.ReceivedAt == default)
                order.ReceivedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(order.Status))
                order.Status = RepairStatuses.Recibido;
        }

        _orders.Save(order);

        // Abono inicial: solo para órdenes nuevas sin pagos previos (comportamiento v2)
        if (order.Id != 0 && order.Deposit > 0 && _pagos.GetPagos(order.Id).Count == 0)
        {
            // Saldo tras el abono; nunca negativo aunque el abono supere el total.
            var saldo = Math.Max(0, order.Total - order.Deposit);
            _pagos.AddPago(new Pago
            {
                ServiceOrderId = order.Id,
                Fecha = DateTime.Now,
                Monto = order.Deposit,
                // Si el método quedó "Pendiente" el abono se asume en efectivo (fallback del v2).
                Metodo = order.PaymentMethod == PaymentMethods.Pendiente ? PaymentMethods.Efectivo : order.PaymentMethod,
                Concepto = "Abono inicial",
                SaldoRestante = saldo
            });
        }

        // El depósito final es siempre la suma real de pagos (no el campo digitado), calculado tras guardar.
        _pagos.RecalcDeposit(order.Id);
        order.Deposit = _pagos.GetPagos(order.Id).Sum(p => p.Monto);
        return order;
    }

    public void Delete(long id) => _orders.Delete(id);

    public void MarkAsDelivered(long id)
    {
        // Si la orden no existe se lanza un error controlado en lugar de un fallo difuso de SQL.
        var o = _orders.Get(id) ?? throw new EntityNotFoundException($"No existe la orden {id}.");
        o.Status = RepairStatuses.Entregado;
        o.DeliveredAt = DateTime.Now;
        _orders.Save(o);
    }

    private void LinkComprador(ServiceOrder order)
    {
        var doc = order.CustomerDoc.Trim();
        var cel = order.CustomerPhone.Trim();
        // Sin ninguna pista (ni celular ni documento) no hay forma de emparejar; se deja sin cliente.
        if (string.IsNullOrEmpty(cel) && string.IsNullOrEmpty(doc)) return;
        // Prioridad al celular; el documento se usa solo cuando no hay celular.
        var found = _clientes.FindClienteComprador(string.IsNullOrEmpty(cel) ? doc : cel);
        if (found != null) order.ClienteId = found.Id;
    }
}