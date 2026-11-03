using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orders.Api.Auth;
using Orders.Application.Services;
using Orders.Domain.Entities;

namespace Orders.Api.Controllers;

[ApiController]
[Route("v1")]
public class PaymentsController(PaymentService payments) : ControllerBase
{
    [Authorize(Policy = "Integration")]
    [EnableRateLimiting("integration")]
    [HttpGet("payments/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.GetAsync(User.MerchantId(), id, cancellationToken);
        return Ok(ToResponse(payment));
    }

    [Authorize(Policy = "Integration")]
    [EnableRateLimiting("integration")]
    [HttpPost("sandbox/payments/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, ResolveRequest request, CancellationToken cancellationToken)
    {
        if (User.Environment() != ApiEnvironment.Sandbox)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "El simulador solo acepta llaves de sandbox." });

        var payment = await payments.ResolveSandboxAsync(id, request.Result, User.MerchantId(), cancellationToken);
        return Ok(ToResponse(payment));
    }

    [AllowAnonymous]
    [HttpPost("/webhooks/wompi")]
    public async Task<IActionResult> Wompi([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        await payments.ApplyWompiEventAsync(body, cancellationToken);
        return Ok(new { received = true });
    }

    internal static object ToResponse(Payment payment) => new
    {
        id = payment.Id,
        order_id = payment.OrderId,
        status = payment.Status,
        amount = payment.Amount,
        currency = payment.Currency,
        environment = payment.Environment,
        checkout_url = payment.CheckoutUrl,
        failure_reason = payment.FailureReason
    };
}

public sealed record ResolveRequest(string Result);
