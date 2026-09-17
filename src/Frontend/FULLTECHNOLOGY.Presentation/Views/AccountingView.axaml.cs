// AccountingView.axaml.cs — Vista de contabilidad: exporta el cierre a PDF/Excel usando file pickers y los reportes de Infrastructure.
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FULLTECHNOLOGY.Infrastructure.Reporting;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class AccountingView : UserControl
{
    public AccountingView()
    {
        InitializeComponent();
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