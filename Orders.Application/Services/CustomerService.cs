using Orders.Application.Abstractions;
using Orders.Domain.Entities;

namespace Orders.Application.Services;

public sealed class CustomerService(IPlatformStore store)
{
    public async Task<(Customer Customer, bool Created)> SaveAsync(
        Guid merchantId,
        string name,
        string email,
        CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var existing = await store.FindCustomerByEmailAsync(merchantId, normalized, cancellationToken);
        if (existing is not null)
            return (existing, false);

        var customer = new Customer(merchantId, name, normalized);
        await store.AddCustomerAsync(customer, cancellationToken);
        await store.SaveAsync(cancellationToken);
        return (customer, true);
    }

    public async Task<Customer> GetAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        await store.FindCustomerAsync(merchantId, id, cancellationToken)
        ?? throw new Domain.Exceptions.NotFoundException("No existe ese cliente.");
}
