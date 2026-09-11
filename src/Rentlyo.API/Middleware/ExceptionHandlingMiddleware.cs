using System.Net;
using Rentlyo.API.Exceptions;

namespace Rentlyo.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, _) = ApiErrorResponseWriter.MapException(exception);
            if (statusCode >= (int)HttpStatusCode.InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception");
            }
            else
            {
                logger.LogWarning(exception, "Handled application exception");
            }

            await ApiErrorResponseWriter.WriteExceptionAsync(context, exception);
        }
    }
}
