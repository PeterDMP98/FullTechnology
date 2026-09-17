// Enums.cs — Estados y categorías del sistema; RepairStatus se persiste como int en la BD.

namespace FULLTECHNOLOGY.Domain.Enums;

// Flujo de la orden en el dashboard: el valor numérico define el orden de las columnas.
public enum RepairStatus
{
    // Empieza en 1: coincide con los ids de estado que ya usaba el SQL de la v2.x.
    Recibido = 1,
    EnDiagnostico = 2,
    EnReparacion = 3,
    Reparado = 4,
    NoReparado = 5,
    ListoParaEntregar = 6,
    Entregado = 7
}

// Rol del tercero en el negocio; hoy se conserva como texto en Cliente.Tipo (catálogo).
public enum CustomerType
{
    Comprador = 1,
    Proveedor = 2
}

// Clasificación de inventario; hoy se conserva como texto en Producto.Tipo (catálogo).
public enum ProductType
{
    Repuesto = 1,
    Accesorio = 2
}

// Métodos de pago; sin valores fijos porque en la práctica el texto se guarda en las tablas.
public enum PaymentMethod
{
    Pendiente,
    Efectivo,
    Nequi,
    Transferencia,
    Sinpe,
    Tarjeta,
    Daviplata,
    Otro
}