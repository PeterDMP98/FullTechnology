// ProductoVentaRowViewModel.cs — Fila del listado de productos para la venta: columnas formateadas y comando Agregar que lo lleva al carrito.
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Venta;

/// <summary>
/// Fila del listado de productos de la venta (solo tipo Accesorio, como el
/// WinForms). Cada fila ofrece el comando "Agregar" que abre el diálogo de
/// cantidad y mete el producto en el carrito.
/// </summary>
public class ProductoVentaRowViewModel : ViewModelBase
{
    private readonly CurrencyService _currency;

    public Producto Producto { get; }
    public ICommand AgregarCommand { get; }

    // Columnas expuestas planas con los montos ya formateados.
    public string Codigo => Producto.Codigo;
    public string Nombre => Producto.Nombre;
    public string Ubicado => Producto.Ubicado ?? "";
    public string Costo => _currency.Fmt(Producto.Costo);
    public string PrecioVenta => _currency.Fmt(Producto.PrecioVenta);
    public string Stock => Producto.Stock.ToString();
    public string Garantia => Producto.Garantia ?? "";

    // Delegada: el VM de venta abre la elección de cantidad y agrega al carrito.
    public ProductoVentaRowViewModel(Producto producto, CurrencyService currency, Action<Producto> onAgregar)
    {
        Producto = producto;
        _currency = currency;
        AgregarCommand = new RelayCommand(() => onAgregar(producto));
    }
}