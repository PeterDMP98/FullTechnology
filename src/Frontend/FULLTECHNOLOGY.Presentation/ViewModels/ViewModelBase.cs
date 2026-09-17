// ViewModelBase.cs — Base común de todos los ViewModels: expone estado de error (mensaje amigable) y comando de reintento.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Presentation.Services;

namespace FULLTECHNOLOGY.Presentation.ViewModels;

/// <summary>
/// Base común de los ViewModels del módulo. Tras F12 expone el estado de
/// error de carga (mensaje amigable, sin excepciones crudas en la UI) y el
/// comando de reintento que solo actúa cuando el VM implementa IRefreshable.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    public partial bool IsError { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Reintenta la recarga del módulo (no-op en VMs sin IRefreshable).</summary>
    [RelayCommand]
    private void Retry() => (this as IRefreshable)?.Refresh();
}