using Orders.Domain.Entities;

namespace Orders.Application.Interfaces.Repositories;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByTransactionIdAsync(string transactionId);
    Task AddAsync(PaymentTransaction transaction);
    Task UpdateAsync(PaymentTransaction transaction);
}

