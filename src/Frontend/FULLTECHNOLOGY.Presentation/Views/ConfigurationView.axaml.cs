// ConfigurationView.axaml.cs — Vista de configuración: file pickers de backup e importación de base de datos en code-behind, delegan en el VM.
// El nombre cumple la convención del ViewLocator (ConfigurationViewModel → ConfigurationView).
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FULLTECHNOLOGY.Presentation.ViewModels;

namespace FULLTECHNOLOGY.Presentation.Views;

public partial class ConfigurationView : UserControl
{
    public ConfigurationView()
    {
        InitializeComponent();
    }

    // Picker de "guardar dónde" del backup; el VM hace la copia real en esa ruta.
    private async void OnBackupClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConfigurationViewModel vm) return;
        var top = TopLevel.GetTopLevel(this);
        var file = await top?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = $"DecoTechnology_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
            DefaultExtension = "db",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Base de datos SQLite") { Patterns = new[] { "*.db" } }
            }
        })!;
        if (file is null) return;
        await vm.CopiaSeguridadCommand.ExecuteAsync(file.Path.LocalPath);
    }

    // Picker de "abrir archivo" de la BD a importar; el VM la reemplaza y reinicia.
    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConfigurationViewModel vm) return;
        var top = TopLevel.GetTopLevel(this);
        var files = await top?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar base de datos a importar",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Base de datos SQLite (*.db)") { Patterns = new[] { "*.db" } }
            }
        })!;
        if (files.Count == 0) return;
        await vm.ImportarBDCommand.ExecuteAsync(files[0].Path.LocalPath);
    }
}