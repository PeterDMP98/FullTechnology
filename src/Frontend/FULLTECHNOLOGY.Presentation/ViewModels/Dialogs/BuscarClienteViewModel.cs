// BuscarClienteViewModel.cs — Diálogo de búsqueda en vivo de clientes (comprador/proveedor) para seleccionar y devolver uno al VM llamante.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Búsqueda de cliente comprador (BuscarClienteDialog v2): filtra en
// vivo por nombre, documento o celular; doble-clic o "Aceptar".
// ============================================================
public partial class BuscarClienteViewModel : ViewModelBase
{
    private readonly CustomersService _customers;
    private string _tipo = "Cliente comprador";

    // Título del diálogo según el tipo buscado (comprador o proveedor).
    public string Title { get; private set; } = "";

    // Texto de búsqueda (recarga en vivo al cambiar).
    [ObservableProperty]
    public partial string Search { get; set; } = "";

    public ObservableCollection<ClienteRow> Results { get; } = new();

    // Cliente elegido por el llamante (se lee tras cerrar el diálogo).
    public Cliente? Selected { get; private set; }

    public bool TieneResultados { get; private set; } = true;

    public BuscarClienteViewModel(CustomersService customers)
    {
        _customers = customers;
    }

    // Configura el diálogo y lanza la primera búsqueda.
    public void Initialize(string tipo = "Cliente comprador")
    {
        _tipo = tipo;
        Title = tipo == "Proveedor" ? "Buscar proveedor" : "Buscar cliente comprador";
        Reload();
    }

    partial void OnSearchChanged(string value) => Reload();

    // Ejecuta la búsqueda en el servicio y rellena los resultados.
    public void Reload()
    {
        Results.Clear();
        foreach (var cl in _customers.Search(_tipo, Search))
            Results.Add(new ClienteRow(cl.Nombre, cl.Documento, cl.Celular) { Cliente = cl });
        TieneResultados = Results.Count > 0;
        OnPropertyChanged(nameof(TieneResultados));
    }

    // [RelayCommand] genera SeleccionarCommand (doble-clic / "Aceptar").
    [RelayCommand]
    private void Seleccionar(ClienteRow? row)
    {
        if (row is null) return;
        Selected = row.Cliente;
    }
}