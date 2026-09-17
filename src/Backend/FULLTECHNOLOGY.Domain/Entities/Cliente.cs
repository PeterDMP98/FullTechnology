// Cliente.cs — Entidad de clientes y proveedores con las reglas mínimas de negocio.

namespace FULLTECHNOLOGY.Domain.Entities;

using FULLTECHNOLOGY.Domain.Exceptions;

/// <summary>Cliente o proveedor del negocio; el campo Tipo distingue ambos roles.</summary>
public class Cliente
{
    public long Id { get; set; }
    // Default del rol más frecuente (comprador); Proveedor se elige en el form.
    public string Tipo { get; set; } = "Cliente comprador";
    // Datos de contacto/identificación denormalizados: los recibos imprimen sin consultar el catálogo.
    public string Nombre { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Celular { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Web { get; set; } = "";
    public string RedSocial { get; set; } = "";

    /// <summary>Reglas de negocio de un cliente/proveedor.</summary>
    public void Validate()
    {
        // Invariante: un cliente sin nombre no puede persistir (ni facturar, ni localizarlo).
        if (string.IsNullOrWhiteSpace(Nombre))
            throw new ValidationException("El nombre es obligatorio.");
        // El tipo no es texto libre: se limita a los dos valores del catálogo para no ensuciar la BD.
        if (Tipo != CustomerTypes.Comprador && Tipo != CustomerTypes.Proveedor)
            throw new ValidationException($"El tipo de cliente debe ser '{CustomerTypes.Comprador}' o '{CustomerTypes.Proveedor}'.");
    }
}