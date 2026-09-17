// DomainException.cs — Excepciones de reglas de negocio: validación, entidades, stock y saldos.

namespace FULLTECHNOLOGY.Domain.Exceptions;

/// <summary>Base de todo error de negocio; las capas superiores la traducen a mensajes de usuario.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Datos de entrada inválidos o regla de negocio rota (campos obligatorios, montos, estados).</summary>
public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>La entidad pedida no existe; evita errores de SQL difusos mostrando un mensaje claro.</summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string message) : base(message) { }
}

/// <summary>Falta stock para la venta; conserva cantidades para que la UI sugiera el inventario real.</summary>
public class InsufficientStockException : DomainException
{
    public string ProductName { get; }
    public int Available { get; }
    public int Requested { get; }

    public InsufficientStockException(string productName, int available, int requested)
        : base($"Stock insuficiente para '{productName}': hay {available} y se solicitan {requested}.")
    {
        ProductName = productName;
        Available = available;
        Requested = requested;
    }
}

/// <summary>El cobro supera el saldo pendiente; Balance alimenta el mensaje de error al cajero.</summary>
public class PaymentExceedsBalanceException : DomainException
{
    public decimal Balance { get; }

    public PaymentExceedsBalanceException(decimal balance)
        : base($"El monto no puede superar el saldo pendiente ({balance:N0}).")
    {
        Balance = balance;
    }
}