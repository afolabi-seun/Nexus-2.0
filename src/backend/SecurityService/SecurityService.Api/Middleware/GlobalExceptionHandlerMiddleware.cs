using System.Net;
using System.Text.Json;
using SecurityService.Application.DTOs;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services;

namespace SecurityService.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private async Task HandleDomainExceptionAsync(HttpContext context, DomainException ex)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        _logger.LogWarning(
            "DomainException occurred. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}, ErrorValue={ErrorValue}, ServiceName={ServiceName}, RequestPath={RequestPath}",
            correlationId, ex.ErrorCode, ex.ErrorValue, "SecurityService", context.Request.Path);

        var resolver = context.RequestServices.GetService<IErrorCodeResolverService>();
        var (responseCode, responseDescription) = resolver is not null
            ? await resolver.ResolveAsync(ex.ErrorCode, context.RequestAborted)
            : (ex.ErrorCode, ex.Message);

        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorValue = ex.ErrorValue,
            ErrorCode = ex.ErrorCode,
            Message = ex.Message,
            CorrelationId = correlationId,
            ResponseCode = responseCode,
            ResponseDescription = responseDescription
        };

        if (ex is RateLimitExceededException rle)
        {
            context.Response.Headers["Retry-After"] = rle.RetryAfterSeconds.ToString();
        }

        context.Response.StatusCode = (int)ex.StatusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(response);
    }

    private async Task HandleUnhandledExceptionAsync(HttpContext context, Exception ex)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        _logger.LogError(ex,
            "Unhandled exception occurred. CorrelationId={CorrelationId}, ServiceName={ServiceName}, RequestPath={RequestPath}, ExceptionType={ExceptionType}",
            correlationId, "SecurityService", context.Request.Path, ex.GetType().Name);

        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorValue = ErrorCodes.InternalErrorValue,
            ErrorCode = ErrorCodes.InternalError,
            Message = "An unexpected error occurred.",
            CorrelationId = correlationId,
            ResponseCode = "98",
            ResponseDescription = "An unexpected error occurred."
        };

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(response);
    }
}
