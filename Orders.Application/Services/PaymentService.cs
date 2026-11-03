using System.Text.Json;
using Orders.Application.Abstractions;
using Orders.Application.Wompi;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Application.Services;

public sealed class PaymentService(
    IPlatformStore store,
    IWompiClient wompi,
    ISecretProtector protector,
    IWebhookDispatcher webhooks)
{
    public async Task<Payment> StartAsync(
        Guid merchantId,
        ApiEnvironment environment,
        Guid orderId,
        string? sandboxResult,
        string publicBaseUrl,
        string? redirectUrl,
        CancellationToken cancellationToken)
    {
        var order = await store.FindOrderAsync(merchantId, orderId, cancellationToken)
            ?? throw new NotFoundException("No existe ese pedido.");

        var pending = await store.FindPendingPaymentAsync(order.Id, cancellationToken);
        if (pending is not null && string.IsNullOrWhiteSpace(sandboxResult))
            return pending;

        if (environment == ApiEnvironment.Production)
        {
            var integration = await store.FindIntegrationAsync(merchantId, cancellationToken);
            if (integration is null || !integration.Activated)
                throw new ForbiddenException("La integración de producción no está activa.");
        }

        if (pending is null)
        {
            order.BeginPayment();
            pending = new Payment(merchantId, order.Id, order.Total, environment);
            await store.AddPaymentAsync(pending, cancellationToken);
        }

        if (environment == ApiEnvironment.Sandbox)
        {
            pending.AttachCheckout($"{publicBaseUrl.TrimEnd('/')}/sandbox/checkout/{pending.Id}", "sandbox");
            await store.SaveAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(sandboxResult))
                await ResolveAsync(pending, sandboxResult, cancellationToken);
            return pending;
        }

        var wompiIntegration = await store.FindIntegrationAsync(merchantId, cancellationToken)
            ?? throw new ForbiddenException("La integración de producción no está activa.");
        var linkId = await wompi.CreatePaymentLinkAsync(new WompiLinkRequest(
            protector.Unprotect(wompiIntegration.PrivateKeyProtected),
            wompiIntegration.Target == ApiEnvironment.Sandbox
                ? "https://sandbox.wompi.co/v1"
                : "https://production.wompi.co/v1",
            $"Pedido {order.Id:N}",
            ToCents(order.Total),
            order.Currency,
            string.IsNullOrWhiteSpace(redirectUrl) ? publicBaseUrl : redirectUrl), cancellationToken);

        pending.AttachCheckout($"https://checkout.wompi.co/l/{linkId}", linkId);
        await store.SaveAsync(cancellationToken);
        return pending;
    }

    public async Task<Payment> GetAsync(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await store.FindPaymentAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException("No existe ese pago.");
        if (payment.MerchantId != merchantId)
            throw new NotFoundException("No existe ese pago.");
        return payment;
    }

    public async Task<Payment> ResolveSandboxAsync(Guid paymentId, string result, Guid? merchantId, CancellationToken cancellationToken)
    {
        var payment = await store.FindPaymentAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException("No existe ese pago.");
        if (payment.Environment != ApiEnvironment.Sandbox)
            throw new ForbiddenException("Ese pago no es de sandbox.");
        if (merchantId is not null && payment.MerchantId != merchantId)
            throw new NotFoundException("No existe ese pago.");

        await ResolveAsync(payment, result, cancellationToken);
        return payment;
    }

    public async Task ApplyWompiEventAsync(JsonElement body, CancellationToken cancellationToken)
    {
        if (!body.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("transaction", out var transaction))
            return;

        var providerId = transaction.TryGetProperty("payment_link_id", out var link) ? link.GetString() : null;
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        var payment = await store.FindPaymentByProviderIdAsync(providerId, cancellationToken);
        if (payment is null)
            return;

        var integration = await store.FindIntegrationAsync(payment.MerchantId, cancellationToken);
        if (integration is null || !integration.IsConfigured)
            throw new ForbiddenException("No hay credenciales para verificar el evento.");

        if (!WompiSignature.IsValid(body, protector.Unprotect(integration.EventsSecretProtected)))
            throw new ForbiddenException("La firma del evento de Wompi no es válida.");

        var status = transaction.GetProperty("status").GetString() ?? "";
        var cents = transaction.GetProperty("amount_in_cents").GetInt64();
        await ApplyProviderResultAsync(payment, status, cents / 100m, cancellationToken);
    }

    private async Task ResolveAsync(Payment payment, string result, CancellationToken cancellationToken)
    {
        var normalized = result.Trim().ToLowerInvariant();
        var status = normalized switch
        {
            "approved" => "APPROVED",
            "declined" => "DECLINED",
            "pending" => "PENDING",
            _ => throw new DomainException("El resultado de sandbox tiene que ser approved, declined o pending.")
        };
        await ApplyProviderResultAsync(payment, status, payment.Amount, cancellationToken);
    }

    private async Task ApplyProviderResultAsync(Payment payment, string status, decimal amount, CancellationToken cancellationToken)
    {
        if (payment.Status != PaymentStatus.Pending)
            return;

        var order = await store.FindOrderAsync(payment.MerchantId, payment.OrderId, cancellationToken)
            ?? throw new NotFoundException("No existe el pedido de este pago.");

        if (order.Status != OrderStatus.PendingPayment)
            return;

        if (status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
            return;

        if (status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            if (amount != order.Total)
            {
                payment.Decline("El monto cobrado no coincide con el total del pedido.");
                order.MarkPaymentFailed();
                await store.SaveAsync(cancellationToken);
                await NotifyAsync(order, payment, "order.payment_failed", cancellationToken);
                return;
            }

            payment.Approve();
            order.MarkPaid(amount);
            await store.SaveAsync(cancellationToken);
            await NotifyAsync(order, payment, "order.paid", cancellationToken);
            return;
        }

        payment.Decline($"Wompi informó el estado {status}.");
        order.MarkPaymentFailed();
        await store.SaveAsync(cancellationToken);
        await NotifyAsync(order, payment, "order.payment_failed", cancellationToken);
    }

    private Task NotifyAsync(Order order, Payment payment, string type, CancellationToken cancellationToken) =>
        webhooks.DispatchAsync(order.MerchantId, order.Id, type, new
        {
            order_id = order.Id,
            payment_id = payment.Id,
            status = order.Status.ToString().ToLowerInvariant(),
            amount = order.Total,
            currency = order.Currency
        }, cancellationToken);

    private static long ToCents(decimal amount) => checked((long)(amount * 100m));
}
