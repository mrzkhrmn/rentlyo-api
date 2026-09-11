using System.Net;
using Microsoft.AspNetCore.Diagnostics;

namespace Rentlyo.API.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, _) = ApiErrorResponseWriter.MapException(exception);

        if (statusCode >= (int)HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled application exception");
        }

        await ApiErrorResponseWriter.WriteExceptionAsync(httpContext, exception, cancellationToken);
        return true;
    }
}
