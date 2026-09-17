// InventoryService.cs — Inventario: CRUD de productos y ajustes de stock (InventarioForm v2).

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Inventario (InventarioForm v2): CRUD de productos, ajuste de
// stock y código autogenerado cuando se deja en blanco.
// ============================================================
public class InventoryService
{
    private readonly IProductoRepository _productos;

    public InventoryService(IProductoRepository productos)
    {
        _productos = productos;
    }

    public List<Producto> Search(string? tipo = null, string? search = null) =>
        _productos.GetProductos(tipo, search);

    public Producto? Get(long id) => _productos.GetProducto(id);

    public long Save(Producto producto)
    {
        producto.Validate();
        // Sin fecha de ingreso se sella con hoy: el listado y los reportes de antigüedad la necesitan.
        if (producto.FechaIngreso == default)
            producto.FechaIngreso = DateTime.Now;
        return _productos.SaveProducto(producto);
    }

    public void Delete(long id) => _productos.DeleteProducto(id);

    public void UpdateStock(long id, int newStock)
    {
        // El ajuste manual nunca puede dejar stock negativo (unidades físicas).
        if (newStock < 0)
            throw new ValidationException("El stock no puede ser negativo.");
        _productos.UpdateProductoStock(id, newStock);
    }
}