using Orders.Domain.Common;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public class Merchant : AggregateRoot
{
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";

    private Merchant() { }

    public Merchant(string name, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del comercio no puede estar vacío.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("El email del comercio no es válido.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("La contraseña no puede estar vacía.");

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }
}
