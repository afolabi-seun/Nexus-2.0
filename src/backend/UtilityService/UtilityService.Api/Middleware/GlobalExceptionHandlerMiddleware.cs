using System.Net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UtilityService.Application.DTOs;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Services.ErrorCodeResolver;

namespace UtilityService.Api.Middleware;

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
        catch (DbUpdateException ex)
        {
            await HandleDbUpdateExceptionAsync(context, ex);
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
            correlationId, ex.ErrorCode, ex.ErrorValue, "UtilityService", context.Request.Path);

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

        context.Response.StatusCode = (int)ex.StatusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(response);
    }

    private async Task HandleDbUpdateExceptionAsync(HttpContext context, DbUpdateException ex)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        if (ex.InnerException is PostgresException pgEx)
        {
            var (errorCode, errorValue, message, statusCode) = MapPostgresException(pgEx);

            _logger.LogWarning(ex,
                "Database constraint violation. CorrelationId={CorrelationId}, SqlState={SqlState}, ConstraintName={ConstraintName}, TableName={TableName}, ServiceName={ServiceName}, RequestPath={RequestPath}",
                correlationId, pgEx.SqlState, pgEx.ConstraintName, pgEx.TableName, "UtilityService", context.Request.Path);

            var response = new ApiResponse<object>
            {
                Success = false,
                ErrorValue = errorValue,
                ErrorCode = errorCode,
                Message = message,
                CorrelationId = correlationId,
                ResponseCode = errorCode,
                ResponseDescription = message
            };

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        _logger.LogError(ex,
            "DbUpdateException occurred. CorrelationId={CorrelationId}, ServiceName={ServiceName}, RequestPath={RequestPath}, InnerException={InnerExceptionType}",
            correlationId, "UtilityService", context.Request.Path, ex.InnerException?.GetType().Name ?? "None");

        var fallback = new ApiResponse<object>
        {
            Success = false,
            ErrorValue = ErrorCodes.InternalErrorValue,
            ErrorCode = ErrorCodes.InternalError,
            Message = "A database error occurred.",
            CorrelationId = correlationId,
            ResponseCode = "98",
            ResponseDescription = "A database error occurred."
        };

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(fallback);
    }

    private async Task HandleUnhandledExceptionAsync(HttpContext context, Exception ex)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        _logger.LogError(ex,
            "Unhandled exception occurred. CorrelationId={CorrelationId}, ServiceName={ServiceName}, RequestPath={RequestPath}, ExceptionType={ExceptionType}, InnerExceptionType={InnerExceptionType}",
            correlationId, "UtilityService", context.Request.Path, ex.GetType().Name, ex.InnerException?.GetType().Name ?? "None");

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

    private static (string ErrorCode, int ErrorValue, string Message, HttpStatusCode StatusCode) MapPostgresException(PostgresException pgEx)
    {
        return pgEx.SqlState switch
        {
            "23505" => (
                ErrorCodes.UniqueConstraintViolation,
                ErrorCodes.UniqueConstraintViolationValue,
                $"A record with this value already exists (constraint: {pgEx.ConstraintName}).",
                HttpStatusCode.Conflict),
            "23503" => (
                ErrorCodes.ForeignKeyViolation,
                ErrorCodes.ForeignKeyViolationValue,
                $"Referenced record does not exist or cannot be removed (constraint: {pgEx.ConstraintName}).",
                HttpStatusCode.Conflict),
            _ => (
                ErrorCodes.InternalError,
                ErrorCodes.InternalErrorValue,
                "A database error occurred.",
                HttpStatusCode.InternalServerError)
        };
    }
}
