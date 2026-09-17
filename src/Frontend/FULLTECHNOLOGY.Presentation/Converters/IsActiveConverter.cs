// IsActiveConverter.cs — Devuelve un pincel de acento (#2563EB) si el valor es true (ítem activo/seleccionado) y transparente en caso contrario. Se usa en módulos para resaltar la fila o pestaña activa.
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace FULLTECHNOLOGY.Presentation.Converters;

/// <summary>true → pincel de acento (ítem activo); false → transparente.</summary>
public sealed class IsActiveConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? new SolidColorBrush(Color.Parse("#2563EB")) : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}