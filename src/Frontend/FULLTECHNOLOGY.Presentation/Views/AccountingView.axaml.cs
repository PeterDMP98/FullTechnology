// AccountingView.axaml.cs — Vista de contabilidad: exporta el cierre a PDF/Excel usando file pickers y los reportes de Infrastructure.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using FULLTECHNOLOGY.Infrastructure.Reporting;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class AccountingView : UserControl
{
    // --- Redimensionado estilo Excel de las columnas del cierre (celdas dinámicas) ---
    private double[] _cellWidths = Array.Empty<double>();
    private readonly List<Border> _cierreRows = new();
    private int _cierreDragColumn = -1;
    private double _cierreDragStartX;
    private double _cierreDragStartWidth;

    private const double CellWidthMax = 500;
    private const double CellSpacingX = 4;

    public AccountingView()
    {
        InitializeComponent();
        EnsureCierreWidths();
    }

    private void OnCierreHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        var x = e.GetCurrentPoint(HeaderCells).Position.X;
        var column = CierreColumnAtX(x);
        if (column < 0) return;

        _cierreDragColumn = column;
        _cierreDragStartX = x;
        _cierreDragStartWidth = _cellWidths[column];
        e.Pointer.Capture(sender as Control ?? HeaderCells);
        e.Handled = true;
    }

    private void OnCierreHeaderMoved(object? sender, PointerEventArgs e)
    {
        if (_cierreDragColumn < 0) return;
        var x = e.GetCurrentPoint(HeaderCells).Position.X;
        EnsureCierreWidths();
        var width = Math.Clamp(_cierreDragStartWidth + (x - _cierreDragStartX), 60, CellWidthMax);
        ApplyCierreCellWidth(_cierreDragColumn, width);
        e.Handled = true;
    }

    private void OnCierreHeaderReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_cierreDragColumn < 0) return;
        _cierreDragColumn = -1;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnCierreRowLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Border row) return;
        if (!_cierreRows.Contains(row)) _cierreRows.Add(row);
        EnsureCierreWidths();
        ApplyCierreRowWidths(row);
    }

    private void OnCierreRowUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Border row) _cierreRows.Remove(row);
    }

    // Devuelve la columna cuyo borde derecho está cerca de x (en píxeles del ItemsControl del header).
    private int CierreColumnAtX(double x)
    {
        EnsureCierreWidths();
        double cumulative = 0;
        for (int i = 0; i < _cellWidths.Length - 1; i++)
        {
            cumulative += _cellWidths[i];
            var boundary = cumulative + (i * CellSpacingX);
            if (Math.Abs(x - boundary) <= 6) return i;
        }
        return -1;
    }

    // Mantiene _cellWidths sincronizado con las celdas del header (crece con 110 cuando hay nuevas columnas).
    private void EnsureCierreWidths()
    {
        var count = CollectTextBlocks(HeaderCells).Count;
        if (_cellWidths.Length < count)
        {
            var grown = new double[count];
            Array.Copy(_cellWidths, grown, _cellWidths.Length);
            for (int i = _cellWidths.Length; i < count; i++) grown[i] = 110;
            _cellWidths = grown;
        }
    }

    private void ApplyCierreCellWidth(int column, double width)
    {
        _cellWidths[column] = width;
        ApplyCierreRowWidths(HeaderCells);
        foreach (var row in _cierreRows) ApplyCierreRowWidths(row);
    }

    private void ApplyCierreRowWidths(Control root)
    {
        var blocks = CollectTextBlocks(root);
        for (int i = 0; i < blocks.Count && i < _cellWidths.Length; i++)
            blocks[i].Width = _cellWidths[i];
    }

    private static List<TextBlock> CollectTextBlocks(object? visual)
    {
        var result = new List<TextBlock>();
        if (visual is not Visual v) return result;
        foreach (var child in v.GetVisualChildren())
        {
            if (child is TextBlock tb) result.Add(tb);
            else result.AddRange(CollectTextBlocks(child));
        }
        return result;
    }

    // --- Exportación (el picker de archivo vive en code-behind de la vista) ---
    private async void OnExportPdf(object? sender, RoutedEventArgs e) => await ExportAsync("pdf");

    private async void OnExportExcel(object? sender, RoutedEventArgs e) => await ExportAsync("xlsx");

    // Pide la ruta de destino y delega en PdfReport/ExcelReport los datos que
    // prepara el VM (títulos, encabezados, anchos y filas ya formateadas).
    private async Task ExportAsync(string ext)
    {
        if (DataContext is not AccountingViewModel vm || vm.ExportHeaders.Length == 0)
            return;

        var top = TopLevel.GetTopLevel(this);
        var storage = top?.StorageProvider;
        var file = await storage?.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = Sanitize(vm.FileNameBase) + "." + ext,
            DefaultExtension = ext,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(ext.ToUpperInvariant()) { Patterns = new[] { $"*.{ext}" } }
            }
        })!;
        if (file is null) return;

        try
        {
            var path = file.Path.LocalPath;
            if (ext == "pdf")
                PdfReport.WriteTable(path, vm.ExportTitle, vm.ExportSublinea, vm.ExportHeaders, vm.ExportWidths, vm.ExportRows);
            else
                ExcelReport.WriteTable(path, vm.ExportTitle, vm.ExportSublinea, vm.ExportHeaders, vm.ExportRows);

            await new DialogService().ShowMessageAsync("Descargar", $"Archivo {ext.ToUpperInvariant()} generado correctamente.\n\n{path}");
        }
        catch (Exception ex)
        {
            await new DialogService().ShowMessageAsync("Error", $"No se pudo generar el archivo: {ex.Message}");
        }
    }

    // Nombre base seguro para el archivo: reemplaza caracteres no válidos por "_".
    private static string Sanitize(string s)
    {
        var ok = new string((s ?? "").Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        return ok.Length == 0 ? "Cierre" : ok;
    }
}