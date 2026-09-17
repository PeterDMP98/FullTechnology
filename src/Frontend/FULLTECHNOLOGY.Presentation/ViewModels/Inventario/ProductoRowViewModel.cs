// ProductoRowViewModel.cs — Fila de la tabla de inventario: envuelve un producto y expone columnas formateadas más los comandos Editar/Eliminar.
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Inventario;

/// <summary>
/// Fila de la tabla de Inventario (ItemsControl + DataTemplate). Envuelve un
/// <see cref="Producto"/> y expone los comandos de fila Editar/Eliminar.
/// </summary>
public class ProductoRowViewModel : ViewModelBase
{
    private readonly CurrencyService _currency;

    public Producto Producto { get; }
    public ICommand EditarCommand { get; }
    public ICommand EliminarCommand { get; }

    // Columnas expuestas planas; los montos salen formateados y los campos
    // opcionales nunca rompen el binding (cadena vacía si son null).
    public string Codigo => Producto.Codigo;
    public string Nombre => Producto.Nombre;
    public string Tipo => Producto.Tipo;
    public string Costo => _currency.Fmt(Producto.Costo);
    public string PrecioVenta => _currency.Fmt(Producto.PrecioVenta);
    public string Proveedor => Producto.Proveedor ?? "";
    public string Ingreso => Producto.FechaIngreso.ToString("dd/MM/yyyy");
    public string Garantia => Producto.Garantia ?? "";
    public string Ubicado => Producto.Ubicado ?? "";
    public string Stock => Producto.Stock.ToString();

    // Recibe las acciones del VM padre para delegar la edición/eliminación.
    public ProductoRowViewModel(Producto producto, CurrencyService currency, Action<Producto> onEditar, Action<Producto> onEliminar)
    {
        Producto = producto;
        _currency = currency;
        EditarCommand = new RelayCommand(() => onEditar(producto));
        EliminarCommand = new RelayCommand(() => onEliminar(producto));
    }
}