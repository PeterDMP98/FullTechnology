// CartItemViewModel.cs — Ítem del carrito de venta: producto con cantidad ajustable (+/- dentro del stock) y total de línea en vivo.
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Venta;

/// <summary>
/// Ítem del carrito: un producto con su cantidad. El +/- respeta el stock
/// (la lógica vive en VentaViewModel vía callbacks) y algo notifica el total
/// de línea al cambiar la cantidad.
/// </summary>
public partial class CartItemViewModel : ViewModelBase
{
    private readonly CurrencyService _currency;

    public Producto Producto { get; }

    public ICommand MasCommand { get; }
    public ICommand MenosCommand { get; }
    public ICommand QuitarCommand { get; }

    [ObservableProperty]
    public partial int Cantidad { get; set; }

    // Columnas del carrito; el total de línea se recalcula con la cantidad.
    public string Nombre => Producto.Nombre;
    public string PrecioUnitario => _currency.Fmt(Producto.PrecioVenta);
    public string Total => _currency.Fmt(Producto.PrecioVenta * Cantidad);

    // Los botones delegan la lógica (stock, límites) al VM de venta.
    public CartItemViewModel(
        Producto producto,
        int cantidad,
        CurrencyService currency,
        Action<Producto, int> onCambiar,
        Action<long> onQuitar)
    {
        Producto = producto;
        Cantidad = cantidad;
        _currency = currency;
        MasCommand = new RelayCommand(() => onCambiar(producto, +1));
        MenosCommand = new RelayCommand(() => onCambiar(producto, -1));
        QuitarCommand = new RelayCommand(() => onQuitar(producto.Id));
    }

    // Al cambiar la cantidad se refresca el total de línea mostrado.
    partial void OnCantidadChanged(int value) => OnPropertyChanged(nameof(Total));
}