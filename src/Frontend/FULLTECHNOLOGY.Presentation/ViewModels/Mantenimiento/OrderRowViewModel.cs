// OrderRowViewModel.cs — Modelos de fila y de card para la tabla de órdenes: opciones de estado, columna de orden, fila con montos formateados y card con filtro por click.
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Mantenimiento;

/// <summary>
/// Opción del combo de estado: etiqueta mostrada + valor de filtro
/// ("" = Todos, "_listos_" = Reparado+Listo para entregar, o un estado TEXT canónico).
/// </summary>
public sealed record StatusOption(string Label, string Value);

/// <summary>Filtro agregado de la card "Listos" (Reparado + Listo para entregar).</summary>
public static class StatusFilters
{
    public const string Todos = "";
    public const string Listos = "_listos_";
    public const string ListosLabel = "Listos";
}

/// <summary>Columna por la que se puede ordenar la tabla de órdenes (spec §3.3.d).</summary>
public enum SortColumn
{
    OrderNumber,
    Cliente,
    Documento,
    Celular,
    Equipo,
    Estado,
    Ingreso,
    Total,
    Saldo
}

/// <summary>Fila de la tabla de órdenes (ItemsControl + DataTemplate).</summary>
public class OrderRowViewModel : ViewModelBase
{
    private readonly CurrencyService _currency;

    public ServiceOrder Order { get; }
    public ICommand VerCommand { get; }

    // Columnas expuestas planas para el binding; los montos salen formateados.
    public long OrderId => Order.Id;
    public string OrderNumber => Order.OrderNumber;
    public string Cliente => Order.CustomerName;
    public string Documento => Order.CustomerDoc;
    public string Celular => Order.CustomerPhone;
    public string Equipo => $"{Order.Brand} {Order.Model}".Trim();
    public string Status => Order.Status;
    public string Ingreso => Order.ReceivedAt.ToString("dd/MM/yyyy HH:mm");
    public string Total => _currency.Fmt(Order.Total);
    public string Saldo => _currency.Fmt(Order.Balance);

    // Una orden con saldo pendiente puede cobrarse.
    public bool CanCobrar => Order.Balance > 0;

    public OrderRowViewModel(ServiceOrder order, CurrencyService currency, ICommand verCommand)
    {
        Order = order;
        _currency = currency;
        VerCommand = verCommand;
    }
}

/// <summary>Card de métrica con filtro por click (spec §3.3.c).</summary>
public partial class StatCardViewModel : ViewModelBase
{
    public string Label { get; }
    public string Glyph { get; }
    public string Filter { get; }
    public string AccentToken { get; }
    public ICommand SelectCommand { get; }

    // Valor del contador (lo actualiza el VM padre tras cada carga).
    [ObservableProperty]
    public partial string Value { get; set; } = "0";

    // Estado visual de selección al hacer clic en la card.
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public StatCardViewModel(string label, string glyph, string filter, string accentToken, Action<string> onSelect)
    {
        Label = label;
        Glyph = glyph;
        Filter = filter;
        AccentToken = accentToken;
        SelectCommand = new RelayCommand(() => onSelect(filter));
    }
}