using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orders.Application.Wompi;

public static class WompiSignature
{
    public static bool IsValid(JsonElement body, string eventsSecret)
    {
        if (!body.TryGetProperty("signature", out var signature) ||
            !signature.TryGetProperty("checksum", out var checksumElement) ||
            !body.TryGetProperty("timestamp", out var timestampElement))
            return false;

        var expected = checksumElement.GetString();
        if (string.IsNullOrWhiteSpace(expected))
            return false;

        var actual = Compute(body, eventsSecret);
        var left = Encoding.UTF8.GetBytes(actual);
        var right = Encoding.UTF8.GetBytes(expected.ToLowerInvariant());
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    public static string Compute(JsonElement body, string eventsSecret)
    {
        var data = body.GetProperty("data");
        var builder = new StringBuilder();
        foreach (var property in body.GetProperty("signature").GetProperty("properties").EnumerateArray())
        {
            var path = property.GetString() ?? "";
            builder.Append(Read(data, path));
        }

        builder.Append(body.GetProperty("timestamp").GetInt64());
        builder.Append(eventsSecret);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Read(JsonElement data, string path)
    {
        var current = data;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!current.TryGetProperty(part, out current))
                return "";
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? "",
            JsonValueKind.Number => current.GetRawText(),
            _ => current.ToString()
        };
    }
}
