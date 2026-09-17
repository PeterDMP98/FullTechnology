// TokenBrushConverter.cs — Resuelve el nombre de un token de tema (p. ej. "StatusSuccess", "AccentPrimary", "TextSecondary") en su pincel del tema activo con fallback hex acento (#2563EB); ConverterParameter="soft" → fondo al 15% de opacidad. Un único token por concepto en toda la app (cards y pills).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using AvaloniaApp = Avalonia.Application;

namespace FULLTECHNOLOGY.Presentation.Converters;

/// <summary>
/// Convierte el nombre de un token (p. ej. "StatusSuccess", "AccentPrimary")
/// en su pincel del tema activo. Si no existe, usa AccentPrimary.
/// ConverterParameter="soft" → color al 15% de opacidad (fondo suave, spec §1).
/// </summary>
public sealed class TokenBrushConverter : IValueConverter
{
    private static readonly Color Fallback = new(255, 0x25, 0x63, 0xEB);
    private const byte SoftAlpha = 38; // ≈ 15% de opacidad (spec §1)

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var token = value as string;
        if (token is not null && AvaloniaApp.Current?.TryFindResource(token, out var o) == true && o is IBrush b)
        {
            var soft = parameter is string s && s.Equals("soft", StringComparison.OrdinalIgnoreCase);
            if (!soft || b is not ISolidColorBrush solid)
                return b;
            var c = solid.Color;
            return new SolidColorBrush(new Color(SoftAlpha, c.R, c.G, c.B));
        }
        return new SolidColorBrush(Fallback);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}