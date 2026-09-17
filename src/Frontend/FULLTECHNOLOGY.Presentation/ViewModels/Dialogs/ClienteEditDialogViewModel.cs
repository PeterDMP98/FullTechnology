// ClienteEditDialogViewModel.cs — Alta/edición de cliente o proveedor: valida datos obligatorios y unicidad, guarda vía servicio y expone SaveAsync.
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Crear/editar cliente o proveedor (ClienteEditForm v2 completo):
// tipo, nombre, documento/NIT, celular y (solo proveedor) dirección,
// web y red social. Replica la validación del formulario WinForms:
// nombre obligatorio, doc-o-cel para comprador, celular obligatorio
// para proveedor y unicidad de documento/celular (excluyendo el mismo).
// ============================================================
public partial class ClienteEditDialogViewModel : ViewModelBase
{
    private readonly CustomersService _customers;
    private readonly IDialogService _dialogs;
    private long _id;

    public string Title { get; private set; } = "Nuevo cliente";

    // Tipos posibles (comprador/proveedor) tal y como los define el dominio.
    public IReadOnlyList<string> TipoOptions { get; } = CustomerTypes.All;

    // Campos del formulario: tipo, nombre, documento/NIT y celular.
    [ObservableProperty]
    public partial string SelectedTipo { get; set; } = CustomerTypes.Comprador;

    [ObservableProperty]
    public partial string Nombre { get; set; } = "";

    [ObservableProperty]
    public partial string Documento { get; set; } = "";

    [ObservableProperty]
    public partial string Celular { get; set; } = "";

    // Campos adicionales solo para proveedor.
    [ObservableProperty]
    public partial string Direccion { get; set; } = "";

    [ObservableProperty]
    public partial string Web { get; set; } = "";

    [ObservableProperty]
    public partial string RedSocial { get; set; } = "";

    /// <summary>Campos de proveedor: solo visibles cuando el tipo es Proveedor.</summary>
    [ObservableProperty]
    public partial bool EsProveedor { get; set; }

    /// <summary>Cliente persistido tras guardar con éxito.</summary>
    public Cliente? Saved { get; private set; }

    public ClienteEditDialogViewModel(CustomersService customers, IDialogService dialogs)
    {
        _customers = customers;
        _dialogs = dialogs;
    }

    // Al cambiar el tipo se muestran/ocultan los campos de proveedor.
    partial void OnSelectedTipoChanged(string value) => EsProveedor = value == CustomerTypes.Proveedor;

    // Carga del formulario: datos del cliente existente (edición) o vacíos (alta).
    public void Initialize(Cliente? existing, string defaultTipo)
    {
        _id = existing?.Id ?? 0;
        SelectedTipo = existing?.Tipo ?? defaultTipo;
        Nombre = existing?.Nombre ?? "";
        Documento = existing?.Documento ?? "";
        Celular = existing?.Celular ?? "";
        Direccion = existing?.Direccion ?? "";
        Web = existing?.Web ?? "";
        RedSocial = existing?.RedSocial ?? "";
        EsProveedor = SelectedTipo == CustomerTypes.Proveedor;
        Title = existing is null ? "Nuevo cliente" : $"Editar: {Nombre}";
    }

    // Valida y guarda; devuelve true solo si la vista debe cerrarse con OK.
    public async Task<bool> SaveAsync()
    {
        var comprador = SelectedTipo == CustomerTypes.Comprador;
        var nombre = Nombre.Trim();
        var doc = Documento.Trim();
        var cel = Celular.Trim();

        // Validaciones replicadas del formulario WinForms (F7).
        if (nombre.Length == 0)
            return await Fail("El nombre es obligatorio.");
        if (comprador && doc.Length == 0 && cel.Length == 0)
            return await Fail("Para un cliente comprador debe llenar el documento de identidad o el número de celular.");
        if (!comprador && cel.Length == 0)
            return await Fail("Para un proveedor el número de celular es obligatorio.");

        // Unicidad de documento/celular en toda la base (excluyendo el propio id).
        var dup = FindDuplicate(doc, cel);
        if (dup is not null && dup.Id != _id)
        {
            var campo = dup.Celular is { Length: > 0 } && dup.Celular == cel ? "número de celular" : "documento";
            return await Fail($"Ya existe un cliente con ese {campo}: {dup.Nombre}.");
        }

        try
        {
            var cliente = new Cliente
            {
                Id = _id,
                Tipo = SelectedTipo,
                Nombre = nombre,
                Documento = doc,
                Celular = cel,
                Direccion = Direccion.Trim(),
                Web = Web.Trim(),
                RedSocial = RedSocial.Trim()
            };
            _customers.Save(cliente);
            Saved = cliente;
            return true;
        }
        catch (ValidationException ex)
        {
            return await Fail(ex.Message);
        }
        catch (DomainException ex)
        {
            return await Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return await Fail($"No se pudo guardar el cliente: {ex.Message}");
        }
    }

    // Busca en todos los clientes alguno con el mismo celular o documento.
    private Cliente? FindDuplicate(string doc, string cel)
    {
        foreach (var cl in _customers.Search())
        {
            if (!string.IsNullOrEmpty(cel) && !string.IsNullOrEmpty(cl.Celular) && cl.Celular == cel) return cl;
            if (!string.IsNullOrEmpty(doc) && !string.IsNullOrEmpty(cl.Documento) && cl.Documento == doc) return cl;
        }
        return null;
    }

    // Muestra el error y devuelve false para que la vista no cierre.
    private async Task<bool> Fail(string message)
    {
        await _dialogs.ShowMessageAsync("No se pudo guardar", message);
        return false;
    }
}