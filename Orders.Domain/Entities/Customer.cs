using Orders.Domain.Common;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public class Customer : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public string Email { get; private set; } = "";
    public string Name { get; private set; } = "";

    private Customer() { }

    public Customer(Guid merchantId, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del cliente no puede estar vacío.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("El email del cliente no es válido.");

        MerchantId = merchantId;
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
    }
}
