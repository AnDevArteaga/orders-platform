using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public class WompiIntegration
{
    public Guid MerchantId { get; private set; }
    public string PublicKeyProtected { get; private set; } = "";
    public string PrivateKeyProtected { get; private set; } = "";
    public string EventsSecretProtected { get; private set; } = "";
    public ApiEnvironment Target { get; private set; }
    public bool Activated { get; private set; }

    private WompiIntegration() { }

    public WompiIntegration(Guid merchantId)
    {
        MerchantId = merchantId;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKeyProtected) &&
        !string.IsNullOrWhiteSpace(PrivateKeyProtected) &&
        !string.IsNullOrWhiteSpace(EventsSecretProtected);

    public void Configure(string publicKey, string privateKey, string eventsSecret, ApiEnvironment target)
    {
        PublicKeyProtected = publicKey;
        PrivateKeyProtected = privateKey;
        EventsSecretProtected = eventsSecret;
        Target = target;
        Activated = false;
    }

    public void Activate()
    {
        if (!IsConfigured)
            throw new DomainException("Primero hay que guardar las credenciales de Wompi.");
        Activated = true;
    }
}
