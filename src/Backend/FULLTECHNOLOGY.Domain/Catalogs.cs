// Catalogs.cs — Catálogo canónico de textos TEXT persistentes en BD (estados, tipos, métodos).

using FULLTECHNOLOGY.Domain.Enums;

namespace FULLTECHNOLOGY.Domain;

// ============================================================
// Catálogo canónico con los valores TEXT que persisten en la
// base de datos. Fuente única: la UI y los repositorios usan
// estos textos exactos, por lo que los datos existentes son
// compatibles (no se altera el esquema).
// ============================================================
public static class RepairStatuses
{
    // "Default" es el estado inicial (Recibido); aquí conviven el valor canónico y su alias.
    public const string Default = "Recibido";
    public const string Recibido = "Recibido";

    // Estados del flujo; el orden de declaración sigue la secuencia de trabajo.
    public const string EnDiagnostico = "En diagnóstico";
    public const string EnReparacion = "En reparación";
    public const string Reparado = "Reparado";
    public const string NoReparado = "No reparado";
    public const string ListoParaEntregar = "Listo para entregar";
    public const string Entregado = "Entregado";

    // Traduce el texto persistido a la enum; un texto desconocido cae a Recibido (tolerante a datos viejos).
    public static RepairStatus ToEnum(string? value) => value?.Trim() switch
    {
        EnDiagnostico => RepairStatus.EnDiagnostico,
        EnReparacion => RepairStatus.EnReparacion,
        ListoParaEntregar => RepairStatus.ListoParaEntregar,
        Reparado => RepairStatus.Reparado,
        NoReparado => RepairStatus.NoReparado,
        Entregado => RepairStatus.Entregado,
        _ => RepairStatus.Recibido
    };

    // Operación inversa: vuelca la enum al texto canónico que guarda la BD.
    public static string ToDbText(RepairStatus status) => status switch
    {
        RepairStatus.EnDiagnostico => EnDiagnostico,
        RepairStatus.EnReparacion => EnReparacion,
        RepairStatus.ListoParaEntregar => ListoParaEntregar,
        RepairStatus.Reparado => Reparado,
        RepairStatus.NoReparado => NoReparado,
        RepairStatus.Entregado => Entregado,
        _ => Recibido
    };

    // Los estados terminales (Entregado/No reparado) son inmutables para la edición.
    public static bool IsEditable(string? value) => value is not Entregado and not NoReparado;

    // Estados finales del flujo; el dashboard y el cierre los cuentan como terminados.
    public static bool IsFinished(string? value) => value is Entregado or NoReparado;

    /// <summary>Orden visual de las secciones del dashboard.</summary>
    public static IReadOnlyList<string> DisplayOrder { get; } = new[]
    {
        Recibido, EnDiagnostico, EnReparacion, Reparado, NoReparado, ListoParaEntregar, Entregado
    };

    // Todos los estados de la enum, para combos y filtros.
    public static IEnumerable<RepairStatus> All { get; } = Enum.GetValues<RepairStatus>();
}

public static class CustomerTypes
{
    // Etiquetas canónicas del campo Tipo (columna tipo de clientes/proveedores).
    public const string Comprador = "Cliente comprador";
    public const string Proveedor = "Proveedor";
    public static IReadOnlyList<string> All { get; } = new[] { Comprador, Proveedor };
}

public static class ProductTypes
{
    // Clasificación de inventario: el SQL del esquema valida contra estos valores.
    public const string Repuesto = "Repuesto";
    public const string Accesorio = "Accesorio";
    public static IReadOnlyList<string> All { get; } = new[] { Repuesto, Accesorio };
}

public static class PaymentMethods
{
    // Texto persistido en la columna Método; el combo de cobros usa exactamente estos valores.
    public const string Pendiente = "Pendiente";
    public const string Efectivo = "Efectivo";
    public const string Nequi = "Nequi";
    public const string Transferencia = "Transferencia";
    public const string Tarjeta = "Tarjeta";
    public const string Daviplata = "Daviplata";
    public const string Otro = "Otro";
    public const string Sinpe = "Sinpe";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Pendiente, Efectivo, Nequi, Transferencia, Tarjeta, Daviplata, Otro, Sinpe
    };
}