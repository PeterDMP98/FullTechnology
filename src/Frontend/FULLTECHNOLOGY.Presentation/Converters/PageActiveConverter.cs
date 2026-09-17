// PageActiveConverter.cs — Devuelve el pincel de acento (página/menú activo) o el fondo de tarjeta (inactivo) resolviendo los tokens del tema actual; con fallback si el token no está registrado.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using AvaloniaApp = Avalonia.Application;

namespace FULLTECHNOLOGY.Presentation.Converters;

/// <summary>
/// true → pincel de acento (página activa); false → fondo de tarjeta.
/// Resuelve los tokens del tema actual (single source).
/// </summary>
public sealed class PageActiveConverter : IValueConverter
{
    private static readonly Color FallbackActive = new(255, 0x25, 0x63, 0xEB);
    private static readonly Color FallbackInactive = new(255, 0x16, 0x1F, 0x37);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var token = value is true ? "AccentPrimary" : "BgCard";
        if (AvaloniaApp.Current?.TryFindResource(token, out var o) == true && o is IBrush b)
            return b;
        return new SolidColorBrush(value is true ? FallbackActive : FallbackInactive);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}