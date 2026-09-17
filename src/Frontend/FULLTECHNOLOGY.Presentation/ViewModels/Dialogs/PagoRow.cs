// PagoRow.cs — Modelos de fila reutilizados: una fila de historial de pagos y una fila de resultados de búsqueda de clientes.
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

/// <summary>
/// Fila del historial de pagos (fechas y montos ya formateados).
/// Record inmutable: se usa tanto en el diálogo de cobro como en el historial de la orden.
/// </summary>
public sealed record PagoRow(string Fecha, string Monto, string Metodo, string Concepto, string Saldo);

/// <summary>
/// Fila de resultados de búsqueda de clientes. Lleva la entidad <see cref="Cliente"/>
/// completa para que el llamante pueda leer Id/Nombre tras seleccionar una fila.
/// </summary>
public sealed record ClienteRow(string Nombre, string Documento, string Celular) { public Cliente Cliente { get; init; } = null!; }