// VentaViewModel.cs — Punto de venta de accesorios: catálogo con búsqueda, carrito con +/-, descuento, cliente, pago y confirmación de compra.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Infrastructure.Reporting;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;
using FULLTECHNOLOGY.Presentation.ViewModels.Venta;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
// Alias con nombre propio: el namespace ...ViewModels.Venta gana la búsqueda del nombre 'Venta'.
using VentaFactura = FULLTECHNOLOGY.Domain.Entities.Venta;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// ============================================================
// VENTA (F10): punto de venta de accesorios con carrito. Replica
// VentaView v2 sobre SalesService.CreateSale: catálogo de
// accesorios con búsqueda en vivo, carrito lateral con +/-/quitar,
// descuento opcional, medio de pago y confirmación de compra.
// El stock se respeta siempre (máximo = p.Stock - en carro, y el
// servicio re-valida con EnsureStock antes de persistir).
// ============================================================
public partial class VentaViewModel : ViewModelBase, IRefreshable
{
    private readonly InventoryService _inventory;
    private readonly SalesService _sales;
    private readonly CurrencyService _currency;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    // Recarga versionada del catálogo de accesorios.
    private int _version;

    public string Subtitle => "Punto de venta de accesorios con carrito.";

    // Se dispara al grabarse una venta (venta + líneas ya congeladas); la vista
    // u otros suscriptores pueden reaccionar (p. ej. recargar cierres de caja).
    public event Action<VentaFactura, IReadOnlyList<InvoiceReport.Linea>>? VentaCompletada;

    public ObservableCollection<ProductoVentaRowViewModel> Rows { get; } = new();
    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();

    // Búsqueda en vivo del catálogo (solo accesorios).
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int ProductsCount { get; set; }

    // Descuento opcional sobre el subtotal (se valida contra el saldo en venta).
    [ObservableProperty]
    public partial bool DescuentoActivo { get; set; }

    [ObservableProperty]
    public partial decimal DescuentoValue { get; set; }

    // Cliente opcional de la venta (se elige en un diálogo de búsqueda).
    [ObservableProperty]
    public partial string? ClienteNombre { get; set; }

    public long? ClienteId { get; private set; }

    // Textos derivados del carrito (se recalculan en RecomposeTotals).
    public string SubtotalText => _currency.Fmt(SubtotalActual);
    public string DescuentoText => _currency.Fmt(DescuentoActual);
    public string TotalText => _currency.Fmt(TotalActual);
    public string ItemsText => $"Ítems: {CartItems.Count}";

    public bool HasCartItems => CartItems.Count > 0;

    // Cálculos puros del total (el total nunca baja de 0 aunque el descuento
    // exceda el subtotal).
    private decimal SubtotalActual => CartItems.Sum(i => i.Producto.PrecioVenta * i.Cantidad);
    private decimal DescuentoActual => DescuentoActivo ? DescuentoValue : 0m;
    private decimal TotalActual => Math.Max(0m, SubtotalActual - DescuentoActual);

    public VentaViewModel(
        InventoryService inventory,
        SalesService sales,
        CurrencyService currency,
        IDialogService dialogs,
        IServiceProvider services)
    {
        _inventory = inventory;
        _sales = sales;
        _currency = currency;
        _dialogs = dialogs;
        _services = services;
        // Cada cambio del carrito (añadir/±/quitar) recalcula el resumen.
        CartItems.CollectionChanged += (s, e) => RecomposeTotals();
    }

    public void Refresh() => Reload();

    partial void OnSearchTextChanged(string value) => Reload();

    // Desactivar el descuento también lo pone a 0 y recalcula.
    partial void OnDescuentoActivoChanged(bool value)
    {
        if (!value) DescuentoValue = 0m;
        RecomposeTotals();
    }

    partial void OnDescuentoValueChanged(decimal value) => RecomposeTotals();

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
            var texto = SearchText;
            var data = await Task.Run(() => _inventory.Search(ProductTypes.Accesorio, texto));
            if (version != _version) return; // sobrevino una recarga más nueva

            // Cada fila del catálogo llama AgregarAsync al hacer click.
            Rows.Clear();
            foreach (var p in data)
                Rows.Add(new ProductoVentaRowViewModel(p, _currency, prod => { _ = AgregarAsync(prod); }));
            ProductsCount = Rows.Count;
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
            ErrorMessage = $"No se pudo cargar el catálogo de accesorios. Detalle: {ex.Message}";
        }
    }

    // Renotifica todos los totales al cambiar cualquier dato del carrito.
    private void RecomposeTotals()
    {
        OnPropertyChanged(nameof(SubtotalText));
        OnPropertyChanged(nameof(DescuentoText));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(ItemsText));
        OnPropertyChanged(nameof(HasCartItems));
    }

    // ---- Carrito ----
    // Pide la cantidad en un diálogo y añade el producto respetando el stock.
    private async Task AgregarAsync(Producto p)
    {
        if (p.Stock <= 0)
        {
            await _dialogs.ShowMessageAsync("Stock", "Este producto no tiene stock disponible.");
            return;
        }

        var vm = _services.GetRequiredService<CantidadDialogViewModel>();
        vm.Initialize(p, CartItems.FirstOrDefault(i => i.Producto.Id == p.Id)?.Cantidad ?? 0);
        var win = new CantidadDialogView { DataContext = vm };
        if (!await win.ShowDialog<bool>(DialogService.OwnerWindow!)) return;

        TryAgregar(p, (int)vm.Cantidad);
    }

    /// <summary>
    /// Añade la cantidad indicada al carrito respetando el stock restante
    /// (máximo = stock del producto − lo ya agregado). Devuelve false si no
    /// cabe y avisa por diálogo, como el WinForms.
    /// </summary>
    public bool TryAgregar(Producto p, int cantidad)
    {
        var yaEnCarro = CartItems.FirstOrDefault(i => i.Producto.Id == p.Id)?.Cantidad ?? 0;
        var max = p.Stock - yaEnCarro;

        if (p.Stock <= 0)
        {
            _ = _dialogs.ShowMessageAsync("Stock", "Este producto no tiene stock disponible.");
            return false;
        }
        if (cantidad < 1 || cantidad > max)
        {
            _ = _dialogs.ShowMessageAsync("Stock", "Cantidad máxima del stock alcanzada.");
            return false;
        }

        if (yaEnCarro > 0)
        {
            // Si ya estaba en el carrito se suma a la línea existente.
            CartItems.First(i => i.Producto.Id == p.Id).Cantidad += cantidad;
        }
        else
        {
            // Primera vez: se crea la línea con sus callbacks (+/-, quitar).
            CartItems.Add(new CartItemViewModel(p, cantidad, _currency, ChangeCartQuantity, RemoveCartItem));
        }
        RecomposeTotals();
        return true;
    }

    /// <summary>+/+ por ítem: respeta el stock y no baja de 1.</summary>
    public void ChangeCartQuantity(Producto p, int delta)
    {
        var item = CartItems.FirstOrDefault(i => i.Producto.Id == p.Id);
        if (item is null) return;
        var nueva = item.Cantidad + delta;
        if (nueva > p.Stock)
        {
            _ = _dialogs.ShowMessageAsync("Stock", "Cantidad máxima del stock alcanzada.");
            return;
        }
        if (nueva < 1) return;
        item.Cantidad = nueva;
    }

    // Quita del carrito la línea del producto indicado (para botón ✕/Quitar).
    public void RemoveCartItem(long id)
    {
        var item = CartItems.FirstOrDefault(i => i.Producto.Id == id);
        if (item is not null) CartItems.Remove(item);
    }

    // ---- Acciones de la venta ----
    // Flujo de compra: pedir pago (diálogo), crear la venta y mostrar la factura.
    [RelayCommand]
    private async Task ComprarAsync()
    {
        if (CartItems.Count == 0)
        {
            await _dialogs.ShowMessageAsync("Carrito", "El carrito está vacío.");
            return;
        }

        // El diálogo de pago captura el medio y valida la confirmación.
        var pagoVm = _services.GetRequiredService<PagoVentaDialogViewModel>();
        pagoVm.Initialize(TotalActual);
        var pagoWin = new PagoVentaDialogView { DataContext = pagoVm };
        if (!await pagoWin.ShowDialog<bool>(DialogService.OwnerWindow!)) return;

        try
        {
            var items = CartItems.Select(i => new SaleItem(i.Producto.Id, i.Cantidad)).ToList();
            // Las líneas se congelan del carrito (precio exportado del producto) antes de vaciarlo.
            var lineas = CartItems.Select(i => new InvoiceReport.Linea(i.Producto.Nombre, i.Cantidad, i.Producto.PrecioVenta)).ToList();
            var venta = _sales.CreateSale(items, DescuentoActual, pagoVm.SelectedMetodo, ClienteId, ClienteNombre);
            await LimpiarCarrito();
            Reload();
            await MostrarFacturaAsync(venta, lineas);
        }
        catch (Exception ex)
        {
            // Fallo de regla de negocio o de BD: se informa y el carrito queda intacto.
            await _dialogs.ShowMessageAsync("No se pudo registrar la venta", ex.Message);
        }
    }

    /// <summary>
    /// Al concluir la compra: notifica a los suscriptores, genera la factura en PDF
    /// (Documentos\Facturas) y muestra la previsualización con opción de imprimirla.
    /// En modo headless (pruebas) solo se notifica y se deja el PDF para el entorno real.
    /// </summary>
    private async Task MostrarFacturaAsync(VentaFactura venta, IReadOnlyList<InvoiceReport.Linea> lineas)
    {
        VentaCompletada?.Invoke(venta, lineas);
        if (DialogService.OwnerWindow is null) return;

        try
        {
            var pdf = InvoiceReport.SaveInvoicePdf(venta.VentaNumber, venta.ClienteNombre, venta.Fecha,
                venta.MetodoPago, lineas, venta.Subtotal, venta.Descuento, venta.Total);
            var preview = new InvoicePreviewViewModel(venta, lineas, _currency, pdf);
            var win = new InvoicePreviewDialogView { DataContext = preview };
            await win.ShowDialog<bool>(DialogService.OwnerWindow!);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("Factura",
                $"La compra {venta.VentaNumber} se registró, pero no se pudo generar la factura.\nDetalle: {ex.Message}");
        }
    }

    // Cancela la venta actual y vacía el carrito.
    [RelayCommand]
    private async Task CancelarVentaAsync()
    {
        await LimpiarCarrito();
        await _dialogs.ShowMessageAsync("Venta cancelada", "La venta fue cancelada y el carrito se ha vaciado.");
    }

    // Busca un cliente comprador en un diálogo y lo asocia a la venta.
    [RelayCommand]
    private async Task SeleccionarClienteAsync()
    {
        var busq = _services.GetRequiredService<BuscarClienteViewModel>();
        busq.Initialize(CustomerTypes.Comprador);
        var win = new BuscarClienteView { DataContext = busq };
        if (await win.ShowDialog<bool>(DialogService.OwnerWindow!) && busq.Selected is not null)
        {
            ClienteId = busq.Selected.Id;
            ClienteNombre = busq.Selected.Nombre;
        }
    }

    // Vacía el carrito y resetea el descuento tras una venta o cancelación.
    private Task LimpiarCarrito()
    {
        CartItems.Clear();
        DescuentoActivo = false;
        DescuentoValue = 0m;
        return Task.CompletedTask;
    }
}