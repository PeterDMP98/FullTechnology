// Pago.cs — Abonos y cobros registrados sobre una orden de mantenimiento.

namespace FULLTECHNOLOGY.Domain.Entities;

/// <summary>Movimiento de dinero de una orden; cada registro arrastra el saldo restante del momento.</summary>
public class Pago
{
    public long Id { get; set; }
    // La orden que se liquida; sin FK navegable a propósito: la relación vive en el repositorio.
    public long ServiceOrderId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = "";
    public string Concepto { get; set; } = "";
    // Saldo de la orden tras este pago (snapshot); al borrar pagos lo reconstruye RecalcDeposit.
    public decimal SaldoRestante { get; set; }

    // "Salida" es el código interno de las salidas de caja; aquí solo se humaniza la etiqueta.
    public string MetodoLabel => Metodo == "Salida" ? "Salida de efectivo" : Metodo;
}