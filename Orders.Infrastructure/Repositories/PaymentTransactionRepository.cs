using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces.Repositories;
using Orders.Domain.Entities;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly OrdersDbContext _context;

    public PaymentTransactionRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentTransaction?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);
    }

    public async Task AddAsync(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Update(transaction);
        await _context.SaveChangesAsync();
    }
}

