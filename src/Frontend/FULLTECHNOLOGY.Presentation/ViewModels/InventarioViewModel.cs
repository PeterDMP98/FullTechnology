// InventarioViewModel.cs — Módulo de inventario: listado de productos con filtro por tipo y búsqueda en vivo, más alta/edición/eliminación vía diálogo.
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using FULLTECHNOLOGY.Presentation.ViewModels.Inventario;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// Módulo INVENTARIO (F10). Replica InventarioView v2 sobre
// Application/Services: filtro por tipo (Todos/Repuesto/Accesorio)
// y búsqueda en vivo; editar/eliminar productos.
// ============================================================
public partial class InventarioViewModel : ViewModelBase, IRefreshable
{
    // Valor "Todos" = sin filtro de tipo (se envía null al servicio).
    private const string Todos = "Todos";

    private readonly InventoryService _inventory;
    private readonly CurrencyService _currency;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    // Recarga versionada: evita aplicar resultados obsoletos.
    private int _version;

    public string Subtitle => "Productos, stock y costos.";

    public ObservableCollection<ProductoRowViewModel> Rows { get; } = new();

    public IReadOnlyList<string> TipoOptions { get; } = new[] { Todos, ProductTypes.Repuesto, ProductTypes.Accesorio };

    // Filtros activos: tipo de producto y texto de búsqueda.
    [ObservableProperty]
    public partial string SelectedTipo { get; set; } = Todos;

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int TotalRows { get; set; }

    // Datos de exportación a PDF/Excel (encabezados, anchos y filas ya formateadas).
    [ObservableProperty]
    public partial string[] ExportHeaders { get; set; } = Array.Empty<string>();

    [ObservableProperty]
    public partial float[] ExportWidths { get; set; } = Array.Empty<float>();

    [ObservableProperty]
    public partial IReadOnlyList<string[]> ExportRows { get; set; } = Array.Empty<string[]>();

    [ObservableProperty]
    public partial string ExportTitle { get; set; } = "INVENTARIO";

    [ObservableProperty]
    public partial string ExportSublinea { get; set; } = "";

    [ObservableProperty]
    public partial string FileNameBase { get; set; } = "Inventario";

    public InventarioViewModel(
        InventoryService inventory,
        CurrencyService currency,
        IDialogService dialogs,
        IServiceProvider services)
    {
        _inventory = inventory;
        _currency = currency;
        _dialogs = dialogs;
        _services = services;
    }

    public void Refresh() => Reload();

    // Cambiar el tipo o el texto recarga la lista al instante.
    partial void OnSelectedTipoChanged(string value) => Reload();

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
            var (tipo, texto) = (SelectedTipo, SearchText);
            // "Todos" se traduce a null para que el servicio no filtre por tipo.
            var data = await Task.Run(() => _inventory.Search(tipo is Todos or "" ? null : tipo, texto));
            if (version != _version) return; // sobrevino una recarga más nueva

            // Cada fila recibe callbacks de editar/eliminar que delegan aquí.
            Rows.Clear();
            foreach (var p in data)
                Rows.Add(new ProductoRowViewModel(p, _currency,
                    prod => { _ = EditarAsync(prod); },
                    prod => { _ = EliminarAsync(prod); }));
            TotalRows = Rows.Count;
            IsEmpty = Rows.Count == 0;

            // Catálogo listo para exportar: números en formato invariante para que
            // PDF/Excel los escriban como valores numéricos y se puedan reimportar.
            ExportHeaders = new[] { "Código", "Nombre", "Tipo", "Costo", "Precio venta", "Proveedor", "Ingreso", "Garantía", "Ubicado", "Stock" };
            ExportWidths = new[] { 90f, 150f, 80f, 80f, 90f, 120f, 80f, 70f, 80f, 60f };
            ExportRows = data.Select(p => new[]
            {
                p.Codigo, p.Nombre, p.Tipo,
                Num(p.Costo), Num(p.PrecioVenta),
                p.Proveedor ?? "", p.FechaIngreso.ToString("yyyy-MM-dd"),
                p.Garantia ?? "", p.Ubicado ?? "",
                p.Stock.ToString(CultureInfo.InvariantCulture)
            }).ToList();
            ExportSublinea = $"{DateTime.Today:dd/MM/yyyy} · {data.Count} productos";
            IsLoading = false;
        }
        catch (Exception ex)
        {
            if (version != _version) return;
            Rows.Clear();
            IsLoading = false;
            IsEmpty = false;
            IsError = true;
            ErrorMessage = $"No se pudieron cargar los productos. Detalle: {ex.Message}";
        }
    }

    // ---- Acciones ----
    // [RelayCommand] genera NuevoProductoCommand para el botón "Nuevo".
    [RelayCommand]
    private async Task NuevoProductoAsync()
    {
        if (await OpenDialogAsync(null)) Reload();
    }

    private async Task EditarAsync(Producto producto)
    {
        if (await OpenDialogAsync(producto)) Reload();
    }

    private async Task EliminarAsync(Producto producto)
    {
        if (!await _dialogs.ConfirmAsync("Confirmar", $"¿Eliminar {producto.Nombre}? Esta acción no se puede deshacer."))
            return;
        _inventory.Delete(producto.Id);
        Reload();
    }

    // Abre el diálogo modal de alta/edición de producto (VM por DI, vista local).
    private async Task<bool> OpenDialogAsync(Producto? existing)
    {
        var vm = _services.GetRequiredService<ProductoEditDialogViewModel>();
        vm.Initialize(existing);
        var win = new ProductoEditDialogView { DataContext = vm };
        return await win.ShowDialog<bool>(DialogService.OwnerWindow!);
    }

    // ---- Importación (los archivos los lee la vista; el VM aplica) ----
    // Carga o actualiza productos desde el archivo importado: si el código ya
    // existe se actualiza el producto; si no, se da de alta. Recarga la lista.
    public (int nuevos, int actualizados) AplicarImportacion(IReadOnlyList<Producto> import)
    {
        int nuevos = 0, actualizados = 0;
        foreach (var p in import.Where(x => !string.IsNullOrWhiteSpace(x.Nombre)))
        {
            // Sanea lo que venga del archivo para no fallar la validación del dominio.
            p.Tipo = p.Tipo == ProductTypes.Repuesto ? ProductTypes.Repuesto : ProductTypes.Accesorio;
            p.Costo = Math.Max(0, p.Costo);
            p.PrecioVenta = Math.Max(0, p.PrecioVenta);
            p.Stock = Math.Max(0, p.Stock);

            var existente = string.IsNullOrWhiteSpace(p.Codigo)
                ? null
                : _inventory.Search(null, p.Codigo).FirstOrDefault(x => x.Codigo == p.Codigo);
            if (existente is null)
            {
                _inventory.Save(p);
                nuevos++;
            }
            else
            {
                existente.Nombre = p.Nombre;
                existente.Tipo = p.Tipo;
                existente.Costo = p.Costo;
                existente.PrecioVenta = p.PrecioVenta;
                existente.Stock = p.Stock;
                _inventory.Save(existente);
                actualizados++;
            }
        }
        Reload();
        return (nuevos, actualizados);
    }

    // Números en formato invariante para el export (PDF/Excel y reimportación).
    private static string Num(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}