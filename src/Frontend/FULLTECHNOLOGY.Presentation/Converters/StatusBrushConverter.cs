// StatusBrushConverter.cs — Mapea el estado de un módulo/equipo (Recibido, En diagnóstico, En reparación, Reparado, Entregado…) al pincel de token de tema correspondiente (un único color por estado en toda la app: cards y pills), con tabla hex de fallback si el token no está registrado y modo "soft" (fondo al 15%) mediante ConverterParameter.
// StatusBrushConverter.cs — Convierte el estado de un módulo/equipo (Recibido, En diagnóstico, En reparación, Reparado, No reparado, Listo para entregar, Entregado…) en el pincel de token de tema correspondiente (un único color por estado en toda la app, spec §1), con hex de fallback si el token falta y modo "soft" (fondo al ~15%) vía ConverterParameter.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using AvaloniaApp = Avalonia.Application;

namespace FULLTECHNOLOGY.Presentation.Converters;

// ============================================================
// Mapeo consistente estado→color (spec §1 + §22). Cada estado
// tiene UN color en toda la app (cards + pills):
//   Recibido            → StatusSuccess (verde)
//   En diagnóstico      → StatusInfo    (cian/sky)
//   En reparación       → StatusWarning (ámbar)
//   Reparado            → StatusSuccess (verde)
//   No reparado         → DangerBadge   (rojo)
//   Listo para entregar → StatusPurple  (violeta)
//   Entregado           → StatusTeal    (teal)
//   (cualquier otro)    → TextSecondary (gris)
// El color se resuelve desde los tokens de tema (single source);
// la tabla hex es solo fallback (p. ej. pruebas sin Application).
// ConverterParameter="soft" → fondo al 15% de opacidad (spec §1).
// ============================================================
public sealed class StatusBrushConverter : IValueConverter
{
    private const byte SoftAlpha = 38; // ≈ 15% de opacidad (spec §1)

    public static IBrush For(string? status, bool soft = false)
    {
        var color = ColorFor(status);
        return new SolidColorBrush(soft ? new Color(SoftAlpha, color.R, color.G, color.B) : color);
    }

    internal static Color ColorFor(string? status)
    {
        var token = TokenFor(status);
        return FindTokenColor(token) ?? FallbackHex(token);
    }

    private static string TokenFor(string? status) => status switch
    {
        "Recibido" => "StatusSuccess",
        "En diagnóstico" => "StatusInfo",
        "En reparación" => "StatusWarning",
        "Reparado" => "StatusSuccess",
        "No reparado" => "DangerBadge",
        "Listo para entregar" => "StatusPurple",
        "Entregado" => "StatusTeal",
        _ => "TextSecondary",
    };

    private static Color? FindTokenColor(string token)
    {
        if (AvaloniaApp.Current?.TryFindResource(token, out var o) == true && o is IBrush b && b is ISolidColorBrush s)
            return s.Color;
        return null;
    }

    private static Color FallbackHex(string token) => token switch
    {
        "StatusSuccess" => new Color(255, 0x22, 0xC5, 0x5E),
        "StatusInfo" => new Color(255, 0x38, 0xBD, 0xF8),
        "StatusWarning" => new Color(255, 0xF5, 0x9E, 0x0B),
        "DangerBadge" => new Color(255, 0xEF, 0x44, 0x44),
        "StatusPurple" => new Color(255, 0x8B, 0x5C, 0xF6),
        "StatusTeal" => new Color(255, 0x14, 0xB8, 0xA6),
        _ => new Color(255, 0x8A, 0x94, 0xA6),
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var soft = parameter is string s && s.Equals("soft", StringComparison.OrdinalIgnoreCase);
        return For(value as string, soft);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}