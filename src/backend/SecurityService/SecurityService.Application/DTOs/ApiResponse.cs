namespace SecurityService.Application.DTOs;

public class ApiResponse<T>
{
    public string ResponseCode { get; set; } = "00";
    public string ResponseDescription { get; set; } = "Request successful";
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public int? ErrorValue { get; set; }
    public string? Message { get; set; }
    public string? CorrelationId { get; set; }
    public List<ErrorDetail>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        ResponseCode = "00",
        ResponseDescription = "Request successful"
    };

    public static ApiResponse<T> Fail(int errorValue, string errorCode, string message) => new()
    {
        Success = false,
        ErrorValue = errorValue,
        ErrorCode = errorCode,
        Message = message,
        ResponseCode = MapErrorToResponseCode(errorCode),
        ResponseDescription = message
    };

    public static ApiResponse<T> ValidationFail(string message, List<ErrorDetail> errors) => new()
    {
        Success = false,
        ErrorValue = 1000,
        ErrorCode = "VALIDATION_ERROR",
        Message = message,
        Errors = errors,
        ResponseCode = "96",
        ResponseDescription = message
    };

    private static string MapErrorToResponseCode(string errorCode) => errorCode switch
    {
        "INVALID_CREDENTIALS" => "01",
        "ACCOUNT_LOCKED" or "ACCOUNT_INACTIVE" => "02",
        "INSUFFICIENT_PERMISSIONS" or "DEPARTMENT_ACCESS_DENIED" or "ORGANIZATION_MISMATCH" => "03",
        _ when errorCode.StartsWith("OTP_") => "04",
        _ when errorCode.StartsWith("PASSWORD_") => "05",
        _ when errorCode.Contains("DUPLICATE") || errorCode.Contains("CONFLICT") => "06",
        _ when errorCode.Contains("NOT_FOUND") => "07",
        "RATE_LIMIT_EXCEEDED" => "08",
        _ when errorCode.StartsWith("INVALID_") => "09",
        "VALIDATION_ERROR" => "96",
        "INTERNAL_ERROR" => "98",
        _ => "99"
    };
}
