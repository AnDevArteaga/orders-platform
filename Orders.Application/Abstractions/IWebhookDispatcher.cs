namespace Orders.Application.Abstractions;

public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid merchantId, Guid orderId, string type, object data, CancellationToken cancellationToken);
}
