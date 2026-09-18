using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace FULLTECHNOLOGY.Presentation.Services;

/// <summary>
/// Redimensiona las columnas de las tablas custom (header Grid + filas Grid dentro de
/// un ItemsControl) arrastrando el borde de cada columna, al estilo Excel.
///
/// Uso (en el code-behind de la vista):
///   _resizer = ColumnResizer.Enable(HeaderGrid, new double[] { 60, 80, ... });
///   // en el DataTemplate, el <Grid> de cada fila registra/des-registra su Grid:
///   //   Loaded="OnRowLoaded" Unloaded="OnRowUnloaded"
///   private void OnRowLoaded(object? s, RoutedEventArgs e) => _resizer?.AddRow(s as Grid);
///   private void OnRowUnloaded(object? s, RoutedEventArgs e) => _resizer?.RemoveRow(s as Grid);
///
/// Los anchos son por sesión (no se persisten). El mínimo por columna viene en minWidths
/// y el máximo es común (maxWidth); al arrastrar se escribe el ancho en el header y en
/// todas las filas registradas.
/// </summary>
public sealed class ColumnResizer
{
    private readonly Grid _header;
    private readonly double[] _minWidths;
    private readonly double _maxWidth;
    private readonly List<Grid> _rows = new();
    private int _dragColumn = -1;
    private double _dragStartX;
    private double _dragStartWidth;

    private ColumnResizer(Grid header, double[] minWidths, double maxWidth)
    {
        _header = header;
        _minWidths = minWidths;
        _maxWidth = maxWidth;
        AddGrips();
    }

    public static ColumnResizer Enable(Grid header, double[] minWidths, double maxWidth = 900)
        => new(header, minWidths, maxWidth);

    /// <summary>Registra el Grid de una fila (Loaded) y le aplica los anchos actuales.</summary>
    public void AddRow(Grid? row)
    {
        if (row is null || _rows.Contains(row)) return;
        ApplyRowWidths(row);
        _rows.Add(row);
    }

    /// <summary>Des-registra el Grid de una fila (Unloaded).</summary>
    public void RemoveRow(Grid? row)
    {
        if (row is null) return;
        _rows.Remove(row);
    }

    // Agarrador transparente (6px) pegado al borde derecho de cada columna, salvo la última.
    private void AddGrips()
    {
        var count = _header.ColumnDefinitions.Count;
        for (int i = 0; i < count - 1; i++)
        {
            var column = i;
            var grip = new Border
            {
                Width = 6,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.SizeWestEast)
            };
            Grid.SetColumn(grip, column);

            grip.PointerPressed += (_, e) => OnResizePressed(e, column, grip);
            grip.PointerMoved += (_, e) => OnResizeMoved(e, column);
            grip.PointerReleased += (_, e) => OnResizeReleased(e, column);

            _header.Children.Add(grip);
        }
    }

    private void OnResizePressed(PointerPressedEventArgs e, int column, Control grip)
    {
        _dragColumn = column;
        _dragStartX = e.GetCurrentPoint(_header).Position.X;
        _dragStartWidth = ColumnPixelWidth(column);
        e.Pointer.Capture(grip);
        e.Handled = true;
    }

    private void OnResizeMoved(PointerEventArgs e, int column)
    {
        if (_dragColumn != column) return;

        var x = e.GetCurrentPoint(_header).Position.X;
        var min = _minWidths.Length > column && _minWidths[column] > 0 ? _minWidths[column] : 60;
        var width = Math.Clamp(_dragStartWidth + (x - _dragStartX), min, _maxWidth);
        SetColumnWidth(column, width);
        e.Handled = true;
    }

    private void OnResizeReleased(PointerEventArgs e, int column)
    {
        if (_dragColumn != column) return;
        _dragColumn = -1;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void SetColumnWidth(int column, double width)
    {
        var w = new GridLength(width, GridUnitType.Pixel);
        _header.ColumnDefinitions[column].Width = w;
        foreach (var row in _rows)
            row.ColumnDefinitions[column].Width = w;
    }

    private void ApplyRowWidths(Grid row)
    {
        var count = Math.Min(row.ColumnDefinitions.Count, _header.ColumnDefinitions.Count);
        for (int i = 0; i < count; i++)
            row.ColumnDefinitions[i].Width = _header.ColumnDefinitions[i].Width;
    }

    // Ancho real de la columna en píxeles (las columnas * se miden por su contenido).
    private double ColumnPixelWidth(int column)
    {
        var def = _header.ColumnDefinitions[column];
        if (def.Width.IsAbsolute) return def.Width.Value;

        foreach (var child in _header.Children)
            if (Grid.GetColumn(child) == column && child is Control c)
                return c.Bounds.Width;
        return 100;
    }
}