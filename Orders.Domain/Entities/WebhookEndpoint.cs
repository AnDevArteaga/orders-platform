using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public class WebhookEndpoint
{
    public Guid MerchantId { get; private set; }
    public string Url { get; private set; } = "";
    public string SecretProtected { get; private set; } = "";

    private WebhookEndpoint() { }

    public WebhookEndpoint(Guid merchantId, string url, string secretProtected)
    {
        MerchantId = merchantId;
        Replace(url, secretProtected);
    }

    public void Replace(string url, string secretProtected)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp))
            throw new DomainException("La URL del webhook tiene que ser absoluta.");

        Url = url;
        SecretProtected = secretProtected;
    }
}
