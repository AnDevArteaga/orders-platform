using System.Security.Cryptography;
using System.Text;

namespace Orders.Application.Security;

public static class WebhookSigner
{
    public static string Sign(string secret, long timestamp, string body)
    {
        var payload = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Header(string secret, long timestamp, string body) =>
        $"t={timestamp},v1={Sign(secret, timestamp, body)}";
}
