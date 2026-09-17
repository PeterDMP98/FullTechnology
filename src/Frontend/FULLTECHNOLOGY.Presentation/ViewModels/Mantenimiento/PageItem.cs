// PageItem.cs — Botón de número de página para el pie de la tabla de órdenes (con comando y estado de página actual).
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Mantenimiento;

/// <summary>Botón de número de página en el pie de la tabla (activo resaltado).</summary>
public partial class PageItem : ViewModelBase
{
    public int Number { get; }

    // Página actual: controla el resaltado visual del botón.
    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    public ICommand Go { get; }

    // Delegada: al hacer clic el VM padre cambia de página y reconstruye los botones.
    public PageItem(int number, bool isCurrent, Action<int> onGo)
    {
        Number = number;
        IsCurrent = isCurrent;
        Go = new RelayCommand(() => onGo(number));
    }
}