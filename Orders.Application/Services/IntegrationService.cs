using Orders.Application.Abstractions;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Application.Services;

public sealed class IntegrationService(IPlatformStore store, ISecretProtector protector)
{
    public async Task SaveWompiAsync(
        Guid merchantId,
        string publicKey,
        string privateKey,
        string eventsSecret,
        ApiEnvironment target,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicKey) ||
            string.IsNullOrWhiteSpace(privateKey) ||
            string.IsNullOrWhiteSpace(eventsSecret))
            throw new DomainException("Faltan credenciales de Wompi.");

        var integration = await store.FindIntegrationAsync(merchantId, cancellationToken);
        if (integration is null)
        {
            integration = new WompiIntegration(merchantId);
            await store.AddIntegrationAsync(integration, cancellationToken);
        }

        integration.Configure(
            protector.Protect(publicKey.Trim()),
            protector.Protect(privateKey.Trim()),
            protector.Protect(eventsSecret.Trim()),
            target);
        await store.SaveAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        var integration = await store.FindIntegrationAsync(merchantId, cancellationToken)
            ?? throw new DomainException("Primero hay que guardar las credenciales de Wompi.");
        integration.Activate();
        await store.SaveAsync(cancellationToken);
    }

    public async Task<WompiIntegration?> GetAsync(Guid merchantId, CancellationToken cancellationToken) =>
        await store.FindIntegrationAsync(merchantId, cancellationToken);

    public async Task<(WebhookEndpoint Endpoint, string Secret)> SaveWebhookAsync(
        Guid merchantId,
        string url,
        CancellationToken cancellationToken)
    {
        var secret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var endpoint = await store.FindWebhookAsync(merchantId, cancellationToken);
        if (endpoint is null)
        {
            endpoint = new WebhookEndpoint(merchantId, url, protector.Protect(secret));
            await store.AddWebhookAsync(endpoint, cancellationToken);
        }
        else
        {
            endpoint.Replace(url, protector.Protect(secret));
        }

        await store.SaveAsync(cancellationToken);
        return (endpoint, secret);
    }

    public Task<WebhookEndpoint?> GetWebhookAsync(Guid merchantId, CancellationToken cancellationToken) =>
        store.FindWebhookAsync(merchantId, cancellationToken);

    public Task<IReadOnlyList<WebhookDelivery>> ListEventsAsync(Guid merchantId, CancellationToken cancellationToken) =>
        store.ListDeliveriesAsync(merchantId, cancellationToken);
}
