// OrderHistoryViewModel.cs — Ventana informativa de una orden: datos completos, estado financiero e historial de pagos (solo lectura).
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Ver orden: historial de pagos y datos completos (ventana
// informativa, OrderHistoryForm v2). No edita directamente.
// ============================================================
public partial class OrderHistoryViewModel : ViewModelBase
{
    private readonly PaymentsService _payments;
    private readonly CurrencyService _currency;
    private ServiceOrder _order = new();

    public string Title { get; private set; } = "";

    // Datos del cliente.
    [ObservableProperty]
    public partial string Cliente { get; set; } = "";

    [ObservableProperty]
    public partial string Documento { get; set; } = "";

    [ObservableProperty]
    public partial string Celular { get; set; } = "";

    // Datos del equipo.
    [ObservableProperty]
    public partial string Equipo { get; set; } = "";

    [ObservableProperty]
    public partial string SerialImei { get; set; } = "";

    [ObservableProperty]
    public partial string EstadoText { get; set; } = "";

    [ObservableProperty]
    public partial string Ingreso { get; set; } = "";

    // Diagnóstico y reparación.
    [ObservableProperty]
    public partial string Diagnostico { get; set; } = "";

    [ObservableProperty]
    public partial string TrabajoRealizado { get; set; } = "";

    [ObservableProperty]
    public partial string Observaciones { get; set; } = "";

    // Estado financiero.
    [ObservableProperty]
    public partial string TotalText { get; set; } = "";

    [ObservableProperty]
    public partial string PagadoText { get; set; } = "";

    [ObservableProperty]
    public partial string SaldoText { get; set; } = "";

    public ObservableCollection<PagoRow> Historial { get; } = new();

    public bool HasPagos { get; private set; }

    public OrderHistoryViewModel(PaymentsService payments, CurrencyService currency)
    {
        _payments = payments;
        _currency = currency;
    }

    // Rellena toda la ventana y el historial de pagos de la orden.
    public void Initialize(ServiceOrder order)
    {
        _order = order;
        Title = $"Ver orden {order.OrderNumber}";

        Cliente = order.CustomerName;
        Documento = order.CustomerDoc;
        Celular = order.CustomerPhone;
        Equipo = $"{order.DeviceType} {order.Brand} {order.Model}".Trim();
        SerialImei = order.SerialImei;
        EstadoText = order.Status;
        Ingreso = order.ReceivedAt.ToString("dd/MM/yyyy HH:mm");
        Diagnostico = order.Diagnosis;
        TrabajoRealizado = order.RepairDetails;
        Observaciones = order.Notes;
        TotalText = _currency.Fmt(order.Total);
        PagadoText = _currency.Fmt(order.Deposit);
        SaldoText = _currency.Fmt(order.Balance);

        Historial.Clear();
        foreach (var p in _payments.GetOrderPayments(order.Id))
            Historial.Add(new PagoRow(
                p.Fecha.ToString("dd/MM/yyyy HH:mm"),
                _currency.Fmt(p.Monto),
                p.Metodo,
                p.Concepto,
                _currency.Fmt(p.SaldoRestante)));
        HasPagos = Historial.Count > 0;
    }
}