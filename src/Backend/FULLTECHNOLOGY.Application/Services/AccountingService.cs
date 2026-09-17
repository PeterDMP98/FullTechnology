// AccountingService.cs — Cierre contable, balance y exportación (réplica de ContableView v2).

using FULLTECHNOLOGY.Application.Ports;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Application.Services;

// ============================================================
// Cierre contable y balance. Replica ContableView v2: vistas
// (movimientos, tipo de ingreso, método, producto, cliente),
// totales del periodo y series para la gráfica de balance.
// El formato de moneda se delega en CurrencyService, por lo que
// el catálogo sale listo para exportar a PDF/Excel.
// ============================================================
public class AccountingService
{
    // Categorías del cierre por tipo de ingreso; "Otros" aglutina todo método distinto de Efectivo/Nequi.
    private const string Efectivo = "Efectivo";
    private const string Nequi = "Nequi";
    private const string Otros = "Otros";
    // Métodos que se agregan en la vista "Por método"; el texto coincide con el guardado en las ventas.
    private static readonly string[] Metodos = { "Efectivo", "Nequi", "Transferencia", "Tarjeta", "Daviplata", "Otros" };

    private readonly IVentaRepository _ventas;
    private readonly IAccountingRepository _accounting;
    private readonly CurrencyService _money;

    public AccountingService(IVentaRepository ventas, IAccountingRepository accounting, CurrencyService money)
    {
        _ventas = ventas;
        _accounting = accounting;
        _money = money;
    }

    /// <summary>
    /// Construye el catálogo de la vista indicada (0..4 igual que v2)
    /// con los filtros activos. Devuelve los totales del periodo y las
    /// filas visibles + las filas para exportar (con resumen de facturas).
    /// </summary>
    public ClosingSummary BuildCierre(DateTime from, DateTime to, string metodo, string search, int vista)
    {
        // Rango normalizado a fechas enteras; si "to" vino antes que "from" se revierte al día inicial.
        var f = from.Date;
        var t = to.Date < from ? f : to.Date;
        var m = metodo;
        // Búsqueda insensible a mayúsculas aplicada en memoria, igual que el filtro del v2.
        var s = search?.Trim().ToLower() ?? "";

        var ventas = _ventas.GetVentasConLineas(f, t, m);
        var cobros = _accounting.GetCobrosConOrden(f, t, m);

        // El criterio de coincidencia mira cabecera y líneas (ventas) o la orden/pago (cobros).
        if (s.Length > 0)
        {
            ventas = ventas.Where(x => x.venta.VentaNumber.ToLower().Contains(s) ||
                                       x.venta.ClienteNombre.ToLower().Contains(s) ||
                                       x.linea.ProductoNombre.ToLower().Contains(s)).ToList();
            cobros = cobros.Where(x => x.pago.Concepto.ToLower().Contains(s) ||
                                       x.orden.OrderNumber.ToLower().Contains(s) ||
                                       x.orden.CustomerName.ToLower().Contains(s)).ToList();
        }

        // 0=movimientos, 1=tipo de ingreso, 2=método, 3=por producto, 4=por cliente (misma numeración del v2).
        var (headers, widths, rows) = vista switch
        {
            1 => PorTipoIngreso(ventas, cobros),
            2 => PorMetodo(ventas, cobros),
            3 => PorProducto(ventas),
            4 => PorCliente(ventas, cobros),
            _ => Movimientos(ventas, cobros)
        };

        // Una venta cuenta una vez aunque tenga varias líneas: se agrupa por Id antes de sumar y contar.
        var totV = ventas.GroupBy(x => x.venta.Id).Sum(g => g.First().venta.Total);
        var totC = cobros.Sum(x => x.pago.Monto);
        var totals = new ClosingTotals(totV, totC,
            ventas.GroupBy(x => x.venta.Id).Count(), cobros.Count);

        return new ClosingSummary(totals, new ClosingCatalog(headers, widths, rows), BuildExport(headers.Length, rows, totals));
    }

    // Serie por día para la gráfica de balance; las fechas ya vienen agrupadas por el SQL.
    public List<(DateTime fecha, decimal ventas, decimal cobros)> BalanceSeries(DateTime from, DateTime to)
    {
        var f = from.Date;
        var t = to.Date < from ? f : to.Date;
        return _accounting.BalanceSeries(f, t);
    }

    // ---- Vista: Facturas (movimientos) -------------------------
    private (string[] h, float[] w, List<string[]> r) Movimientos(
        List<(Venta venta, VentaDetalle linea)> ventas,
        List<(Pago pago, ServiceOrder orden)> cobros)
    {
        string[] h = { "Fecha", "Código", "Cliente", "Detalle", "Tipo", "Método", "Monto" };
        float[] w = { 50f, 65f, 85f, 140f, 40f, 50f, 55f };

        var rows = new List<(DateTime fecha, string[] cells)>();
        foreach (var g in ventas.GroupBy(x => x.venta.Id))
        {
            var v = g.First().venta;
            // Agrupa las líneas de la misma factura en un detalle "cantidad× nombre, ...".
            var detalle = string.Join(", ", g.Select(x => $"{x.linea.Cantidad}× {x.linea.ProductoNombre}".Trim()));
            // Se recorta a ~70 caracteres para que la columna no desborde la hoja (igual que v2).
            if (detalle.Length > 70) detalle = detalle[..67] + "...";
            rows.Add((v.Fecha, new[] { v.Fecha.ToString("dd/MM/yy HH:mm"), v.VentaNumber, v.ClienteNombre, detalle, "Venta", v.MetodoPago, _money.Fmt(v.Total) }));
        }
        foreach (var (p, o) in cobros)
        {
            // Describe el equipo cobrado: "Marca Modelo (Tipo)"; sin marca/modelo usa solo el tipo.
            var equipo = string.Join(" ", new[] { o.Brand, o.Model }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            if (equipo.Length == 0) equipo = o.DeviceType;
            else if (!string.IsNullOrWhiteSpace(o.DeviceType)) equipo += $" ({o.DeviceType})";
            var detalle = p.Concepto.Length > 0 ? $"{equipo} · {p.Concepto}" : equipo;
            if (detalle.Length > 70) detalle = detalle[..67] + "...";
            rows.Add((p.Fecha, new[] { p.Fecha.ToString("dd/MM/yy HH:mm"), o.OrderNumber, o.CustomerName, detalle, "Cobro", p.Metodo, _money.Fmt(p.Monto) }));
        }

        // Orden cronológico descendente: lo más reciente aparece primero.
        var r = rows.OrderByDescending(x => x.fecha).Select(x => x.cells).ToList();
        return (h, w, r);
    }

    // ---- Vista: Tipo de ingreso ---------------------------------
    private (string[] h, float[] w, List<string[]> r) PorTipoIngreso(
        List<(Venta venta, VentaDetalle linea)> ventas,
        List<(Pago pago, ServiceOrder orden)> cobros)
    {
        string[] h = { "Tipo de ingreso", "Efectivo", "Nequi", "Otros", "Total" };
        float[] w = { 170f, 80f, 80f, 80f, 85f };

        // Montos de ventas (v) y de cobros (c) agrupados por la categoría del método (Cat).
        decimal Ver(string c) => ventas.GroupBy(x => x.venta.Id).Where(g => Cat(g.First().venta.MetodoPago) == c).Sum(g => g.First().venta.Total);
        decimal Cer(string c) => cobros.Where(x => Cat(x.pago.Metodo) == c).Sum(x => x.pago.Monto);
        var vCat = new Dictionary<string, decimal> { [Efectivo] = Ver(Efectivo), [Nequi] = Ver(Nequi), [Otros] = Ver(Otros) };
        var cCat = new Dictionary<string, decimal> { [Efectivo] = Cer(Efectivo), [Nequi] = Cer(Nequi), [Otros] = Cer(Otros) };

        var r = new List<string[]>
        {
            new[] { "Ventas (accesorios)", F(vCat[Efectivo]), F(vCat[Nequi]), F(vCat[Otros]), F(vCat[Efectivo] + vCat[Nequi] + vCat[Otros]) },
            new[] { "Cobros de mantenimiento", F(cCat[Efectivo]), F(cCat[Nequi]), F(cCat[Otros]), F(cCat[Efectivo] + cCat[Nequi] + cCat[Otros]) },
            new[] { "TOTAL DEL PERIODO", F(vCat[Efectivo] + cCat[Efectivo]), F(vCat[Nequi] + cCat[Nequi]), F(vCat[Otros] + cCat[Otros]), F(vCat[Efectivo] + cCat[Efectivo] + vCat[Nequi] + cCat[Nequi] + vCat[Otros] + cCat[Otros]) }
        };
        return (h, w, r);
    }

    // ---- Vista: Por método de pago ------------------------------
    private (string[] h, float[] w, List<string[]> r) PorMetodo(
        List<(Venta venta, VentaDetalle linea)> ventas,
        List<(Pago pago, ServiceOrder orden)> cobros)
    {
        string[] h = { "Método de pago", "Ventas", "Cobros", "Total" };
        float[] w = { 140f, 105f, 140f, 110f };

        var filas = new List<(string m, decimal v, decimal c)>();
        foreach (var metodo in Metodos)
        {
            var v = ventas.GroupBy(x => x.venta.Id).Where(g => Cat(g.First().venta.MetodoPago) == metodo).Sum(g => g.First().venta.Total);
            var c = cobros.Where(x => Cat(x.pago.Metodo) == metodo).Sum(x => x.pago.Monto);
            // Métodos sin movimiento se omiten, salvo "Otros" que se muestra siempre como fila de referencia.
            if (v == 0 && c == 0 && metodo != Otros) continue;
            filas.Add((metodo, v, c));
        }
        var totV = filas.Sum(x => x.v);
        var totC = filas.Sum(x => x.c);

        var r = filas.Select(x => new[] { x.m, F(x.v), F(x.c), F(x.v + x.c) }).ToList();
        r.Add(new[] { "TOTAL", F(totV), F(totC), F(totV + totC) });
        return (h, w, r);
    }

    // ---- Vista: Por producto (solo accesorios) -------------------
    private (string[] h, float[] w, List<string[]> r) PorProducto(
        List<(Venta venta, VentaDetalle linea)> ventas)
    {
        string[] h = { "Producto", "Cantidad", "Monto" };
        float[] w = { 260f, 90f, 120f };

        // Suma por producto en memoria (una línea por venta), ordenada por monto de mayor a menor.
        var byProd = ventas.GroupBy(x => x.linea.ProductoNombre)
            .Select(g => new { Nombre = string.IsNullOrWhiteSpace(g.Key) ? "(sin nombre)" : g.Key, Cant = g.Sum(x => x.linea.Cantidad), Monto = g.Sum(x => x.linea.PrecioUnitario * x.linea.Cantidad) })
            .OrderByDescending(x => x.Monto).ToList();

        var r = byProd.Select(p => new[] { p.Nombre.Length > 60 ? p.Nombre[..57] + "..." : p.Nombre, p.Cant.ToString(), F(p.Monto) }).ToList();
        return (h, w, r);
    }

    // ---- Vista: Por cliente --------------------------------------
    private (string[] h, float[] w, List<string[]> r) PorCliente(
        List<(Venta venta, VentaDetalle linea)> ventas,
        List<(Pago pago, ServiceOrder orden)> cobros)
    {
        string[] h = { "Cliente", "Ventas", "Cobros", "Total" };
        float[] w = { 180f, 115f, 120f, 80f };

        // Combina ambos universos: la misma persona puede tener ventas Y cobros que deben sumarse.
        var ventasCliente = ventas.GroupBy(x => x.venta.Id)
            .Select(g => (nom: string.IsNullOrWhiteSpace(g.First().venta.ClienteNombre) ? "(sin nombre)" : g.First().venta.ClienteNombre, tot: g.First().venta.Total))
            .GroupBy(x => x.nom).ToDictionary(g => g.Key, g => g.Sum(x => x.tot));
        var cobrosCliente = cobros.GroupBy(x => x.orden.CustomerName)
            .ToDictionary(g => string.IsNullOrWhiteSpace(g.Key) ? "(sin nombre)" : g.Key, g => g.Sum(x => x.pago.Monto));

        // Los clientes de un solo lado se muestran igual: GetValueOrDefault aporta el cero del otro lado.
        var clientes = ventasCliente.Keys.Concat(cobrosCliente.Keys).Distinct()
            .Select(c => (nom: c, v: ventasCliente.GetValueOrDefault(c), cb: cobrosCliente.GetValueOrDefault(c)))
            .OrderByDescending(x => x.v + x.cb).ToList();

        var r = clientes.Select(x => new[] { x.nom.Length > 60 ? x.nom[..57] + "..." : x.nom, F(x.v), F(x.cb), F(x.v + x.cb) }).ToList();
        return (h, w, r);
    }

    // ============================================================
    // Helpers
    // ============================================================
    // Normaliza el texto del método a las tres categorías del cierre: detecta por substring y tolera vacíos.
    private static string Cat(string m)
    {
        var s = (m ?? "").Trim();
        if (s.Length == 0) return Otros;
        var l = s.ToLower();
        if (l.Contains("efectivo")) return Efectivo;
        if (l.Contains("nequi")) return Nequi;
        return Otros;
    }

    // La exportación reusa las filas visibles (copiadas, no por referencia) y anexa el resumen al final.
    private static IReadOnlyList<string[]> BuildExport(int cols, List<string[]> rows, ClosingTotals totals)
    {
        var list = rows.Select(x => (string[])x.Clone()).ToList();
        list.Add(SummaryRow(cols, "FACTURAS POR ACCESORIOS", totals.FacturasAcc.ToString()));
        list.Add(SummaryRow(cols, "FACTURAS POR MANTENIMIENTO", totals.FacturasMant.ToString()));
        list.Add(SummaryRow(cols, "TOTAL DE FACTURAS", totals.Facturas.ToString()));
        list.Add(SummaryRow(cols, "TOTAL INGRESOS DEL PERIODO", totals.Total.ToString("N0")));
        return list;
    }

    // Fila de total: etiqueta en la primera columna y cifra alineada a la derecha (la última).
    private static string[] SummaryRow(int cols, string label, string value)
    {
        var a = new string[cols];
        Array.Fill(a, "");
        a[0] = label;
        if (cols > 1) a[^1] = value;
        return a;
    }

    private string F(decimal v) => _money.Fmt(v);
}