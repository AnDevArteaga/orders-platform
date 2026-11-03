using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orders.Application.Abstractions;
using Orders.Application.Security;

namespace Orders.Api.Auth;

public sealed class PlatformAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IPlatformStore store,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (token.Count(character => character == '.') == 2)
            return AuthenticateJwt(token);

        return await AuthenticateApiKeyAsync(token);
    }

    private AuthenticateResult AuthenticateJwt(string token)
    {
        var secret = configuration["Auth:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            return AuthenticateResult.Fail("Falta la clave de firma.");

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Auth:Issuer"],
            ValidAudience = configuration["Auth:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            NameClaimType = "sub"
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out _);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception exception)
        {
            return AuthenticateResult.Fail(exception);
        }
    }

    private async Task<AuthenticateResult> AuthenticateApiKeyAsync(string token)
    {
        var key = await store.FindApiKeyByHashAsync(ApiKeys.Hash(token), CancellationToken.None);
        if (key is null || key.Revoked)
            return AuthenticateResult.Fail("API key inválida.");

        key.Touch(DateTime.UtcNow);
        await store.SaveAsync(CancellationToken.None);

        var identity = new ClaimsIdentity(
        [
            new Claim("sub", key.MerchantId.ToString()),
            new Claim("kind", "api_key"),
            new Claim("env", key.Environment.ToString().ToLowerInvariant()),
            new Claim("key_id", key.Id.ToString())
        ], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
