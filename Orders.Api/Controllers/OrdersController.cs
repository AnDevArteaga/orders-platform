using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orders.Api.Auth;
using Orders.Application.Services;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize(Policy = "Integration")]
[EnableRateLimiting("integration")]
[Route("v1/orders")]
public class OrdersController(OrderService orders, PaymentService payments, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new DomainException("El pedido necesita al menos una línea.");

        var order = await orders.CreateAsync(
            User.MerchantId(),
            request.CustomerId,
            request.Reference,
            request.Lines.Select(line => (line.Name, line.Quantity, line.UnitAmount)).ToList(),
            cancellationToken);
        return Created($"/v1/orders/{order.Id}", ToResponse(order));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var data = await orders.ListAsync(User.MerchantId(), ParseStatus(status), cancellationToken);
        return Ok(new { count = data.Count, data = data.Select(ToResponse) });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetAsync(User.MerchantId(), id, cancellationToken);
        return Ok(ToResponse(order));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.CancelAsync(User.MerchantId(), id, cancellationToken);
        return Ok(ToResponse(order));
    }

    [HttpPost("{id:guid}/fulfill")]
    public async Task<IActionResult> Fulfill(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.FulfillAsync(User.MerchantId(), id, cancellationToken);
        return Ok(ToResponse(order));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> Pay(Guid id, PaymentRequest? request, CancellationToken cancellationToken)
    {
        var payment = await payments.StartAsync(
            User.MerchantId(),
            User.Environment(),
            id,
            request?.SandboxResult,
            configuration["PublicBaseUrl"] ?? "http://localhost:5080",
            request?.RedirectUrl,
            cancellationToken);
        return Ok(PaymentsController.ToResponse(payment));
    }

    private static OrderStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        foreach (var name in Enum.GetNames<OrderStatus>())
        {
            if (name.Equals(status, StringComparison.OrdinalIgnoreCase) ||
                Snake(name).Equals(status, StringComparison.OrdinalIgnoreCase))
                return Enum.Parse<OrderStatus>(name);
        }

        throw new DomainException("El estado del pedido no es válido.");
    }

    private static string Snake(string name) =>
        string.Concat(name.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));

    internal static object ToResponse(Order order) => new
    {
        id = order.Id,
        customer_id = order.CustomerId,
        reference = order.Reference,
        currency = order.Currency,
        status = order.Status,
        total = order.Total,
        created_at = order.CreatedAt,
        updated_at = order.UpdatedAt,
        lines = order.Lines.Select(line => new
        {
            name = line.Name,
            quantity = line.Quantity,
            unit_amount = line.UnitAmount,
            total = line.LineTotal
        }),
        history = order.History
            .OrderBy(change => change.CreatedAt)
            .Select(change => new { status = change.Status, at = change.CreatedAt })
    };
}

public sealed record CreateOrderRequest(Guid CustomerId, string? Reference, List<OrderLineRequest> Lines);
public sealed record OrderLineRequest(string Name, int Quantity, decimal UnitAmount);
public sealed record PaymentRequest(string? SandboxResult, string? RedirectUrl);
