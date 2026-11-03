using System.Diagnostics;
using System.Security.Claims;
using Orders.Application.Abstractions;
using Orders.Domain.Entities;

namespace Orders.Api.Middlewares;

public sealed class RequestLogMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IPlatformStore store)
    {
        if (!context.Request.Path.StartsWithSegments("/v1"))
        {
            await next(context);
            return;
        }

        var id = Guid.NewGuid();
        context.Response.Headers["X-Request-Id"] = id.ToString();
        var watch = Stopwatch.StartNew();
        await next(context);
        watch.Stop();

        try
        {
            await store.AddRequestLogAsync(new RequestLog(
                id,
                context.User.Identity?.IsAuthenticated == true ? context.User.MerchantIdOrNull() : null,
                context.User.ApiKeyIdOrNull(),
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                watch.ElapsedMilliseconds), context.RequestAborted);
            await store.SaveAsync(context.RequestAborted);
        }
        catch (Exception exception)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<RequestLogMiddleware>>();
            logger.LogWarning(exception, "No se pudo guardar el registro de la solicitud.");
        }
    }
}

file static class RequestLogClaims
{
    public static Guid? MerchantIdOrNull(this System.Security.Claims.ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? ApiKeyIdOrNull(this System.Security.Claims.ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("key_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
