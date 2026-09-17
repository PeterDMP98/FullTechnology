// ServiceOrder.cs — Orden de mantenimiento con totales calculados y validación del form v2.

namespace FULLTECHNOLOGY.Domain.Entities;

using FULLTECHNOLOGY.Domain.Exceptions;

/// <summary>Orden de reparación; sus campos denormalizados reproducen la tabla de mantenimientos del v2.</summary>
public class ServiceOrder
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = "";
    // Cliente opcional: se intenta autovincular en Application; aquí vive el snapshot de contacto.
    public long? ClienteId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerDoc { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string DeviceType { get; set; } = "Teléfono";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string SerialImei { get; set; } = "";
    public string Color { get; set; } = "";
    public string PhysicalCondition { get; set; } = "";
    public string Accessories { get; set; } = "";
    public string ReportedFault { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public string RepairDetails { get; set; } = "";
    // Estado = texto canónico de RepairStatuses; el flujo se mueve entre esos valores.
    public string Status { get; set; } = "Recibido";
    public decimal DiagnosisCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal Discount { get; set; }
    // Abono inicial captado; el valor "real" siempre lo reconstruye la suma de pagos.
    public decimal Deposit { get; set; }
    public string PaymentMethod { get; set; } = "Pendiente";
    public DateTime ReceivedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string Notes { get; set; } = "";

    // Total = costos − descuento; un descuento mayor que los costos se trunca a cero (no hay saldo a favor).
    public decimal Total => Math.Max(0, DiagnosisCost + PartsCost + LaborCost - Discount);
    // Lo pendiente de cobrar; si el abono supera el total no se devuelve sobrante.
    public decimal Balance => Math.Max(0, Total - Deposit);

    /// <summary>Valida las reglas de negocio de una orden (comportamiento v2 del form).</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CustomerName))
            throw new ValidationException("El nombre del cliente es obligatorio.");
        // Contacto mínimo exigido: sin celular/WhatsApp ni documento no se puede localizar al dueño del equipo.
        if (string.IsNullOrWhiteSpace(CustomerPhone) && string.IsNullOrWhiteSpace(CustomerDoc))
            throw new ValidationException("Debe indicar el número de celular/WhatsApp o el documento de identidad del cliente.");
        // Datos mínimos del equipo para arrancar la reparación; sin ellos la orden no es accionable.
        if (string.IsNullOrWhiteSpace(DeviceType) || string.IsNullOrWhiteSpace(Brand))
            throw new ValidationException("El tipo de equipo y la marca son obligatorios.");
    }
}