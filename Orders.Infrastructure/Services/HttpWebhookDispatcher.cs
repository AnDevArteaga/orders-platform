using System.Text;
using System.Text.Json;
using Orders.Application.Abstractions;
using Orders.Application.Security;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Services;

public sealed class HttpWebhookDispatcher(IPlatformStore store, ISecretProtector protector, HttpClient http) : IWebhookDispatcher
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task DispatchAsync(Guid merchantId, Guid orderId, string type, object data, CancellationToken cancellationToken)
    {
        var endpoint = await store.FindWebhookAsync(merchantId, cancellationToken);
        var payload = JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            type,
            created_at = DateTime.UtcNow,
            data
        }, Json);

        var delivery = new WebhookDelivery(merchantId, orderId, type, payload);
        await store.AddDeliveryAsync(delivery, cancellationToken);

        if (endpoint is null)
        {
            await store.SaveAsync(cancellationToken);
            return;
        }

        var secret = protector.Unprotect(endpoint.SecretProtected);
        for (var attempt = 0; attempt < 3 && !delivery.Delivered; attempt++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("X-Orders-Signature", WebhookSigner.Header(secret, timestamp, payload));
            request.Headers.TryAddWithoutValidation("X-Orders-Event", type);

            try
            {
                var response = await http.SendAsync(request, cancellationToken);
                delivery.RecordAttempt((int)response.StatusCode, response.IsSuccessStatusCode);
            }
            catch (HttpRequestException)
            {
                delivery.RecordAttempt(0, false);
            }
        }

        await store.SaveAsync(cancellationToken);
    }
}
