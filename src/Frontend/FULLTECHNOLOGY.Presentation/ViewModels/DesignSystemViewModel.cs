// DesignSystemViewModel.cs — Datos de demostración estáticos para la ventana del Design System (validación F7).
using CommunityToolkit.Mvvm.ComponentModel;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

// VM de la ventana `--design`: alimenta la galería de estilos con ejemplos
// estáticos (no toca servicios ni base de datos). Muestra los estados de una
// orden, tarjetas de métricas y una tabla de ejemplo.
public partial class DesignSystemViewModel : ViewModelBase
{
    // Estados posibles de una orden de mantenimiento (gallinas de display).
    public IReadOnlyList<string> Statuses { get; } = new[]
    {
        "Recibido", "En diagnóstico", "En reparación", "Reparado",
        "No reparado", "Listo para entregar", "Entregado",
    };

    // Tarjetas de métricas de ejemplo ("Total de órdenes", valor y clave de color).
    public IReadOnlyList<StatSample> Stats { get; } = new[]
    {
        new StatSample("Total de órdenes", "9", "AccentPrimaryLight"),
        new StatSample("Recibido", "2", "StatusSuccess"),
        new StatSample("En reparación", "3", "StatusWarning"),
        new StatSample("Listos para entregar", "1", "StatusPurple"),
        new StatSample("Entregados", "2", "StatusTeal"),
    };

    // Filas de ejemplo de una tabla de órdenes de mantenimiento.
    public IReadOnlyList<RowSample> Rows { get; } = new[]
    {
        new RowSample("ORD-001", "Juan Pérez", "CC 1001", "3001112233", "iPhone 11", "En reparación", "$ 150.000", "$ 40.000"),
        new RowSample("ORD-002", "María Gómez", "CC 1002", "3102223344", "PC Gamer", "Listo para entregar", "$ 320.000", "$ 0"),
        new RowSample("ORD-003", "Carlos Ruiz", "CC 1003", "3203334455", "Tablet", "Entregado", "$ 90.000", "$ 0"),
    };

    // Modelo de una tarjeta de métrica (etiqueta, número y clave de estilo de color).
    public sealed record StatSample(string Label, string Number, string ColorKey);

    // Modelo de una fila de tabla (todas las columnas que pinta la demo).
    public sealed record RowSample(string Orden, string Cliente, string Documento, string Celular, string Equipo, string Estado, string Total, string Saldo);
}