// AlertListDialogViewModel.cs — Ventana flotante de detalle de alertas (Inicio): título, subtítulo y lista simple de ítems.
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

/// <summary>Contenido de la ventana de lista usada por las alertas del Inicio (stock y tiempos).</summary>
public sealed class AlertListDialogViewModel
{
    public string Title { get; }
    public string Subtitle { get; }
    public IReadOnlyList<AlertRowItem> Rows { get; }

    public AlertListDialogViewModel(string title, string subtitle, IReadOnlyList<AlertRowItem> rows)
    {
        Title = title;
        Subtitle = subtitle;
        Rows = rows;
    }
}