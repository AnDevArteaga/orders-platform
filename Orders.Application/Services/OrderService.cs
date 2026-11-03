using Orders.Application.Abstractions;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Application.Services;

public sealed class OrderService(IPlatformStore store, IWebhookDispatcher webhooks)
{
    public async Task<Order> CreateAsync(
        Guid merchantId,
        Guid customerId,
        string? reference,
        IReadOnlyList<(string Name, int Quantity, decimal UnitAmount)> lines,
        CancellationToken cancellationToken)
    {
        _ = await store.FindCustomerAsync(merchantId, customerId, cancellationToken)
            ?? throw new NotFoundException("No existe ese cliente.");

        if (!string.IsNullOrWhiteSpace(reference) &&
            await store.ReferenceExistsAsync(merchantId, reference.Trim(), cancellationToken))
            throw new ConflictException("Ya existe un pedido con esa referencia.");

        var order = Order.Create(merchantId, customerId, reference, lines);
        await store.AddOrderAsync(order, cancellationToken);
        await store.SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> GetAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        await store.FindOrderAsync(merchantId, id, cancellationToken)
        ?? throw new NotFoundException("No existe ese pedido.");

    public Task<IReadOnlyList<Order>> ListAsync(Guid merchantId, OrderStatus? status, CancellationToken cancellationToken) =>
        store.ListOrdersAsync(merchantId, status, cancellationToken);

    public async Task<Order> CancelAsync(Guid merchantId, Guid id, CancellationToken cancellationToken)
    {
        var order = await GetAsync(merchantId, id, cancellationToken);
        order.Cancel();
        await store.SaveAsync(cancellationToken);
        return order;
    }

    public async Task<Order> FulfillAsync(Guid merchantId, Guid id, CancellationToken cancellationToken)
    {
        var order = await GetAsync(merchantId, id, cancellationToken);
        order.Fulfill();
        await store.SaveAsync(cancellationToken);
        await webhooks.DispatchAsync(merchantId, order.Id, "order.fulfilled", new
        {
            order_id = order.Id,
            status = "fulfilled"
        }, cancellationToken);
        return order;
    }
}
