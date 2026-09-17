// CustomersService.cs — Casos de uso de clientes/proveedores (réplica de ClienteForm v2).

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Almacén de clientes y proveedores (ClienteForm v2): CRUD,
// búsqueda, unicidad por diseño y bloqueo de borrado en uso.
// ============================================================
public class CustomersService
{
    private readonly IClienteRepository _clientes;

    public CustomersService(IClienteRepository clientes)
    {
        _clientes = clientes;
    }

    public List<Cliente> Search(string? tipo = null, string? search = null) =>
        _clientes.GetClientes(tipo, search);

    public Cliente? Get(long id) => _clientes.GetClienteById(id);

    // Útil desde la orden de servicio: localiza al comprador por documento o celular.
    public Cliente? FindComprador(string docOrCel) => _clientes.FindClienteComprador(docOrCel);

    // Se valida en el dominio antes de persistir: el repositorio solo recibe datos limpios.
    public long Save(Cliente cliente)
    {
        cliente.Validate();
        return _clientes.SaveCliente(cliente);
    }

    public void Delete(long id)
    {
        // Guard de integridad: con mantenimientos o ventas asociadas el borrado se bloquea, como en el v2.
        if (_clientes.ClienteEnUso(id))
            throw new DomainException("No se puede eliminar: el cliente tiene mantenimientos o ventas asociadas.");
        _clientes.DeleteCliente(id);
    }

    public bool EnUso(long id) => _clientes.ClienteEnUso(id);
}