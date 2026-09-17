// ClientesViewModel.cs — Módulo de clientes y proveedores: listado con filtro por tipo y búsqueda en vivo, más alta/edición/eliminación vía diálogos.
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.Clientes;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// Módulo CLIENTES (F10). Replica ClienteView v2 sobre
// Application/Services: filtro por tipo (comprador/proveedor),
// campo de búsqueda y búsqueda en vivo; editar/eliminar con
// validación de uso y unicidad. Los diálogos se abren como
// ventanas modales con el owner del shell (sin ventana por módulo).
// ============================================================
public partial class ClientesViewModel : ViewModelBase, IRefreshable
{
    private readonly CustomersService _customers;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    // Recarga versionada: evita aplicar resultados obsoletos si el usuario
    // cambió de filtro mientras una carga anterior estaba en curso.
    private int _version;

    // Campos de búsqueda disponibles según el tipo de cliente.
    private static readonly string[] CompradorFields = { "Nombre", "Documento", "Celular" };
    private static readonly string[] ProveedorFields = { "Nombre", "Documento o NIT", "Celular" };

    public string Subtitle => "Almacén de clientes y proveedores.";

    public ObservableCollection<ClienteRowViewModel> Rows { get; } = new();

    public IReadOnlyList<string> TipoOptions { get; } = CustomerTypes.All;

    public IReadOnlyList<string> FilterFieldOptions { get; private set; } = CompradorFields;

    // Filtro activo: tipo de cliente, campo por el que se busca y texto.
    [ObservableProperty]
    public partial string SelectedTipo { get; set; } = CustomerTypes.Comprador;

    [ObservableProperty]
    public partial string SelectedFilterField { get; set; } = "Nombre";

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int TotalRows { get; set; }

    public ClientesViewModel(CustomersService customers, IDialogService dialogs, IServiceProvider services)
    {
        _customers = customers;
        _dialogs = dialogs;
        _services = services;
    }

    /// <summary>Recarga al volver al módulo (NavigationService llama a IRefreshable).</summary>
    public void Refresh() => Reload();

    // Al cambiar de tipo se ajusta el catálogo de campos de búsqueda (los
    // proveedores usan "Documento o NIT") y se recarga la lista.
    partial void OnSelectedTipoChanged(string value)
    {
        FilterFieldOptions = value == CustomerTypes.Proveedor ? ProveedorFields : CompradorFields;
        if (!FilterFieldOptions.Contains(SelectedFilterField))
            SelectedFilterField = "Nombre";
        OnPropertyChanged(nameof(FilterFieldOptions));
        Reload();
    }

    // Cambiar el campo de búsqueda o el texto recarga la lista.
    partial void OnSelectedFilterFieldChanged(string value) => Reload();

    partial void OnSearchTextChanged(string value) => Reload();

    private void Reload()
    {
        var version = Interlocked.Increment(ref _version);
        IsLoading = true;
        IsEmpty = false;
        IsError = false;
        _ = LoadAsync(version);
    }

    private async Task LoadAsync(int version)
    {
        try
        {
            var (tipo, campo, texto) = (SelectedTipo, SelectedFilterField, SearchText);
            // Búsqueda base por tipo fuera del hilo de UI; el filtrado fino por
            // campo se hace aquí en memoria sobre el resultado.
            var data = await Task.Run(() => _customers.Search(tipo, texto));
            if (version != _version) return; // sobrevino una recarga más nueva

            IEnumerable<Cliente> q = data;
            var term = texto.Trim();
            if (term.Length > 0)
                q = q.Where(c => Matches(c, campo, term));

            var list = q.ToList();
            Rows.Clear();
            foreach (var cl in list)
                Rows.Add(new ClienteRowViewModel(cl,
                    c => { _ = EditarAsync(c); },
                    c => { _ = EliminarAsync(c); }));
            TotalRows = list.Count;
            IsEmpty = Rows.Count == 0;
            IsLoading = false;
        }
        catch (Exception ex)
        {
            if (version != _version) return;
            Rows.Clear();
            IsLoading = false;
            IsEmpty = false;
            IsError = true;
            ErrorMessage = $"No se pudieron cargar los clientes. Detalle: {ex.Message}";
        }
    }

    // Comprueba si el cliente coincide con el término en el campo elegido
    // (las claves "Nom", "Doc" de los filtros se comparan por prefijo).
    private static bool Matches(Cliente c, string campo, string term)
    {
        if (campo.StartsWith("Nom", StringComparison.Ordinal))
            return c.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase);
        if (campo.StartsWith("Doc", StringComparison.Ordinal))
            return c.Documento.Contains(term, StringComparison.OrdinalIgnoreCase);
        return c.Celular.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Acciones ----
    // [RelayCommand] genera el comando NuevoClienteCommand para el botón "Nuevo".
    [RelayCommand]
    private async Task NuevoClienteAsync()
    {
        // Si el diálogo devuelve true (guardado) se refresca la lista.
        if (await OpenDialogAsync(null)) Reload();
    }

    private async Task EditarAsync(Cliente cliente)
    {
        if (await OpenDialogAsync(cliente)) Reload();
    }

    private async Task EliminarAsync(Cliente cliente)
    {
        if (!await _dialogs.ConfirmAsync("Confirmar", $"¿Eliminar a {cliente.Nombre}? Esta acción no se puede deshacer."))
            return;
        try
        {
            _customers.Delete(cliente.Id);
        }
        catch (DomainException ex)
        {
            // Eliminación bloqueada por reglas de negocio (p. ej. cliente con movimientos).
            await _dialogs.ShowMessageAsync("No se puede eliminar", ex.Message);
            return;
        }
        Reload();
    }

    // Abre el diálogo modal de alta/edición. El VM del diálogo se resuelve por DI,
    // la vista se crea aquí y se muestra como ventana con el owner del shell.
    private async Task<bool> OpenDialogAsync(Cliente? existing)
    {
        var vm = _services.GetRequiredService<ClienteEditDialogViewModel>();
        vm.Initialize(existing, existing is null ? SelectedTipo : existing.Tipo);
        var win = new ClienteEditDialogView { DataContext = vm };
        return await win.ShowDialog<bool>(DialogService.OwnerWindow!);
    }
}