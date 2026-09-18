// DashboardService.cs — Alertas del panel Inicio (v3.5): stock bajo y órdenes estancadas.

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Panel Inicio (v3.5). Reemplaza al placeholder: muestra avisos
// de dos naturalezas, ambas configurables en Configuración:
//   1. Stock por agotarse (accesorios/repuestos bajo umbral).
//   2. Órdenes atascadas en el tiempo: "recibido→listo" con más
//      de X días en proceso y "listo→entregado" sin recoger.
// La fecha ancla de una orden es StatusChangedAt (el último
// cambio de estado), con ReceivedAt como respaldo para las
// cargadas antes de la v3.5.
// ============================================================

/// <summary>Ítem de stock bajo: el producto y el umbral que disparó el aviso.</summary>
public sealed record StockAlertItem(Producto Producto, string Tipo, int Umbral);

/// <summary>Ítem de tiempo: orden con los días acumulados y a qué etapa pertenece el atraso.</summary>
public sealed record TimeAlertItem(ServiceOrder Order, int Dias, string Etapa);

public class DashboardService
{
    private readonly IProductoRepository _productos;
    private readonly IServiceOrderRepository _orders;
    private readonly AlertsSettingsService _alerts;

    public DashboardService(IProductoRepository productos, IServiceOrderRepository orders, AlertsSettingsService alerts)
    {
        _productos = productos;
        _orders = orders;
        _alerts = alerts;
    }

    // --- Stock bajo -----------------------------------------------------------

    /// <summary>Productos de accesorios/repuestos con stock menor o igual al umbral de su tipo.</summary>
    public List<StockAlertItem> GetLowStockAlerts()
    {
        var items = new List<StockAlertItem>();
        AddTipo(items, ProductTypes.Accesorio, _alerts.StockAccesorioUmbral);
        AddTipo(items, ProductTypes.Repuesto, _alerts.StockRepuestoUmbral);
        return items;
    }

    private void AddTipo(List<StockAlertItem> items, string tipo, int umbral)
    {
        foreach (var p in _productos.GetProductos(tipo))
        {
            if (p.Stock <= umbral)
                items.Add(new StockAlertItem(p, tipo, umbral));
        }
    }

    // --- Tiempo de las órdenes ------------------------------------------------

    /// <summary>Órdenes aún en el taller (recibido→listo) con más de X días en proceso.</summary>
    public List<TimeAlertItem> GetProcessAlerts()
    {
        var items = new List<TimeAlertItem>();
        foreach (var o in _orders.GetAll())
        {
            if (RepairStatuses.IsFinished(o.Status) || o.Status == RepairStatuses.ListoParaEntregar) continue;
            var dias = DaysSince(o);
            if (dias >= _alerts.DiasRecibidoAListo)
                items.Add(new TimeAlertItem(o, dias, "En proceso"));
        }
        return items.OrderByDescending(i => i.Dias).ToList();
    }

    /// <summary>Órdenes listas para entregar sin recoger con más de X días de espera.</summary>
    public List<TimeAlertItem> GetPickupAlerts()
    {
        var items = new List<TimeAlertItem>();
        foreach (var o in _orders.GetAll())
        {
            if (o.Status != RepairStatuses.ListoParaEntregar || o.DeliveredAt is not null) continue;
            var dias = DaysSince(o);
            if (dias >= _alerts.DiasListoAEntregado)
                items.Add(new TimeAlertItem(o, dias, "Lista sin entregar"));
        }
        return items.OrderByDescending(i => i.Dias).ToList();
    }

    /// <summary>Total de alertas activas (para el contador del panel).</summary>
    public int CountActiveAlerts() => GetLowStockAlerts().Count + GetProcessAlerts().Count + GetPickupAlerts().Count;

    // Antigüedad desde el último cambio de estado; si nunca se selló (BD v3.5-)
    // se usa la fecha de recibo.
    private static int DaysSince(ServiceOrder o)
    {
        var anchor = o.StatusChangedAt ?? o.ReceivedAt;
        return (DateTime.Now - anchor).Days;
    }
}