using System.Net;
using System.Text.Json;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationException => (HttpStatusCode.BadRequest, validationException.Message),
            NotFoundException notFoundException => (HttpStatusCode.NotFound, notFoundException.Message),
            UnauthorizedAppException unauthorizedException => (HttpStatusCode.Unauthorized, unauthorizedException.Message),
            ForbiddenException forbiddenException => (HttpStatusCode.Forbidden, forbiddenException.Message),
            BusinessException businessException => (HttpStatusCode.Conflict, businessException.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled application exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = ApiResponse<object>.Failure(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
