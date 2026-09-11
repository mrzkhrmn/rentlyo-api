using System.Net;
using System.Text.Json;
using Rentlyo.Shared.Exceptions;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Exceptions;

public static class ApiErrorResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static (int StatusCode, string Message) MapException(Exception exception) =>
        exception switch
        {
            ValidationException validationException => ((int)HttpStatusCode.BadRequest, validationException.Message),
            NotFoundException notFoundException => ((int)HttpStatusCode.NotFound, notFoundException.Message),
            UnauthorizedAppException unauthorizedException => ((int)HttpStatusCode.Unauthorized, unauthorizedException.Message),
            ForbiddenException forbiddenException => ((int)HttpStatusCode.Forbidden, forbiddenException.Message),
            BusinessException businessException => ((int)HttpStatusCode.Conflict, businessException.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = ApiResponse<object>.Failure(message);
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);
    }

    public static Task WriteExceptionAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var (statusCode, message) = MapException(exception);
        return WriteAsync(context, statusCode, message, cancellationToken);
    }
}
