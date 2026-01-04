using Orders.Domain.Common;

namespace Orders.Domain.Entities;

public class PaymentTransaction : AggregateRoot
{
    // Usamos default! para decirle al compilador que EF se encargará de esto
    // o que no será nulo en la práctica.
    public string TransactionId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string PaymentMethod { get; private set; } = default!;

    // El constructor privado para EF suele causar el warning. 
    // Al inicializar arriba con default!, el warning desaparece.
    private PaymentTransaction() { }

    public PaymentTransaction(string transactionId, decimal amount, string currency, string paymentMethod)
    {
        // Generas el ID (podría ser un Guid o el que te devuelva la pasarela)
        TransactionId = transactionId;
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        Status = "PENDING";
    }

    public void MarkAsCompleted()
    {
        Status = "COMPLETED";
    }

    public void MarkAsFailed()
    {
        Status = "FAILED";
    }
}