// StatusBrushConverterTests.cs — Pruebas del convertidor de estados a colores de pincel (sólido y suave) en la UI.

using Xunit;
using Avalonia.Media;
using FULLTECHNOLOGY.Presentation.Converters;

namespace FULLTECHNOLOGY.Presentation.Tests;

/// <summary>Pruebas del mapeo de estados persistidos a colores estables y de los pinceles sólido/suave.</summary>
public class StatusBrushConverterTests
{
    [Theory]
    [InlineData("Recibido", 0x22, 0xC5, 0x5E)]
    [InlineData("En diagnóstico", 0x38, 0xBD, 0xF8)]
    [InlineData("En reparación", 0xF5, 0x9E, 0x0B)]
    [InlineData("Reparado", 0x22, 0xC5, 0x5E)]
    [InlineData("No reparado", 0xEF, 0x44, 0x44)]
    [InlineData("Listo para entregar", 0x8B, 0x5C, 0xF6)]
    [InlineData("Entregado", 0x14, 0xB8, 0xA6)]
    [InlineData("Cualquier otro", 0x8A, 0x94, 0xA6)]
    public void ColorFor_maps_each_status_to_a_stable_color(string status, byte r, byte g, byte b)
    {
        // "Cualquier otro" cubre los estados desconocidos: caen en un color por defecto estable.
        var color = StatusBrushConverter.ColorFor(status);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }

    [Fact]
    public void Solid_brush_uses_full_alpha()
    {
        var brush = Assert.IsType<SolidColorBrush>(StatusBrushConverter.For("Recibido"));
        Assert.Equal(255, brush.Color.A);
    }

    [Fact]
    public void Soft_brush_uses_15_percent_alpha()
    {
        var brush = Assert.IsType<SolidColorBrush>(StatusBrushConverter.For("Recibido", soft: true));
        Assert.Equal(38, brush.Color.A);
        Assert.Equal(0x22, brush.Color.R);
        Assert.Equal(0xC5, brush.Color.G);
        Assert.Equal(0x5E, brush.Color.B);
    }
}