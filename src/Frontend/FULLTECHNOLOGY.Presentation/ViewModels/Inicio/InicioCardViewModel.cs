// InicioCardViewModel.cs — Card del panel Inicio: contador de alertas con comando que abre su ventana flotante de detalle.
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Inicio;

/// <summary>Card clickeable del Inicio: muestra el conteo y abre la lista de alertas al pinchar.</summary>
public partial class InicioCardViewModel : ViewModelBase
{
    public string Label { get; }
    public string Glyph { get; }
    public string AccentToken { get; }
    public ICommand OpenCommand { get; }

    [ObservableProperty]
    public partial string Value { get; set; } = "0";

    public InicioCardViewModel(string label, string glyph, string accentToken, Action onOpen)
    {
        Label = label;
        Glyph = glyph;
        AccentToken = accentToken;
        OpenCommand = new RelayCommand(onOpen);
    }
}