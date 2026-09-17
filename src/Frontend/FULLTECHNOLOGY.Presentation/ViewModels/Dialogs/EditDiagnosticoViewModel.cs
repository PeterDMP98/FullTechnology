// EditDiagnosticoViewModel.cs — Diálogo para editar solo diagnóstico, trabajo realizado y observaciones de una orden.
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Editar diagnóstico (EditDiagnosticoForm v2): solo la sección de
// Diagnóstico y reparación (diagnóstico, trabajo realizado, observaciones).
// ============================================================
public partial class EditDiagnosticoViewModel : ViewModelBase
{
    private readonly OrdersService _orders;
    private readonly IDialogService _dialogs;
    private ServiceOrder _order = new();

    public string Title { get; private set; } = "";

    // Cabecera informativa y campos editables del diagnóstico.
    [ObservableProperty]
    public partial string OrdenLabel { get; set; } = "";

    [ObservableProperty]
    public partial string Diagnostico { get; set; } = "";

    [ObservableProperty]
    public partial string TrabajoRealizado { get; set; } = "";

    [ObservableProperty]
    public partial string Observaciones { get; set; } = "";

    public EditDiagnosticoViewModel(OrdersService orders, IDialogService dialogs)
    {
        _orders = orders;
        _dialogs = dialogs;
    }

    // Carga los campos desde la orden (se guarda sobre la misma instancia).
    public void Initialize(ServiceOrder order)
    {
        _order = order;
        Title = $"Editar diagnóstico – {order.OrderNumber}";
        OrdenLabel = $"{order.OrderNumber}  ·  {order.Brand} {order.Model}".Trim();
        Diagnostico = order.Diagnosis;
        TrabajoRealizado = order.RepairDetails;
        Observaciones = order.Notes;
    }

    // Copia los campos editados a la orden y la persiste.
    public async Task<bool> SaveAsync()
    {
        _order.Diagnosis = Diagnostico;
        _order.RepairDetails = TrabajoRealizado;
        _order.Notes = Observaciones;
        try
        {
            _orders.SaveOrder(_order);
            return true;
        }
        catch (DomainException ex)
        {
            await _dialogs.ShowMessageAsync("No se pudo guardar", ex.Message);
            return false;
        }
    }
}