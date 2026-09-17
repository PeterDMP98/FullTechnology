// ServicesTests.cs — Pruebas de los servicios de aplicación (órdenes, pagos, ventas, clientes y otros) con fakes en memoria.

using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Application.Tests.Fakes;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;

namespace FULLTECHNOLOGY.Application.Tests;

/// <summary>Pruebas del servicio de órdenes: registro, validaciones, abono inicial, relación con cliente y edición.</summary>
public class OrdersServiceTests
{
    private readonly FakeOrdersRepository _orders = new();
    private readonly FakePagoRepository _pagos = new();
    private readonly FakeClienteRepository _clientes = new();
    private OrdersService Sut => new(_orders, _pagos, _clientes);

    private static ServiceOrder ValidOrder(string name = "Ana", string phone = "3001112233") => new()
    {
        CustomerName = name,
        CustomerPhone = phone,
        DeviceType = "Teléfono",
        Brand = "Samsung",
        PartsCost = 50000,
        LaborCost = 30000,
        Deposit = 0
    };

    [Fact]
    public void NuevaOrden_Valida_AsignaNumeroRecibidoYFecha()
    {
        var o = Sut.SaveOrder(ValidOrder());

        Assert.True(o.Id > 0);
        Assert.Equal(RepairStatuses.Recibido, o.Status);
        Assert.True(o.ReceivedAt != default);
    }

    [Theory]
    [InlineData("", "3001112233")]
    [InlineData(" ", "3001112233")]
    public void NuevaOrden_SinNombre_LanzaValidation(string nombre, string cel)
    {
        var o = ValidOrder(nombre, cel);
        Assert.Throws<ValidationException>(() => Sut.SaveOrder(o));
    }

    [Fact]
    public void NuevaOrden_SinCelularNiDocumento_LanzaValidation()
    {
        var o = ValidOrder(phone: "");
        Assert.Throws<ValidationException>(() => Sut.SaveOrder(o));
    }

    [Fact]
    public void NuevaOrden_SinTipoEquipo_LanzaValidation()
    {
        var o = ValidOrder();
        o.DeviceType = "";
        Assert.Throws<ValidationException>(() => Sut.SaveOrder(o));
    }

    [Fact]
    public void NuevaOrden_ConAbono_RegistraAbonoInicial()
    {
        var o = ValidOrder();
        o.PartsCost = 80000;
        o.Deposit = 50000;

        Sut.SaveOrder(o);

        var pago = Assert.Single(_pagos.GetPagos(o.Id));
        Assert.Equal("Abono inicial", pago.Concepto);
        Assert.Equal(50000, pago.Monto);
        Assert.Equal(110000 - 50000, pago.SaldoRestante);
    }

    [Fact]
    public void NuevaOrden_ConDocumento_RelacionaClienteCompradorExistente()
    {
        var cl = new Cliente { Tipo = CustomerTypes.Comprador, Nombre = "Ana", Documento = "1234", Celular = "3001112233" };
        _clientes.Items.Add(cl);

        var o = ValidOrder();
        o.CustomerDoc = "1234";
        var saved = Sut.SaveOrder(o);

        Assert.Equal(cl.Id, saved.ClienteId);
    }

    [Fact]
    public void Edicion_RecalculaDepositoDesdePagos()
    {
        var o = Sut.SaveOrder(ValidOrder());
        _pagos.AddPago(new Pago { ServiceOrderId = o.Id, Monto = 20000, Fecha = DateTime.Now });
        o.Deposit = 0;

        Sut.SaveOrder(o);

        // Simula un cobro posterior y una edición: al guardar se recalcula el depósito desde los pagos.
        Assert.Equal(20000, o.Deposit);
    }
}

/// <summary>Pruebas del servicio de cobros: validaciones, cobro total y abono parcial.</summary>
public class PaymentsServiceTests
{
    private readonly FakeOrdersRepository _orders = new();
    private readonly FakePagoRepository _pagos = new();

    private PaymentsService Sut => new(_pagos, _orders);

    private ServiceOrder CreateOrder(decimal total, decimal deposit = 0)
    {
        var o = new ServiceOrder
        {
            CustomerName = "Ana",
            CustomerPhone = "3001112233",
            DeviceType = "Teléfono",
            Brand = "Samsung",
            DiagnosisCost = total,
            Deposit = deposit,
            Status = RepairStatuses.Reparado
        };
        o.Id = _orders.Save(o);
        return o;
    }

    [Fact]
    public void Cobro_SuperaSaldo_LanzaPaymentExceeds()
    {
        var o = CreateOrder(10000);
        Assert.Throws<PaymentExceedsBalanceException>(() => Sut.Charge(o.Id, 15000, "Efectivo"));
    }

    [Fact]
    public void Cobro_MontoCero_NoPermitido()
    {
        var o = CreateOrder(10000);
        Assert.Throws<ValidationException>(() => Sut.Charge(o.Id, 0, "Efectivo"));
    }

    [Fact]
    public void Cobro_Total_MarcaEntregadoYCierraSaldo()
    {
        var o = CreateOrder(10000);

        var r = Sut.Charge(o.Id, 10000, "Efectivo", markDelivered: true);

        Assert.Equal("Cobro total", r.Pago.Concepto);
        Assert.Equal(0, r.NuevoSaldo);
        Assert.True(r.MarcaEntregado);
        Assert.Equal(RepairStatuses.Entregado, _orders.Get(o.Id)!.Status);
        Assert.NotNull(_orders.Get(o.Id)!.DeliveredAt);
    }

    [Fact]
    public void Cobro_Parcial_UsaConceptoAbonoParcial()
    {
        var o = CreateOrder(10000, deposit: 4000);

        var r = Sut.Charge(o.Id, 3000, "Nequi");

        Assert.Equal("Abono parcial", r.Pago.Concepto);
        Assert.Equal(3000, r.NuevoSaldo);
        Assert.False(r.MarcaEntregado);
    }
}

/// <summary>Pruebas del servicio de ventas: carrito vacío, totales con descuento y control de stock.</summary>
public class SalesServiceTests
{
    private readonly FakeVentaRepository _ventas = new();
    private readonly FakeProductoRepository _productos = new();
    private SalesService Sut => new(_ventas, _productos);

    private Producto AddProducto(decimal precio, int stock)
    {
        var p = new Producto { Nombre = "Cargador USB-C", Tipo = ProductTypes.Accesorio, PrecioVenta = precio, Stock = stock };
        _productos.SaveProducto(p);
        return p;
    }

    [Fact]
    public void Venta_CarritoVacio_LanzaValidation()
    {
        Assert.Throws<ValidationException>(() => Sut.CreateSale(new SaleItem[] { }, 0, "Efectivo"));
    }

    [Fact]
    public void Venta_Valida_TotalesYStock()
    {
        var p = AddProducto(20000, 5);

        var venta = Sut.CreateSale(new[] { new SaleItem(p.Id, 2) }, descuento: 5000, metodoPago: "Efectivo");

        Assert.Equal(40000, venta.Subtotal);
        Assert.Equal(5000, venta.Descuento);
        Assert.Equal(35000, venta.Total);
        Assert.False(string.IsNullOrWhiteSpace(venta.VentaNumber));
        var detalle = Assert.Single(_ventas.Detalles, d => d.VentaId == venta.Id);
        Assert.Equal(2, detalle.Cantidad);
        Assert.Equal(20000, detalle.PrecioUnitario);
    }

    [Fact]
    public void Venta_StockInsuficiente_Lanza()
    {
        var p = AddProducto(20000, 1);
        var ex = Assert.Throws<InsufficientStockException>(() =>
            Sut.CreateSale(new[] { new SaleItem(p.Id, 3) }, 0, "Efectivo"));
        Assert.Equal(1, ex.Available);
        Assert.Equal(3, ex.Requested);
    }

    [Fact]
    public void Venta_DescuentoNegativo_LanzaValidation()
    {
        var p = AddProducto(20000, 5);
        Assert.Throws<ValidationException>(() =>
            Sut.CreateSale(new[] { new SaleItem(p.Id, 1) }, -1, "Efectivo"));
    }
}

/// <summary>Pruebas del servicio de clientes: rechaza datos inválidos y guarda clientes válidos.</summary>
public class CustomerServiceTests
{
    [Fact]
    public void Cliente_SinNombre_NoSeGuarda()
    {
        var svc = new CustomersService(new FakeClienteRepository());
        Assert.Throws<ValidationException>(() => svc.Save(new Cliente { Nombre = " ", Tipo = CustomerTypes.Comprador }));
    }

    [Fact]
    public void Cliente_TipoInvalido_NoSeGuarda()
    {
        var svc = new CustomersService(new FakeClienteRepository());
        Assert.Throws<ValidationException>(() => svc.Save(new Cliente { Nombre = "Ana", Tipo = "X" }));
    }

    [Fact]
    public void Cliente_Valido_SeGuarda()
    {
        var repo = new FakeClienteRepository();
        var id = new CustomersService(repo).Save(new Cliente { Nombre = "Ana", Tipo = CustomerTypes.Comprador, Documento = "1234" });
        Assert.True(id > 0);
    }
}

/// <summary>Pruebas del servicio de inventario: validación de productos y asignación de fecha de ingreso.</summary>
public class InventoryServiceTests
{
    [Fact]
    public void Producto_TipoInvalido_NoSeGuarda()
    {
        var svc = new InventoryService(new FakeProductoRepository());
        Assert.Throws<ValidationException>(() => svc.Save(new Producto { Nombre = "X", Tipo = "Otro" }));
    }

    [Fact]
    public void Producto_StockNegativo_NoSeGuarda()
    {
        var svc = new InventoryService(new FakeProductoRepository());
        Assert.Throws<ValidationException>(() => svc.Save(new Producto { Nombre = "X", Tipo = ProductTypes.Accesorio, Stock = -1 }));
    }

    [Fact]
    public void Producto_AsignaFechaIngreso()
    {
        var repo = new FakeProductoRepository();
        var p = new Producto { Nombre = "Cargador", Tipo = ProductTypes.Accesorio };
        var id = new InventoryService(repo).Save(p);
        Assert.True(repo.GetProducto(id)!.FechaIngreso != default);
    }
}

/// <summary>Pruebas del servicio de moneda: formateo según la divisa configurada.</summary>
public class CurrencyServiceTests
{
    [Fact]
    public void Fmt_COP_UsaFormatoCOP()
    {
        var svc = new CurrencyService(new FakeCurrencyStore { Currency = "COP" });
        Assert.EndsWith("COP", svc.Fmt(12345));
    }

    [Fact]
    public void Fmt_USD_UsaSimboloDolar()
    {
        var svc = new CurrencyService(new FakeCurrencyStore { Currency = "USD" });
        Assert.Equal("$12.50", svc.Fmt(12.5m));
    }

    [Fact]
    public void Fmt_EUR_UsaSimboloEuro()
    {
        var svc = new CurrencyService(new FakeCurrencyStore { Currency = "EUR" });
        Assert.Equal("12,50 €", svc.Fmt(12.5m));
    }

    [Fact]
    public void SetCurrency_ReflejaNuevoValor()
    {
        var store = new FakeCurrencyStore { Currency = "COP" };
        var svc = new CurrencyService(store);
        svc.SetCurrency("USD");
        Assert.Equal("USD", svc.Currency);
    }
}

/// <summary>Pruebas de la configuración del negocio: tema claro/oscuro persistido en el almacén.</summary>
public class BusinessSettingsServiceTests
{
    [Fact]
    public void Tema_Default_EsClaro()
    {
        var svc = new BusinessSettingsService(new FakeBusinessInfo(), new FakeSettingsStore(), new CurrencyService(new FakeCurrencyStore()));
        Assert.Equal("claro", svc.Theme);
    }

    [Fact]
    public void Tema_Oscuro_Persiste()
    {
        var store = new FakeSettingsStore();
        var svc = new BusinessSettingsService(new FakeBusinessInfo(), store, new CurrencyService(new FakeCurrencyStore()));
        svc.SetTheme("oscuro");
        Assert.Equal("oscuro", svc.Theme);
        Assert.Equal("oscuro", store.Get("Tema"));
    }

    private class FakeBusinessInfo : FULLTECHNOLOGY.Application.Ports.IBusinessInfoStore
    {
        public string Name = "DECO TECHNOLOGY";
        public string GetBusinessName() => Name;
        public void SetBusinessName(string name) => Name = name;
        public bool PinHabilitado => false;
        public bool EmailDiarioHabilitado => false;
    }
}

/// <summary>Pruebas del servicio contable: cierre de movimientos y filas de exportación.</summary>
public class AccountingServiceTests
{
    [Fact]
    public void Cierre_Movimientos_SinDatos_DaTotalesCero()
    {
        var ventas = new FakeVentaRepository();
        var accounting = new FakeAccountingRepository();
        var svc = new AccountingService(ventas, accounting, new CurrencyService(new FakeCurrencyStore()));

        var result = svc.BuildCierre(DateTime.Today, DateTime.Today, "", "", vista: 0);

        Assert.Equal(0, result.Totals.Ventas);
        Assert.Equal(0, result.Totals.Cobros);
        Assert.Empty(result.Catalogo.Rows);
        // Las filas de export incluyen el resumen de facturas (4 filas)
        Assert.Equal(4, result.ExportRows.Count);
    }
}