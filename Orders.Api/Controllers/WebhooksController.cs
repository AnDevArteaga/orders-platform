using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orders.Api.Auth;
using Orders.Application.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize(Policy = "Integration")]
[EnableRateLimiting("integration")]
[Route("v1")]
public class WebhooksController(IntegrationService integrations) : ControllerBase
{
    [HttpPut("webhook")]
    public async Task<IActionResult> Save(WebhookRequest request, CancellationToken cancellationToken)
    {
        var (endpoint, secret) = await integrations.SaveWebhookAsync(User.MerchantId(), request.Url, cancellationToken);
        return Ok(new { url = endpoint.Url, secret });
    }

    [HttpGet("webhook")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var endpoint = await integrations.GetWebhookAsync(User.MerchantId(), cancellationToken);
        return Ok(new { url = endpoint?.Url, configured = endpoint is not null });
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(CancellationToken cancellationToken)
    {
        var data = await integrations.ListEventsAsync(User.MerchantId(), cancellationToken);
        return Ok(new
        {
            count = data.Count,
            data = data.Select(delivery => new
            {
                id = delivery.Id,
                type = delivery.Type,
                order_id = delivery.OrderId,
                attempts = delivery.Attempts,
                delivered = delivery.Delivered,
                last_status_code = delivery.LastStatusCode,
                created_at = delivery.CreatedAt
            })
        });
    }
}

public sealed record WebhookRequest(string Url);
