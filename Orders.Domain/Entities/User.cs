using Orders.Domain.Common;
using Orders.Domain.Exceptions;
using Orders.Domain.ValueObjects;

namespace Orders.Domain.Entities
{
    public class User: AggregateRoot
    {
        public string Username { get; private set; } = default!;
        public Email Email { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;

        private User() { }

        public User(string username, Email email, string passwordHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash, nameof(passwordHash));
            Username = username;
            Email = email ?? throw new DomainException("Email cannot be null.");
            PasswordHash = passwordHash;
        }
    }
}
