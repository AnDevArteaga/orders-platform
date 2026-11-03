using Orders.Domain.Common;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public enum PaymentStatus
{
    Pending,
    Approved,
    Declined
}

public class Payment : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "COP";
    public PaymentStatus Status { get; private set; }
    public ApiEnvironment Environment { get; private set; }
    public string? ProviderId { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? FailureReason { get; private set; }

    private Payment() { }

    public Payment(Guid merchantId, Guid orderId, decimal amount, ApiEnvironment environment)
    {
        if (amount <= 0)
            throw new DomainException("El monto del pago tiene que ser mayor que cero.");

        MerchantId = merchantId;
        OrderId = orderId;
        Amount = amount;
        Environment = environment;
        Status = PaymentStatus.Pending;
    }

    public void AttachCheckout(string url, string providerId)
    {
        CheckoutUrl = url;
        ProviderId = providerId;
    }

    public void Approve()
    {
        if (Status != PaymentStatus.Pending)
            throw new ConflictException("Este pago ya tiene un resultado.");
        Status = PaymentStatus.Approved;
        FailureReason = null;
    }

    public void Decline(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new ConflictException("Este pago ya tiene un resultado.");
        Status = PaymentStatus.Declined;
        FailureReason = reason;
    }
}
