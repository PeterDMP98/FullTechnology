// DomainTests.cs — Pruebas unitarias del dominio: totales, validación y catálogos.

using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Enums;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Domain.ValueObjects;

namespace FULLTECHNOLOGY.Domain.Tests;

/// <summary>Pruebas de cálculo de Total/Balance y de la validación de órdenes de servicio.</summary>
public class ServiceOrderTests
{
    [Fact]
    public void Total_SumaCostosYDescuenta()
    {
        var o = new ServiceOrder { DiagnosisCost = 20000, PartsCost = 50000, LaborCost = 30000, Discount = 10000 };
        Assert.Equal(90000, o.Total);
    }

    [Fact]
    public void Total_NuncaNegativo()
    {
        var o = new ServiceOrder { DiagnosisCost = 1000, PartsCost = 500, LaborCost = 500, Discount = 5000 };
        // Descuento mayor que los costos: el total se clampa a 0.
        Assert.Equal(0, o.Total);
    }

    [Fact]
    public void Balance_EsTotalMenosDeposit()
    {
        var o = new ServiceOrder { PartsCost = 80000, Deposit = 30000 };
        Assert.Equal(50000, o.Balance);
    }

    [Fact]
    public void Balance_NuncaNegativo()
    {
        var o = new ServiceOrder { PartsCost = 10000, Deposit = 50000 };
        // Abono mayor que el total: el saldo se clampa a 0.
        Assert.Equal(0, o.Balance);
    }

    private static ServiceOrder Valid() => new()
    {
        CustomerName = "Ana",
        CustomerPhone = "3001112233",
        DeviceType = "Teléfono",
        Brand = "Samsung"
    };

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_SinNombre_Lanza(string nombre)
    {
        var o = Valid();
        o.CustomerName = nombre;
        Assert.Throws<ValidationException>(o.Validate);
    }

    [Fact]
    public void Validate_SinCelULARNiDocumento_Lanza()
    {
        var o = Valid();
        o.CustomerPhone = "";
        o.CustomerDoc = "";
        Assert.Throws<ValidationException>(o.Validate);
    }

    [Fact]
    public void Validate_SinMarca_Lanza()
    {
        var o = Valid();
        o.Brand = "";
        Assert.Throws<ValidationException>(o.Validate);
    }

    [Fact]
    public void Validate_OrdenValida_NoLanza()
    {
        var o = Valid();
        o.Validate();
    }
}

/// <summary>Pruebas de control de stock y validación de productos.</summary>
public class ProductoTests
{
    [Fact]
    public void EnsureStock_CantidadNegativa_Lanza()
    {
        var p = new Producto { Nombre = "Forro", Stock = 5, Tipo = ProductTypes.Accesorio };
        Assert.Throws<ValidationException>(() => p.EnsureStock(-1));
    }

    [Fact]
    public void EnsureStock_StockInsuficiente_LanzaConDetalle()
    {
        var p = new Producto { Nombre = "Forro", Stock = 2, Tipo = ProductTypes.Accesorio };
        var ex = Assert.Throws<InsufficientStockException>(() => p.EnsureStock(5));
        Assert.Equal(2, ex.Available);
        Assert.Equal(5, ex.Requested);
        Assert.Equal("Forro", ex.ProductName);
    }

    [Fact]
    public void EnsureStock_StockSuficiente_NoLanza()
    {
        var p = new Producto { Nombre = "Forro", Stock = 3, Tipo = ProductTypes.Accesorio };
        p.EnsureStock(3);
    }

    [Fact]
    public void Validate_StockNegativo_Lanza()
    {
        var p = new Producto { Nombre = "X", Stock = -1, Tipo = ProductTypes.Accesorio };
        Assert.Throws<ValidationException>(p.Validate);
    }

    [Fact]
    public void Validate_TipoInvalido_Lanza()
    {
        var p = new Producto { Nombre = "X", Stock = 0, Tipo = "Otro" };
        Assert.Throws<ValidationException>(p.Validate);
    }

    [Fact]
    public void Validate_NombreVacio_Lanza()
    {
        var p = new Producto { Nombre = " ", Stock = 0, Tipo = ProductTypes.Accesorio };
        Assert.Throws<ValidationException>(p.Validate);
    }
}

/// <summary>Pruebas del formateo de dinero según la moneda (COP, USD, EUR) y sus símbolos.</summary>
public class MoneyTests
{
    [Fact]
    public void Fmt_COP_UsaSeparadorDeMilesYCOP()
    {
        Assert.Equal("1.234.567 COP", Money.Fmt(1234567, "COP"));
    }

    [Fact]
    public void Fmt_USD_UsaSimboloDolar()
    {
        Assert.Equal("$12.50", Money.Fmt(12.5m, "USD"));
    }

    [Fact]
    public void Fmt_EUR_UsaSimboloPospuesto()
    {
        Assert.Equal("12,50 €", Money.Fmt(12.5m, "EUR"));
    }

    [Fact]
    public void Fmt_MonedaDesconocida_TrataComoCOP()
    {
        Assert.EndsWith("COP", Money.Fmt(100, "XXX"));
    }

    [Fact]
    public void Fmt_ConMonedaTxtMinuscula_Normaliza()
    {
        Assert.Equal("$1.00", Money.Fmt(1, "usd"));
    }

    [Fact]
    public void Symbol_ConoceTodasLasMonedas()
    {
        Assert.Equal("$", new Money(0, "USD").Symbol);
        Assert.Equal("€", new Money(0, "EUR").Symbol);
        Assert.Equal("$", new Money(0, "COP").Symbol);
    }
}

/// <summary>Pruebas de validación de clientes (comprador/proveedor).</summary>
public class ClienteTests
{
    [Fact]
    public void Validate_NombreVacio_Lanza()
    {
        Assert.Throws<ValidationException>(() => new Cliente { Nombre = " " }.Validate());
    }

    [Fact]
    public void Validate_TipoInvalido_Lanza()
    {
        Assert.Throws<ValidationException>(() => new Cliente { Nombre = "Ana", Tipo = "X" }.Validate());
    }

    [Fact]
    public void Validate_CompradorValido_NoLanza()
    {
        new Cliente { Nombre = "Ana", Tipo = CustomerTypes.Comprador }.Validate();
        new Cliente { Nombre = "Ana", Tipo = CustomerTypes.Proveedor }.Validate();
    }
}

/// <summary>Pruebas del mapeo de catálogos persistidos (estados de reparación y medios de pago).</summary>
public class CatalogsTests
{
    [Theory]
    [InlineData("Recibido", RepairStatus.Recibido)]
    [InlineData("En diagnóstico", RepairStatus.EnDiagnostico)]
    [InlineData("En reparación", RepairStatus.EnReparacion)]
    [InlineData("Reparado", RepairStatus.Reparado)]
    [InlineData("No reparado", RepairStatus.NoReparado)]
    [InlineData("Listo para entregar", RepairStatus.ListoParaEntregar)]
    [InlineData("Entregado", RepairStatus.Entregado)]
    public void ToEnum_MapeaLosTextosPersistidos(string text, RepairStatus expected)
    {
        Assert.Equal(expected, RepairStatuses.ToEnum(text));
    }

    [Fact]
    public void ToEnum_ValorDesconocido_Recibido()
    {
        Assert.Equal(RepairStatus.Recibido, RepairStatuses.ToEnum("???"));
        // Valores desconocidos o nulos caen al estado por defecto (Recibido).
        Assert.Equal(RepairStatus.Recibido, RepairStatuses.ToEnum(null));
    }

    [Fact]
    public void RoundTrip_ToDbText_Consistente()
    {
        foreach (var s in RepairStatuses.All)
            Assert.Equal(s, RepairStatuses.ToEnum(RepairStatuses.ToDbText(s)));
    }

    [Fact]
    public void Terminados_SoloEntregadoYNoReparado()
    {
        Assert.True(RepairStatuses.IsFinished(RepairStatuses.Entregado));
        Assert.True(RepairStatuses.IsFinished(RepairStatuses.NoReparado));
        Assert.False(RepairStatuses.IsFinished(RepairStatuses.Recibido));
    }

    [Fact]
    public void PaymentMethods_IncluyeLosMediosDeV2()
    {
        Assert.Contains(PaymentMethods.Tarjeta, PaymentMethods.All);
        Assert.Contains(PaymentMethods.Daviplata, PaymentMethods.All);
        Assert.Contains(PaymentMethods.Otro, PaymentMethods.All);
        Assert.Contains(PaymentMethods.Efectivo, PaymentMethods.All);
    }
}