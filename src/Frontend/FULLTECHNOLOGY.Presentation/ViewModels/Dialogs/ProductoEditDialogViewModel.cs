// ProductoEditDialogViewModel.cs — Alta/edición de producto de inventario: datos del producto, proveedor (buscar/crear) y validación al guardar.
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Crear/editar producto del inventario (ProductoEditForm v2):
// nombre, tipo (repuesto/accesorio), costo, precio, proveedor
// (buscar/crear desde el diálogo), fecha de ingreso, garantía,
// ubicación y stock. Guardado vía InventoryService.Validate().
// ============================================================
public partial class ProductoEditDialogViewModel : ViewModelBase
{
    private readonly InventoryService _inventory;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;
    private long _id;
    private DateTime _fechaIngreso = DateTime.Today;

    /// <summary>
    /// Código del producto. No es editable en el diálogo (se autogenera en la
    /// BD: PRD-año-NNNNN); al editar se preserva el existente.
    /// </summary>
    private string _codigo = "";

    public string Title { get; private set; } = "Nuevo producto";

    // Tipos posibles de producto (repuesto/accesorio).
    public IReadOnlyList<string> TipoOptions { get; } = ProductTypes.All;

    // Campos del formulario.
    [ObservableProperty]
    public partial string SelectedTipo { get; set; } = ProductTypes.Accesorio;

    [ObservableProperty]
    public partial string Nombre { get; set; } = "";

    [ObservableProperty]
    public partial decimal? Costo { get; set; } = 0m;

    [ObservableProperty]
    public partial decimal? PrecioVenta { get; set; } = 0m;

    [ObservableProperty]
    public partial string Proveedor { get; set; } = "";

    [ObservableProperty]
    public partial string FechaIngresoLabel { get; set; } = "";

    [ObservableProperty]
    public partial string Garantia { get; set; } = "";

    [ObservableProperty]
    public partial string Ubicado { get; set; } = "";

    [ObservableProperty]
    public partial decimal? Stock { get; set; } = 0m;

    /// <summary>Producto persistido tras guardar con éxito.</summary>
    public Producto? Saved { get; private set; }

    public ProductoEditDialogViewModel(
        InventoryService inventory,
        IDialogService dialogs,
        IServiceProvider services)
    {
        _inventory = inventory;
        _dialogs = dialogs;
        _services = services;
    }

    // Carga los campos desde el producto existente (edición) o vacíos (alta).
    public void Initialize(Producto? existing)
    {
        _id = existing?.Id ?? 0;
        _codigo = existing?.Codigo ?? "";
        SelectedTipo = existing?.Tipo ?? ProductTypes.Accesorio;
        Nombre = existing?.Nombre ?? "";
        Costo = existing?.Costo ?? 0m;
        PrecioVenta = existing?.PrecioVenta ?? 0m;
        Proveedor = existing?.Proveedor ?? "";
        _fechaIngreso = existing?.FechaIngreso ?? DateTime.Today;
        FechaIngresoLabel = _fechaIngreso.ToString("dd/MM/yyyy");
        Garantia = existing?.Garantia ?? "";
        Ubicado = existing?.Ubicado ?? "";
        Stock = existing?.Stock ?? 0m;
        Title = existing is null ? "Nuevo producto" : $"Editar: {existing.Nombre}";
    }

    // Valida el nombre y guarda a través del servicio (que valida costos, etc.).
    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            return await Fail("El nombre del producto es obligatorio.");

        try
        {
            var producto = new Producto
            {
                Id = _id,
                Codigo = _codigo,
                Nombre = Nombre.Trim(),
                Tipo = SelectedTipo,
                Costo = Costo ?? 0m,
                PrecioVenta = PrecioVenta ?? 0m,
                Proveedor = BlankOrNull(Proveedor),
                FechaIngreso = _fechaIngreso,
                Garantia = BlankOrNull(Garantia),
                Ubicado = BlankOrNull(Ubicado),
                Stock = (int)(Stock ?? 0m)
            };
            _inventory.Save(producto);
            Saved = producto;
            return true;
        }
        catch (ValidationException ex)
        {
            return await Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return await Fail($"No se pudo guardar el producto: {ex.Message}");
        }
    }

    // Las cadenas en blanco se guardan como null (campo vacío en BD).
    private static string? BlankOrNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<bool> Fail(string message)
    {
        await _dialogs.ShowMessageAsync("No se pudo guardar", message);
        return false;
    }

    // ---- Proveedor desde el diálogo (buscar o crear, con este como owner) ----
    // Busca un proveedor existente y lo escribe en el campo.
    public async Task BuscarProveedorAsync(Window owner)
    {
        var busq = _services.GetRequiredService<BuscarClienteViewModel>();
        busq.Initialize(CustomerTypes.Proveedor);
        var win = new BuscarClienteView { DataContext = busq };
        if (await win.ShowDialog<bool>(owner) && busq.Selected is not null)
            Proveedor = busq.Selected.Nombre;
    }

    // Crea un proveedor nuevo y lo asigna al producto.
    public async Task CrearProveedorAsync(Window owner)
    {
        var crear = _services.GetRequiredService<ClienteEditDialogViewModel>();
        crear.Initialize(null, CustomerTypes.Proveedor);
        var win = new ClienteEditDialogView { DataContext = crear };
        if (await win.ShowDialog<bool>(owner) && crear.Saved is not null)
            Proveedor = crear.Saved.Nombre;
    }
}