using Orders.Domain.Common;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public enum OrderStatus
{
    Draft,
    PendingPayment,
    Paid,
    PaymentFailed,
    Fulfilled,
    Cancelled
}

public class OrderLine
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Name { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitAmount { get; private set; }

    private OrderLine() { }

    public OrderLine(Guid orderId, string name, int quantity, decimal unitAmount)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("La línea del pedido necesita un nombre.");
        if (quantity <= 0)
            throw new DomainException("La cantidad tiene que ser mayor que cero.");
        if (unitAmount <= 0 || unitAmount != decimal.Truncate(unitAmount))
            throw new DomainException("El precio tiene que ser un entero en pesos, mayor que cero.");

        Id = Guid.NewGuid();
        OrderId = orderId;
        Name = name.Trim();
        Quantity = quantity;
        UnitAmount = unitAmount;
    }

    public decimal LineTotal => UnitAmount * Quantity;
}

public class OrderStatusChange : BaseEntity
{
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }

    private OrderStatusChange() { }

    public OrderStatusChange(Guid orderId, OrderStatus status)
    {
        OrderId = orderId;
        Status = status;
    }
}

public class Order : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? Reference { get; private set; }
    public string Currency { get; private set; } = "COP";
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<OrderLine> _lines = [];
    private readonly List<OrderStatusChange> _history = [];

    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public IReadOnlyCollection<OrderStatusChange> History => _history;

    private Order() { }

    public static Order Create(
        Guid merchantId,
        Guid customerId,
        string? reference,
        IReadOnlyList<(string Name, int Quantity, decimal UnitAmount)> lines)
    {
        if (lines.Count == 0)
            throw new DomainException("El pedido necesita al menos una línea.");

        var order = new Order
        {
            MerchantId = merchantId,
            CustomerId = customerId,
            Currency = "COP",
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var line in lines)
            order._lines.Add(new OrderLine(order.Id, line.Name, line.Quantity, line.UnitAmount));

        order.Total = order._lines.Sum(line => line.LineTotal);
        order.Transition(OrderStatus.Draft);
        return order;
    }

    public void BeginPayment()
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.PaymentFailed))
            throw new ConflictException("Este pedido no admite un nuevo cobro.");
        Transition(OrderStatus.PendingPayment);
    }

    public void MarkPaid(decimal amount)
    {
        if (Status != OrderStatus.PendingPayment)
            throw new ConflictException("El pedido no está esperando un pago.");
        if (amount != Total)
            throw new DomainException("El monto pagado no coincide con el total del pedido.");
        Transition(OrderStatus.Paid);
    }

    public void MarkPaymentFailed()
    {
        if (Status != OrderStatus.PendingPayment)
            throw new ConflictException("El pedido no está esperando un pago.");
        Transition(OrderStatus.PaymentFailed);
    }

    public void Fulfill()
    {
        if (Status != OrderStatus.Paid)
            throw new ConflictException("Solo se despacha un pedido pagado.");
        Transition(OrderStatus.Fulfilled);
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Fulfilled or OrderStatus.Cancelled)
            throw new ConflictException("Este pedido ya no se puede cancelar.");
        Transition(OrderStatus.Cancelled);
    }

    private void Transition(OrderStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
        _history.Add(new OrderStatusChange(Id, status));
    }
}
