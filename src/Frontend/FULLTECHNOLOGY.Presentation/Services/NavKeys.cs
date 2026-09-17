// NavKeys.cs — Claves de navegación de los módulos del shell y sus glifos de icono correspondientes.
namespace FULLTECHNOLOGY.Presentation.Services;

// ============================================================
// Claves de navegación interna. Cada módulo tiene una vista;
// la navegación NUNCA abre ventanas por módulo (v2 usaba una
// sola ventana shell con UserControls).
// ============================================================
public static class NavKeys
{
    public const string Inicio = "Inicio";
    public const string Mantenimiento = "Mantenimiento";
    public const string Ventas = "Ventas";
    public const string Clientes = "Clientes";
    public const string Inventario = "Inventario";
    public const string Historial = "Historial";
    public const string Contable = "Contable";
    public const string Configuracion = "Configuración";

    /// <summary>
    /// Lista de todas las claves en el orden que se muestran en la barra de
    /// navegación del shell (sidebar / bottom bar).
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        Inicio, Mantenimiento, Ventas, Clientes, Inventario, Historial, Contable, Configuracion
    };

    /// <summary>
    /// Emoji (color) que representa a cada módulo en el shell
    /// (sidebar y encabezado de página).
    /// </summary>
    public static string GlyphFor(string key) => key switch
    {
        Inicio => "\uD83C\uDFE0",       // 🏠
        Mantenimiento => "\uD83D\uDD27", // 🔧
        Ventas => "\uD83D\uDED2",       // 🛒
        Clientes => "\uD83D\uDC64",     // 👤
        Inventario => "\uD83D\uDCE6",   // 📦
        Historial => "\uD83D\uDD52",    // 🕒
        Contable => "\uD83D\uDCB0",     // 💰
        Configuracion => "\u2699\uFE0F", // ⚙️
        _ => "\u25A7",                  // ▧
    };
}