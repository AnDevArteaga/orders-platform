using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Auth;
using Orders.Application.Services;
using Orders.Domain.Entities;
using Orders.Infrastructure.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("account")]
public class AccountController(AccountService accounts, JwtService tokens, IntegrationService integrations) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var id = await accounts.RegisterAsync(request.Name, request.Email, request.Password, cancellationToken);
        return Created($"/account/me", new { id });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var merchant = await accounts.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        return Ok(new { token = tokens.Create(merchant.Id, merchant.Email), token_type = "Bearer" });
    }

    [Authorize(Policy = "Account")]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new { id = User.MerchantId() });

    [Authorize(Policy = "Account")]
    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateKey(CreateKeyRequest request, CancellationToken cancellationToken)
    {
        var issued = await accounts.IssueKeyAsync(User.MerchantId(), request.Name, request.Environment, cancellationToken);
        return Created($"/account/api-keys/{issued.Id}", new
        {
            id = issued.Id,
            name = issued.Name,
            prefix = issued.Prefix,
            environment = issued.Environment,
            token = issued.Token
        });
    }

    [Authorize(Policy = "Account")]
    [HttpGet("api-keys")]
    public async Task<IActionResult> ListKeys(CancellationToken cancellationToken)
    {
        var keys = await accounts.ListKeysAsync(User.MerchantId(), cancellationToken);
        return Ok(new
        {
            count = keys.Count,
            data = keys.Select(key => new
            {
                id = key.Id,
                name = key.Name,
                prefix = key.Prefix,
                environment = key.Environment,
                revoked = key.Revoked,
                last_used_at = key.LastUsedAt,
                created_at = key.CreatedAt
            })
        });
    }

    [Authorize(Policy = "Account")]
    [HttpDelete("api-keys/{id:guid}")]
    public async Task<IActionResult> RevokeKey(Guid id, CancellationToken cancellationToken)
    {
        await accounts.RevokeKeyAsync(User.MerchantId(), id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "Account")]
    [HttpPut("integrations/wompi")]
    public async Task<IActionResult> SaveWompi(WompiRequest request, CancellationToken cancellationToken)
    {
        await integrations.SaveWompiAsync(
            User.MerchantId(),
            request.PublicKey,
            request.PrivateKey,
            request.EventsSecret,
            request.Target,
            cancellationToken);
        return Ok(new { configured = true, activated = false });
    }

    [Authorize(Policy = "Account")]
    [HttpPost("integrations/wompi/activate")]
    public async Task<IActionResult> ActivateWompi(CancellationToken cancellationToken)
    {
        await integrations.ActivateAsync(User.MerchantId(), cancellationToken);
        return Ok(new { activated = true });
    }

    [Authorize(Policy = "Account")]
    [HttpGet("integrations/wompi")]
    public async Task<IActionResult> GetWompi(CancellationToken cancellationToken)
    {
        var integration = await integrations.GetAsync(User.MerchantId(), cancellationToken);
        return Ok(new
        {
            configured = integration?.IsConfigured ?? false,
            activated = integration?.Activated ?? false,
            target = integration?.Target
        });
    }
}

public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record CreateKeyRequest(string Name, ApiEnvironment Environment);
public sealed record WompiRequest(string PublicKey, string PrivateKey, string EventsSecret, ApiEnvironment Target);
