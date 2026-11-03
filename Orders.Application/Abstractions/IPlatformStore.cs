using Orders.Domain.Entities;

namespace Orders.Application.Abstractions;

public interface IPlatformStore
{
    Task SaveAsync(CancellationToken cancellationToken);

    Task AddMerchantAsync(Merchant merchant, CancellationToken cancellationToken);
    Task<Merchant?> FindMerchantByEmailAsync(string email, CancellationToken cancellationToken);
    Task<Merchant?> FindMerchantAsync(Guid id, CancellationToken cancellationToken);

    Task AddApiKeyAsync(ApiKey key, CancellationToken cancellationToken);
    Task<ApiKey?> FindApiKeyByHashAsync(string hash, CancellationToken cancellationToken);
    Task<ApiKey?> FindApiKeyAsync(Guid merchantId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApiKey>> ListApiKeysAsync(Guid merchantId, CancellationToken cancellationToken);

    Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken);
    Task<Customer?> FindCustomerAsync(Guid merchantId, Guid id, CancellationToken cancellationToken);
    Task<Customer?> FindCustomerByEmailAsync(Guid merchantId, string email, CancellationToken cancellationToken);

    Task AddOrderAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> FindOrderAsync(Guid merchantId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListOrdersAsync(Guid merchantId, OrderStatus? status, CancellationToken cancellationToken);
    Task<bool> ReferenceExistsAsync(Guid merchantId, string reference, CancellationToken cancellationToken);

    Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> FindPaymentAsync(Guid id, CancellationToken cancellationToken);
    Task<Payment?> FindPaymentByProviderIdAsync(string providerId, CancellationToken cancellationToken);
    Task<Payment?> FindPendingPaymentAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddWebhookAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken);
    Task<WebhookEndpoint?> FindWebhookAsync(Guid merchantId, CancellationToken cancellationToken);

    Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken);
    Task<IReadOnlyList<WebhookDelivery>> ListDeliveriesAsync(Guid merchantId, CancellationToken cancellationToken);

    Task AddIntegrationAsync(WompiIntegration integration, CancellationToken cancellationToken);
    Task<WompiIntegration?> FindIntegrationAsync(Guid merchantId, CancellationToken cancellationToken);

    Task AddRequestLogAsync(RequestLog log, CancellationToken cancellationToken);
}
