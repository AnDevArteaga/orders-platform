using Orders.Domain.Exceptions;

namespace Orders.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El email no puede estar vacío.");

        if (!value.Contains("@"))
            throw new DomainException("El formato del email es inválido.");

        Value = value;
    }

   public override string ToString() => Value;
}