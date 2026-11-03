using Microsoft.AspNetCore.Diagnostics;
using Orders.Domain.Exceptions;

namespace Orders.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Fallo no controlado.");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);
        return true;
    }
}
