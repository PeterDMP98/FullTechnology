// InventarioView.axaml.cs — Vista del inventario: menú "⋯" por fila, exportación PDF/Excel e importación desde archivo (pickers en code-behind).
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FULLTECHNOLOGY.Infrastructure.Reporting;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;
using FULLTECHNOLOGY.Presentation.ViewModels.Inventario;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class InventarioView : UserControl
{
    private ColumnResizer? _resizer;

    public InventarioView()
    {
        InitializeComponent();
        _resizer = ColumnResizer.Enable(HeaderGrid, new double[] { 70, 100, 70, 80, 80, 90, 80, 80, 80, 60, 60 });
    }

    private void OnRowLoaded(object? sender, RoutedEventArgs e) => _resizer?.AddRow(sender as Grid);

    private void OnRowUnloaded(object? sender, RoutedEventArgs e) => _resizer?.RemoveRow(sender as Grid);

    /// <summary>
    /// Menú contextual de acciones por fila (Editar / Eliminar). Los comandos
    /// viven en la fila (ProductoRowViewModel) y ya capturan el producto.
    /// </summary>
    private void OnOptionsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.DataContext is not ProductoRowViewModel row) return;

        var menu = new MenuFlyout();
        menu.Items.Add(new MenuItem { Header = "Editar", Command = row.EditarCommand });
        menu.Items.Add(new MenuItem { Header = "Eliminar", Command = row.EliminarCommand });
        menu.ShowAt(btn);
    }

    // --- Exportación (mismo patrón que AccountingView: el picker vive aquí) ---
    private async void OnExportPdf(object? sender, RoutedEventArgs e) => await ExportAsync("pdf");

    private async void OnExportExcel(object? sender, RoutedEventArgs e) => await ExportAsync("xlsx");

    private async Task ExportAsync(string ext)
    {
        if (DataContext is not InventarioViewModel vm || vm.ExportHeaders.Length == 0)
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

    // --- Importación (leída aquí y aplicada por el VM, que recarga la tabla) ---
    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InventarioViewModel vm) return;

        var top = TopLevel.GetTopLevel(this);
        var storage = top?.StorageProvider;
        var files = await storage?.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar inventario",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Inventario (Excel, CSV)") { Patterns = new[] { "*.xlsx", "*.csv" } }
            }
        })!;
        if (files is null || files.Count == 0) return;

        try
        {
            var path = files[0]!.Path.LocalPath;
            var items = path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? InventarioImport.ReadXlsx(path)
                : InventarioImport.ReadCsv(path);
            if (items.Count == 0)
            {
                await new DialogService().ShowMessageAsync("Importar", "El archivo no contiene productos.");
                return;
            }

            var (nuevos, actualizados) = vm.AplicarImportacion(items);
            await new DialogService().ShowMessageAsync("Importar",
                $"Inventario importado correctamente.\n\nNuevos: {nuevos}\nActualizados: {actualizados}");
        }
        catch (Exception ex)
        {
            await new DialogService().ShowMessageAsync("Error", $"No se pudo importar el archivo: {ex.Message}");
        }
    }

    // Nombre base seguro para el archivo: reemplaza caracteres no válidos por "_".
    private static string Sanitize(string s)
    {
        var ok = new string((s ?? "").Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        return ok.Length == 0 ? "Inventario" : ok;
    }
}