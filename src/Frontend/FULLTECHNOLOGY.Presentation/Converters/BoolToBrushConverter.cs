// BoolToBrushConverter.cs — Convierte true/false en un Brush: true → TrueBrush, false → FalseBrush (por defecto transparente). Se usa p. ej. como fondo de filas según selección o estado.
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace FULLTECHNOLOGY.Presentation.Converters;

/// <summary>true → TrueBrush; false → FalseBrush (por defecto transparente).</summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }

    public IBrush? FalseBrush { get; set; } = Brushes.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? TrueBrush ?? Brushes.Transparent : FalseBrush ?? Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}