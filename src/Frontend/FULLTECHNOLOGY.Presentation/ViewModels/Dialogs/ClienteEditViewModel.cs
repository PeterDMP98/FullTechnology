// ClienteEditViewModel.cs — Alta de cliente comprador dentro del pedido de mantenimiento (nombre, documento, celular) con guardado validado.
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Crear cliente comprador (ClienteEditForm v2, campos del pedido de
// mantenimiento): nombre, documento y celular. El guardado pasa por
// CustomersService (con su validación y unicidad por diseño).
// ============================================================
public partial class ClienteEditViewModel : ViewModelBase
{
    private readonly CustomersService _customers;
    private readonly IDialogService _dialogs;

    public string Title { get; } = "Crear cliente comprador";

    // Campos del formulario (comprador: nombre, documento y celular).
    [ObservableProperty]
    public partial string Nombre { get; set; } = "";

    [ObservableProperty]
    public partial string Documento { get; set; } = "";

    [ObservableProperty]
    public partial string Celular { get; set; } = "";

    /// <summary>Cliente persistido tras guardar con éxito.</summary>
    public Cliente? Saved { get; private set; }

    public ClienteEditViewModel(CustomersService customers, IDialogService dialogs)
    {
        _customers = customers;
        _dialogs = dialogs;
    }

    // Guarda (siempre tipo comprador); devuelve true solo si la vista cierra con OK.
    public async Task<bool> SaveAsync()
    {
        var cliente = new Cliente
        {
            Tipo = "Cliente comprador",
            Nombre = Nombre.Trim(),
            Documento = Documento.Trim(),
            Celular = Celular.Trim()
        };
        try
        {
            _customers.Save(cliente);
            Saved = cliente;
            return true;
        }
        catch (ValidationException ex)
        {
            await _dialogs.ShowMessageAsync("No se pudo guardar", ex.Message);
            return false;
        }
        catch (DomainException ex)
        {
            await _dialogs.ShowMessageAsync("No se pudo guardar", ex.Message);
            return false;
        }
    }
}