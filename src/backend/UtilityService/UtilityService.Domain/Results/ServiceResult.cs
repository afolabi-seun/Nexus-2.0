namespace UtilityService.Domain.Results;

/// <summary>
/// Unified result wrapper returned by service methods.
/// Use ToActionResult() extension to convert to IActionResult in controllers.
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public int StatusCode { get; init; } = 200;
    public int? ErrorValue { get; init; }
    public string? ErrorCode { get; init; }

    public static ServiceResult<T> Ok(T data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message,
        StatusCode = 200
    };

    public static ServiceResult<T> Created(T data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message,
        StatusCode = 201
    };

    public static ServiceResult<T> NoContent(string? message = null) => new()
    {
        IsSuccess = true,
        Data = default,
        Message = message,
        StatusCode = 204
    };

    public static ServiceResult<T> Fail(int errorValue, string errorCode, string message, int statusCode = 400) => new()
    {
        IsSuccess = false,
        Data = default,
        Message = message,
        StatusCode = statusCode,
        ErrorValue = errorValue,
        ErrorCode = errorCode
    };
}
