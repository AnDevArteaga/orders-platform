using Orders.Domain.Common;

namespace Orders.Domain.Entities;

public enum ApiEnvironment
{
    Sandbox,
    Production
}

public class ApiKey : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public string Name { get; private set; } = "";
    public string Prefix { get; private set; } = "";
    public string Hash { get; private set; } = "";
    public ApiEnvironment Environment { get; private set; }
    public bool Revoked { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private ApiKey() { }

    public ApiKey(Guid merchantId, string name, string prefix, string hash, ApiEnvironment environment)
    {
        MerchantId = merchantId;
        Name = name.Trim();
        Prefix = prefix;
        Hash = hash;
        Environment = environment;
    }

    public void Revoke() => Revoked = true;

    public void Touch(DateTime utc) => LastUsedAt = utc;
}
