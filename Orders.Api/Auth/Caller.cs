using System.Security.Claims;
using Orders.Domain.Entities;

namespace Orders.Api.Auth;

public static class Caller
{
    public static Guid MerchantId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("sub")!);

    public static ApiEnvironment Environment(this ClaimsPrincipal user) =>
        Enum.Parse<ApiEnvironment>(user.FindFirstValue("env")!, ignoreCase: true);

    public static Guid? ApiKeyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("key_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
