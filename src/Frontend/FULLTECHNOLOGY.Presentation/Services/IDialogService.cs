// IDialogService.cs — Contrato de diálogos modales de negocio (mensaje / confirmación / prompt) que consumen los ViewModels; lo implementa DialogService sobre MessageDialog propio de Avalonia (reemplazo de MessageBox/InputBox, ERR-002/003/R09). Los VMs nunca dependen de ventanas concretas ni de Avalonia.
// IDialogService.cs — Contrato de diálogos modales de negocio (mensaje / confirmación / prompt) que usan los ViewModels sin depender de ventanas concretas ni de Avalonia (reemplaza MessageBox/InputBox, ERR-002/ERR-003/R09); lo implementa DialogService sobre los MessageDialog propios de Avalonia.
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using AvaloniaApp = Avalonia.Application;

namespace FULLTECHNOLOGY.Presentation.Services;

// ============================================================
// Reemplazo de MessageBox/InputBox (ERR-002 / ERR-003 / R09):
// diálogos modales propios de Avalonia. Los VMs solo dependen
// de esta interfaz (no de WinForms ni de ventanas concretas).
// ============================================================
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);

    Task<bool> ConfirmAsync(string title, string message);

    Task<string?> PromptAsync(string title, string message, string initial = "");
}

public class DialogService : IDialogService
{
    private static Window? FindOwner() =>
        (AvaloniaApp.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    /// <summary>Ventana propietaria (MainWindow) para abrir diálogos de negocio modales.</summary>
    public static Window? OwnerWindow => FindOwner();

    public async Task ShowMessageAsync(string title, string message) =>
        await MessageDialog.ShowAsync(FindOwner(), title, message);

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var r = await MessageDialog.ShowAsync(FindOwner(), title, message, allowCancel: true);
        return r.Ok;
    }

    public async Task<string?> PromptAsync(string title, string message, string initial = "")
    {
        var r = await MessageDialog.ShowAsync(FindOwner(), title, message, allowCancel: true, initial: initial);
        return r.Ok ? r.Text : null;
    }
}