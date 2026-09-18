// InventarioImport.cs — Lectura de inventario desde archivos Excel (.xlsx) y CSV para el botón "Importar" del módulo de inventario.
using System.Globalization;
using ClosedXML.Excel;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;

namespace FULLTECHNOLOGY.Infrastructure.Reporting;

// ============================================================
// IMPORT - Lee un listado de productos desde un .xlsx exportado
// con ExcelReport.WriteTable (fila 3 = encabezados, datos desde
// la fila 4) o desde un .csv sencillo. Complementa la exportación
// PDF/Excel del inventario y permite cargar/actualizar stock.
// ============================================================
public static class InventarioImport
{
    /// <summary>
    /// Lee un libro .xlsx con el layout de ExcelReport.WriteTable: fila 1 =
    /// título, fila 2 = sublínea, fila 3 = encabezados y datos desde la fila 4.
    /// Columnas: Código, Nombre, Tipo, Costo, Precio venta, Proveedor, Ingreso,
    /// Garantía, Ubicado, Stock. Devuelve productos sin Id (para alta o actualización).
    /// </summary>
    public static List<Producto> ReadXlsx(string path)
    {
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheets.First();
        var result = new List<Producto>();
        var last = ws.LastRowUsed()?.RowNumber() ?? 3;
        for (int r = 4; r <= last; r++)
        {
            string Cell(int n) => ws.Cell(r, n).GetString().Trim();
            var codigo = Cell(1);
            var nombre = Cell(2);
            if (nombre.Length == 0 && codigo.Length == 0) continue;
            result.Add(new Producto
            {
                Codigo = codigo,
                Nombre = nombre,
                Tipo = NormalizarTipo(Cell(3)),
                Costo = Math.Max(0, ParseDec(Cell(4))),
                PrecioVenta = Math.Max(0, ParseDec(Cell(5))),
                Stock = Math.Max(0, (int)ParseDec(Cell(10)))
            });
        }
        return result;
    }

    /// <summary>
    /// Lee un CSV sencillo (separador ';' o ','). Acepta una fila de encabezados
    /// o datos directos en el mismo orden: Código, Nombre, Tipo, Costo, Precio
    /// venta, Proveedor, Ingreso, Garantía, Ubicado, Stock.
    /// </summary>
    public static List<Producto> ReadCsv(string path)
    {
        var result = new List<Producto>();
        var lines = File.ReadAllLines(path);
        var start = lines.Length > 0 && lines[0].Contains("odigo") ? 1 : 0;
        for (int i = start; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = Split(lines[i]);
            if (cells.Count < 5) continue;
            string Cell(int n) => (n < cells.Count ? cells[n] : "").Trim();
            var codigo = Cell(0);
            var nombre = Cell(1);
            if (nombre.Length == 0 && codigo.Length == 0) continue;
            result.Add(new Producto
            {
                Codigo = codigo,
                Nombre = nombre,
                Tipo = NormalizarTipo(Cell(2)),
                Costo = Math.Max(0, ParseDec(Cell(3))),
                PrecioVenta = Math.Max(0, ParseDec(Cell(4))),
                Stock = Math.Max(0, (int)ParseDec(Cell(9)))
            });
        }
        return result;
    }

    // El tipo solo admite los del catálogo; lo que no coincida se deja en Accesorio.
    private static string NormalizarTipo(string tipo) =>
        tipo is ProductTypes.Repuesto or ProductTypes.Accesorio ? tipo : ProductTypes.Accesorio;

    private static List<string> Split(string line) =>
        (line.Contains(';') ? line.Split(';') : line.Split(',')).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

    // Tolerante a separadores: quita "$", agrupa miles y usa '.' o ',' como decimal según el contexto.
    private static decimal ParseDec(string s)
    {
        s = s.Replace("$", "").Replace(" ", "").Trim();
        if (s.Length == 0) return 0m;
        bool hasComma = s.Contains(',');
        bool hasDot = s.Contains('.');
        s = hasComma && hasDot ? s.Replace(".", "").Replace(",", ".") : s.Replace(",", "");
        return decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v) ? v : 0m;
    }
}