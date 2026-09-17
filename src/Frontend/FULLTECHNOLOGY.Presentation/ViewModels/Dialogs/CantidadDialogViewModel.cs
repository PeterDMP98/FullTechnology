// CantidadDialogViewModel.cs — Diálogo que pide la cantidad al agregar un producto al carrito, limitado al stock disponible.
using CommunityToolkit.Mvvm.ComponentModel;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

/// <summary>
/// CantidadPBF v2: pide la cantidad al agregar un producto al carrito,
/// con máximo = stock disponible menos lo ya agregado (1..máx).
/// </summary>
public partial class CantidadDialogViewModel : ViewModelBase
{
    // Título (nombre del producto) y cantidad máxima permitida.
    public string Title { get; private set; } = "";
    public decimal Max { get; private set; } = 1;

    // Cantidad elegida (la lee el VM de venta al cerrar el diálogo).
    [ObservableProperty]
    public partial decimal Cantidad { get; set; } = 1;

    // Calcula el máximo: stock restante (stock − lo ya en el carrito), nunca < 1.
    public void Initialize(Producto producto, int yaEnCarro)
    {
        Title = producto.Nombre;
        var max = Math.Max(1, producto.Stock - yaEnCarro);
        Max = max;
        Cantidad = Math.Clamp(1m, 1m, Max);
    }
}