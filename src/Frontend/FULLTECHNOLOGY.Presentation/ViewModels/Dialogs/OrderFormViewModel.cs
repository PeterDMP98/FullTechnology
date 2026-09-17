// OrderFormViewModel.cs — Formulario de alta/edición de una orden (cliente, dispositivo, diagnóstico, costo): valida, relaciona cliente y guarda.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FULLTECHNOLOGY.Application.Services;
using FULLTECHNOLOGY.Domain;
using FULLTECHNOLOGY.Domain.Entities;
using FULLTECHNOLOGY.Domain.Exceptions;
using FULLTECHNOLOGY.Presentation.Services;
using FULLTECHNOLOGY.Presentation.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace FULLTECHNOLOGY.Presentation.ViewModels.Dialogs;

// ============================================================
// Diálogo "Nuevo ingreso / Editar orden" (4 secciones, OrderForm v2):
// 01 Datos de cliente | 02 Dispositivo | 03 Diagnóstico y reparación
// | 04 Costo y entrega. Total/Saldo se recalculan con reglas de Domain;
// el guardado pasa por OrdersService (validación + abono inicial + reclink).
// ============================================================
public partial class OrderFormViewModel : ViewModelBase
{
    private readonly OrdersService _orders;
    private readonly CustomersService _customers;
    private readonly CurrencyService _currency;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    private ServiceOrder _order = new();
    private bool _isNew;
    private long? _clienteId;

    public string Title { get; private set; } = "";

    // ---- Sección 01: Cliente ----
    [ObservableProperty]
    public partial string Nombre { get; set; } = "";

    [ObservableProperty]
    public partial string Documento { get; set; } = "";

    [ObservableProperty]
    public partial string Celular { get; set; } = "";

    // ---- Sección 02: Dispositivo ----
    public IReadOnlyList<string> TiposEquipo { get; } = new[] { "Teléfono", "Tablet", "Computador", "Smartwatch", "Otro" };

    [ObservableProperty]
    public partial string TipoEquipo { get; set; } = "Teléfono";

    // Marcas sugeridas; si la orden trae una marca personalizada se añaden a la lista.
    public IReadOnlyList<string> Marcas { get; private set; } = DefaultMarcas;

    private static readonly string[] DefaultMarcas =
        { "Samsung", "Apple", "Xiaomi", "Huawei", "Motorola", "LG", "HP", "Lenovo", "Dell", "Asus", "Acer", "Otro" };

    [ObservableProperty]
    public partial string Marca { get; set; } = "";

    [ObservableProperty]
    public partial string Modelo { get; set; } = "";

    [ObservableProperty]
    public partial string SerialImei { get; set; } = "";

    [ObservableProperty]
    public partial string Color { get; set; } = "";

    [ObservableProperty]
    public partial string Accesorios { get; set; } = "";

    // ---- Sección 03: Diagnóstico ----
    public IReadOnlyList<string> Condiciones { get; } = new[] { "Bueno", "Regular", "Dañado", "Con señales de uso", "Líquido/golpes" };

    [ObservableProperty]
    public partial string CondicionFisica { get; set; } = "";

    [ObservableProperty]
    public partial string Diagnostico { get; set; } = "";

    [ObservableProperty]
    public partial string TrabajoRealizado { get; set; } = "";

    [ObservableProperty]
    public partial string Observaciones { get; set; } = "";

    // ---- Sección 04: Costo y entrega ----
    public IReadOnlyList<string> Estados => RepairStatuses.DisplayOrder;

    [ObservableProperty]
    public partial string Estado { get; set; } = RepairStatuses.Recibido;

    // Medios de pago (incluye "Pendiente" como valor inicial).
    public IReadOnlyList<string> MediosPago { get; } = new[]
    {
        PaymentMethods.Pendiente, PaymentMethods.Efectivo, PaymentMethods.Nequi,
        PaymentMethods.Transferencia, PaymentMethods.Tarjeta, PaymentMethods.Daviplata, PaymentMethods.Otro
    };

    [ObservableProperty]
    public partial string FormaPago { get; set; } = PaymentMethods.Pendiente;

    // Costos del formulario (repuesto, mano de obra, abono, descuento y diagnóstico).
    [ObservableProperty]
    public partial decimal Repuesto { get; set; }

    [ObservableProperty]
    public partial decimal ManoObra { get; set; }

    [ObservableProperty]
    public partial decimal Abono { get; set; }

    [ObservableProperty]
    public partial decimal Descuento { get; set; }

    [ObservableProperty]
    public partial decimal CostoDiagnostico { get; set; }

    [ObservableProperty]
    public partial string TotalLabel { get; set; } = "";

    [ObservableProperty]
    public partial string SaldoLabel { get; set; } = "";

    // Cálculo en vivo del total (nunca negativo) y del saldo tras el abono.
    public decimal TotalCalc => Math.Max(0, CostoDiagnostico + Repuesto + ManoObra - Descuento);
    public decimal SaldoCalc => Math.Max(0, TotalCalc - Abono);

    public OrderFormViewModel(
        OrdersService orders,
        CustomersService customers,
        CurrencyService currency,
        IDialogService dialogs,
        IServiceProvider services)
    {
        _orders = orders;
        _customers = customers;
        _currency = currency;
        _dialogs = dialogs;
        _services = services;
    }

    // Rellena el formulario desde una orden existente (edición) o desde cero (alta).
    public void Initialize(ServiceOrder? existing, bool isNew)
    {
        _order = existing ?? new ServiceOrder { ReceivedAt = DateTime.Now };
        _isNew = isNew;
        _clienteId = _order.ClienteId;
        Title = isNew ? "Nuevo ingreso" : $"Orden {_order.OrderNumber}";

        Nombre = _order.CustomerName;
        Documento = _order.CustomerDoc;
        Celular = _order.CustomerPhone;

        TipoEquipo = string.IsNullOrEmpty(_order.DeviceType) ? "Teléfono" : _order.DeviceType;
        // Si el tipo no está en la lista (datos históricos) se usa libre.
        if (!TiposEquipo.Contains(_order.DeviceType) && !string.IsNullOrWhiteSpace(_order.DeviceType))
            TipoEquipo = _order.DeviceType;
        // Marca personalizada: se agrega al combo para poder editarla.
        if (!string.IsNullOrWhiteSpace(_order.Brand) && !DefaultMarcas.Contains(_order.Brand))
            Marcas = new[] { _order.Brand }.Concat(DefaultMarcas).ToArray();
        Marca = _order.Brand;
        Modelo = _order.Model;
        SerialImei = _order.SerialImei;
        Color = _order.Color;
        Accesorios = _order.Accessories;

        CondicionFisica = string.IsNullOrEmpty(_order.PhysicalCondition) ? "Bueno" : _order.PhysicalCondition;
        Diagnostico = _order.Diagnosis;
        TrabajoRealizado = _order.RepairDetails;
        Observaciones = _order.Notes;

        Estado = string.IsNullOrEmpty(_order.Status) ? RepairStatuses.Recibido : _order.Status;
        FormaPago = string.IsNullOrEmpty(_order.PaymentMethod) ? PaymentMethods.Pendiente : _order.PaymentMethod;
        Repuesto = _order.PartsCost;
        ManoObra = _order.LaborCost;
        Abono = _order.Deposit;
        Descuento = _order.Discount;
        CostoDiagnostico = _order.DiagnosisCost;

        RecargarTotales();
    }

    // Cualquier cambio en costos/abonos/descuento recalcula Total y Saldo.
    partial void OnRepuestoChanged(decimal value) => RecargarTotales();
    partial void OnManoObraChanged(decimal value) => RecargarTotales();
    partial void OnAbonoChanged(decimal value) => RecargarTotales();
    partial void OnDescuentoChanged(decimal value) => RecargarTotales();
    partial void OnCostoDiagnosticoChanged(decimal value) => RecargarTotales();

    private void RecargarTotales()
    {
        TotalLabel = $"Total a pagar: {_currency.Fmt(TotalCalc)}";
        SaldoLabel = $"Saldo: {_currency.Fmt(SaldoCalc)}";
    }

    // ---- Acciones del cliente ----
    // Busca un cliente ya registrado y lo aplica al formulario.
    [RelayCommand]
    private async Task BuscarClienteAsync()
    {
        var vm = _services.GetRequiredService<BuscarClienteViewModel>();
        vm.Initialize(CustomerTypes.Comprador);
        var win = new BuscarClienteView { DataContext = vm };
        if (await win.ShowDialog<bool>(DialogService.OwnerWindow!) && vm.Selected is not null)
            AplicarCliente(vm.Selected);
    }

    // Crea un cliente nuevo desde el diálogo del pedido.
    [RelayCommand]
    private async Task CrearClienteAsync()
    {
        var vm = _services.GetRequiredService<ClienteEditViewModel>();
        var win = new ClienteEditView { DataContext = vm };
        if (await win.ShowDialog<bool>(DialogService.OwnerWindow!) && vm.Saved is not null)
        {
            // Se relaciona con el último cliente creado que coincida por documento/celular (v2).
            var cl = _customers.FindComprador(string.IsNullOrEmpty(Celular.Trim()) ? Documento.Trim() : Celular.Trim());
            AplicarCliente(cl ?? vm.Saved);
        }
    }

    // Copia los datos del cliente elegido al formulario.
    private void AplicarCliente(Cliente cl)
    {
        _clienteId = cl.Id;
        Nombre = cl.Nombre;
        Documento = cl.Documento;
        Celular = cl.Celular;
    }

    /// <summary>Valida, relaciona el cliente y guarda (OrderForm v2). true = cerrar con OK.</summary>
    public async Task<bool> SaveAsync()
    {
        // Validaciones obligatorias (sección cliente y dispositivo).
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            await _dialogs.ShowMessageAsync("Dato requerido", "El nombre del cliente es obligatorio.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(Celular) && string.IsNullOrWhiteSpace(Documento))
        {
            await _dialogs.ShowMessageAsync("Dato requerido", "Debe indicar el número de celular/WhatsApp o el documento de identidad del cliente.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(TipoEquipo) || string.IsNullOrWhiteSpace(Marca))
        {
            await _dialogs.ShowMessageAsync("Dato requerido", "El tipo de equipo y la marca son obligatorios.");
            return false;
        }

        // Relación del cliente: si no está en la orden, se busca por documento/celular
        // y, si no existe, se ofrece registrarlo como comprador (mismo flujo v2).
        if (_clienteId is null)
        {
            var doc = Documento.Trim();
            var cel = Celular.Trim();
            if (string.IsNullOrEmpty(doc) && string.IsNullOrEmpty(cel))
            {
                await _dialogs.ShowMessageAsync("Dato requerido", "Debe indicar el documento o el celular del cliente para poder registrarlo y relacionarlo.");
                return false;
            }
            var found = _customers.FindComprador(string.IsNullOrEmpty(cel) ? doc : cel);
            if (found is not null)
            {
                _clienteId = found.Id;
            }
            else if (await _dialogs.ConfirmAsync("Registrar cliente", "El cliente no está registrado. ¿Desea guardarlo como cliente comprador?"))
            {
                var nuevo = new Cliente
                {
                    Tipo = CustomerTypes.Comprador,
                    Nombre = Nombre.Trim(),
                    Documento = doc,
                    Celular = cel
                };
                _clienteId = _customers.Save(nuevo);
            }
        }

        // Se vuelcan todos los campos del formulario a la entidad.
        _order.ClienteId = _clienteId;
        _order.CustomerName = Nombre.Trim();
        _order.CustomerDoc = Documento.Trim();
        _order.CustomerPhone = Celular.Trim();
        _order.DeviceType = TipoEquipo;
        _order.Brand = Marca;
        _order.Model = Modelo;
        _order.SerialImei = SerialImei.Trim();
        _order.Color = Color.Trim();
        _order.Accessories = Accesorios.Trim();
        _order.PhysicalCondition = CondicionFisica;
        _order.Diagnosis = Diagnostico;
        _order.RepairDetails = TrabajoRealizado;
        _order.Notes = Observaciones;
        _order.Status = Estado;
        _order.PaymentMethod = FormaPago;
        _order.PartsCost = Repuesto;
        _order.LaborCost = ManoObra;
        _order.Discount = Descuento;
        _order.DiagnosisCost = CostoDiagnostico;
        _order.Deposit = Abono;

        try
        {
            _orders.SaveOrder(_order);
            return true;
        }
        catch (ValidationException ex)
        {
            await _dialogs.ShowMessageAsync("No se pudo guardar", ex.Message);
            return false;
        }
        catch (DomainException ex)
        {
            await _dialogs.ShowMessageAsync("No se pudo guardar", ex.Message);
            return false;
        }
    }
}