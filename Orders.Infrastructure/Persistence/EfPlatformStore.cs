using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence;

public sealed class EfPlatformStore(OrdersDbContext db) : IPlatformStore
{
    public Task SaveAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task AddMerchantAsync(Merchant merchant, CancellationToken cancellationToken) =>
        db.Merchants.AddAsync(merchant, cancellationToken).AsTask();

    public Task<Merchant?> FindMerchantByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Merchants.FirstOrDefaultAsync(merchant => merchant.Email == email, cancellationToken);

    public Task<Merchant?> FindMerchantAsync(Guid id, CancellationToken cancellationToken) =>
        db.Merchants.FirstOrDefaultAsync(merchant => merchant.Id == id, cancellationToken);

    public Task AddApiKeyAsync(ApiKey key, CancellationToken cancellationToken) =>
        db.ApiKeys.AddAsync(key, cancellationToken).AsTask();

    public Task<ApiKey?> FindApiKeyByHashAsync(string hash, CancellationToken cancellationToken) =>
        db.ApiKeys.FirstOrDefaultAsync(key => key.Hash == hash, cancellationToken);

    public Task<ApiKey?> FindApiKeyAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        db.ApiKeys.FirstOrDefaultAsync(key => key.MerchantId == merchantId && key.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ApiKey>> ListApiKeysAsync(Guid merchantId, CancellationToken cancellationToken) =>
        await db.ApiKeys.Where(key => key.MerchantId == merchantId)
            .OrderByDescending(key => key.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken) =>
        db.Customers.AddAsync(customer, cancellationToken).AsTask();

    public Task<Customer?> FindCustomerAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        db.Customers.FirstOrDefaultAsync(customer => customer.MerchantId == merchantId && customer.Id == id, cancellationToken);

    public Task<Customer?> FindCustomerByEmailAsync(Guid merchantId, string email, CancellationToken cancellationToken) =>
        db.Customers.FirstOrDefaultAsync(customer => customer.MerchantId == merchantId && customer.Email == email, cancellationToken);

    public Task AddOrderAsync(Order order, CancellationToken cancellationToken) =>
        db.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task<Order?> FindOrderAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        Orders().FirstOrDefaultAsync(order => order.MerchantId == merchantId && order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListOrdersAsync(Guid merchantId, OrderStatus? status, CancellationToken cancellationToken)
    {
        var query = Orders().Where(order => order.MerchantId == merchantId);
        if (status is not null)
            query = query.Where(order => order.Status == status);
        return await query.OrderByDescending(order => order.CreatedAt).Take(50).ToListAsync(cancellationToken);
    }

    public Task<bool> ReferenceExistsAsync(Guid merchantId, string reference, CancellationToken cancellationToken) =>
        db.Orders.AnyAsync(order => order.MerchantId == merchantId && order.Reference == reference, cancellationToken);

    public Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken) =>
        db.Payments.AddAsync(payment, cancellationToken).AsTask();

    public Task<Payment?> FindPaymentAsync(Guid id, CancellationToken cancellationToken) =>
        db.Payments.FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public Task<Payment?> FindPaymentByProviderIdAsync(string providerId, CancellationToken cancellationToken) =>
        db.Payments.FirstOrDefaultAsync(payment => payment.ProviderId == providerId, cancellationToken);

    public Task<Payment?> FindPendingPaymentAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Payments.FirstOrDefaultAsync(
            payment => payment.OrderId == orderId && payment.Status == PaymentStatus.Pending,
            cancellationToken);

    public Task AddWebhookAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken) =>
        db.WebhookEndpoints.AddAsync(endpoint, cancellationToken).AsTask();

    public Task<WebhookEndpoint?> FindWebhookAsync(Guid merchantId, CancellationToken cancellationToken) =>
        db.WebhookEndpoints.FirstOrDefaultAsync(endpoint => endpoint.MerchantId == merchantId, cancellationToken);

    public Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken) =>
        db.WebhookDeliveries.AddAsync(delivery, cancellationToken).AsTask();

    public async Task<IReadOnlyList<WebhookDelivery>> ListDeliveriesAsync(Guid merchantId, CancellationToken cancellationToken) =>
        await db.WebhookDeliveries.Where(delivery => delivery.MerchantId == merchantId)
            .OrderByDescending(delivery => delivery.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

    public Task AddIntegrationAsync(WompiIntegration integration, CancellationToken cancellationToken) =>
        db.WompiIntegrations.AddAsync(integration, cancellationToken).AsTask();

    public Task<WompiIntegration?> FindIntegrationAsync(Guid merchantId, CancellationToken cancellationToken) =>
        db.WompiIntegrations.FirstOrDefaultAsync(integration => integration.MerchantId == merchantId, cancellationToken);

    public Task AddRequestLogAsync(RequestLog log, CancellationToken cancellationToken) =>
        db.RequestLogs.AddAsync(log, cancellationToken).AsTask();

    private IQueryable<Order> Orders() =>
        db.Orders.Include(order => order.Lines).Include(order => order.History);
}
