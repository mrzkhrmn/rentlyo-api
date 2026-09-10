namespace Rentlyo.Shared.Responses;

public class ApiResponse<T>
{
    public T? Data { get; init; }
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }

    public static ApiResponse<T> Success(T data, string? message = null) => new()
    {
        Data = data,
        IsSuccess = true,
        Message = message
    };

    public static ApiResponse<T> Failure(string message) => new()
    {
        Data = default,
        IsSuccess = false,
        Message = message
    };
}
