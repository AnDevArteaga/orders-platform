using Orders.Application.Abstractions;
using Orders.Application.Security;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Application.Services;

public sealed class AccountService(IPlatformStore store)
{
    public async Task<Guid> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new DomainException("La contraseña tiene que tener al menos 8 caracteres.");

        var normalized = email.Trim().ToLowerInvariant();
        var existing = await store.FindMerchantByEmailAsync(normalized, cancellationToken);
        if (existing is not null)
            throw new ConflictException("Ya existe un comercio con ese email.");

        var merchant = new Merchant(name, normalized, BCrypt.Net.BCrypt.HashPassword(password));
        await store.AddMerchantAsync(merchant, cancellationToken);
        await store.SaveAsync(cancellationToken);
        return merchant.Id;
    }

    public async Task<Merchant> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var merchant = await store.FindMerchantByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (merchant is null || !BCrypt.Net.BCrypt.Verify(password, merchant.PasswordHash))
            throw new ForbiddenException("Email o contraseña incorrectos.");
        return merchant;
    }

    public async Task<IssuedKey> IssueKeyAsync(Guid merchantId, string name, ApiEnvironment environment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("La llave necesita un nombre.");

        var (token, prefix, hash) = ApiKeys.Issue(environment);
        var key = new ApiKey(merchantId, name, prefix, hash, environment);
        await store.AddApiKeyAsync(key, cancellationToken);
        await store.SaveAsync(cancellationToken);
        return new IssuedKey(key.Id, key.Name, key.Prefix, key.Environment, token);
    }

    public Task<IReadOnlyList<ApiKey>> ListKeysAsync(Guid merchantId, CancellationToken cancellationToken) =>
        store.ListApiKeysAsync(merchantId, cancellationToken);

    public async Task RevokeKeyAsync(Guid merchantId, Guid keyId, CancellationToken cancellationToken)
    {
        var key = await store.FindApiKeyAsync(merchantId, keyId, cancellationToken)
            ?? throw new NotFoundException("No existe esa API key.");
        key.Revoke();
        await store.SaveAsync(cancellationToken);
    }
}

public sealed record IssuedKey(Guid Id, string Name, string Prefix, ApiEnvironment Environment, string Token);
