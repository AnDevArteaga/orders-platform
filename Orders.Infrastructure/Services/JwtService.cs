using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Orders.Infrastructure.Services;

public sealed class JwtService(IConfiguration configuration)
{
    public string Create(Guid merchantId, string email)
    {
        var secret = configuration["Auth:Secret"]
            ?? throw new InvalidOperationException("Falta Auth:Secret.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: configuration["Auth:Issuer"],
            audience: configuration["Auth:Audience"],
            claims:
            [
                new Claim("sub", merchantId.ToString()),
                new Claim("email", email),
                new Claim("kind", "account")
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
