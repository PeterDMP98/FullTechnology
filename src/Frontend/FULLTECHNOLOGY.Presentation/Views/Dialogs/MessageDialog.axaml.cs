// MessageDialog.axaml.cs — Diálogo base de mensaje/prompt: animación de aparición, Enter/Esc, y resultado Ok/Cancel con texto opcional.
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace FULLTECHNOLOGY.Presentation.Views.Dialogs;

public partial class MessageDialog : Window
{
    // Resultado devuelto al llamante: ¿se aceptó? y texto si era un prompt.
    public sealed record DialogResult(bool Ok, string? Text);

    public string? PromptText => Input.Text;

    public MessageDialog()
    {
        InitializeComponent();
        Opened += OnOpened;
        KeyDown += OnKeyDown;
    }

    /// <summary>Accesibilidad y foco inicial (F12): Enter acepta, Esc cancela (si hay botón).</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && CancelButton.IsVisible)
        {
            OnCancel(sender, e);
        }
        else if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && Input.IsVisible)
        {
            OnOk(sender, e);
        }
    }

    /// <summary>Aparición suave (F11): fade + scale 0.97→1 de ~140 ms (spec §25, discreto).</summary>
    private async void OnOpened(object? sender, EventArgs e)
    {
        const double ms = 140;
        Opacity = 0;
        var scale = new ScaleTransform(0.97, 0.97);
        RenderTransform = scale;
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        var t0 = DateTime.UtcNow;
        try
        {
            while (true)
            {
                var t = (DateTime.UtcNow - t0).TotalMilliseconds / ms;
                if (t >= 1.0) break;
                t = 1d - Math.Pow(1d - t, 3); // ease-out cúbico
                Opacity = t;
                scale.ScaleX = scale.ScaleY = 0.97 + 0.03 * t;
                await Task.Delay(16);
            }
        }
        finally
        {
            Opacity = 1;
            scale.ScaleX = scale.ScaleY = 1;
        }

        // Foco inicial: el campo de texto si es un prompt, si no el botón Aceptar.
        (Input.IsVisible ? (Control)Input : OkButton).Focus();
    }

    // Muestra el mensaje y, si viene initial, convierte el diálogo en un prompt
    // con campo de texto. El flag allowCancel muestra el botón Cancelar.
    public MessageDialog(string title, string message, bool allowCancel = false, string? initial = null)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        if (initial is not null)
        {
            Input.IsVisible = true;
            Input.Text = initial;
        }
        CancelButton.IsVisible = allowCancel;
    }

    // Abre el diálogo modal; sin owner lo centra en la pantalla.
    public static async Task<DialogResult> ShowAsync(Window? owner, string title, string message, bool allowCancel = false, string? initial = null)
    {
        var dlg = new MessageDialog(title, message, allowCancel, initial);
        if (owner is null)
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return await dlg.ShowDialog<DialogResult>(owner!);
        }
        return await dlg.ShowDialog<DialogResult>(owner);
    }

    private void OnOk(object? sender, RoutedEventArgs e) =>
        Close(new DialogResult(true, Input.Text));

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close(new DialogResult(false, null));
}