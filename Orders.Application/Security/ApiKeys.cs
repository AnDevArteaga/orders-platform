using System.Security.Cryptography;
using System.Text;
using Orders.Domain.Entities;

namespace Orders.Application.Security;

public static class ApiKeys
{
    public static (string Token, string Prefix, string Hash) Issue(ApiEnvironment environment)
    {
        var prefix = environment == ApiEnvironment.Sandbox ? "sk_test_" : "sk_live_";
        var token = prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        return (token, token[..16], Hash(token));
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
