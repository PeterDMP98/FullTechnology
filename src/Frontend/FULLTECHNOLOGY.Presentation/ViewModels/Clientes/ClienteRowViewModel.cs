// ClienteRowViewModel.cs — Fila de la tabla de clientes: envuelve un Cliente y expone comandos de Editar/Eliminar.
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Clientes;

/// <summary>
/// Fila de la tabla de Clientes (ItemsControl + DataTemplate). Envuelve un
/// <see cref="Cliente"/> y expone los comandos de fila Editar/Eliminar.
/// </summary>
public class ClienteRowViewModel : ViewModelBase
{
    // Entidad envuelta y comandos delegados al VM padre (evitan lógica aquí).
    public Cliente Cliente { get; }
    public ICommand EditarCommand { get; }
    public ICommand EliminarCommand { get; }

    // Exposición plana de propiedades del cliente para el DataTemplate.
    public long Id => Cliente.Id;
    public string Tipo => Cliente.Tipo;
    public string Nombre => Cliente.Nombre;
    public string Documento => Cliente.Documento;
    public string Celular => Cliente.Celular;
    public string Direccion => Cliente.Direccion;
    public string Web => Cliente.Web;
    public string RedSocial => Cliente.RedSocial;

    public ClienteRowViewModel(Cliente cliente, Action<Cliente> onEditar, Action<Cliente> onEliminar)
    {
        Cliente = cliente;
        EditarCommand = new RelayCommand(() => onEditar(cliente));
        EliminarCommand = new RelayCommand(() => onEliminar(cliente));
    }
}