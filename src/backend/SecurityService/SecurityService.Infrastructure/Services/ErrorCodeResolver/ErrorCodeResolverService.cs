using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityService.Application.Contracts;
using SecurityService.Domain.Interfaces.Services.ErrorCodeResolver;
using SecurityService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.ErrorCodeResolver;

public class ErrorCodeResolverService : IErrorCodeResolverService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IUtilityServiceClient _utilityClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ErrorCodeResolverService> _logger;

    public ErrorCodeResolverService(
        IUtilityServiceClient utilityClient,
        IConnectionMultiplexer redis,
        ILogger<ErrorCodeResolverService> logger)
    {
        _utilityClient = utilityClient;
        _redis = redis;
        _logger = logger;
    }

    public async Task<(string ResponseCode, string ResponseDescription)> ResolveAsync(
        string errorCode, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = $"error_code:{errorCode}";

        // 1. Check Redis cache
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedResult = JsonSerializer.Deserialize<ErrorCodeResponse>(cached!, JsonOptions);
            if (cachedResult is not null)
                return (cachedResult.ResponseCode, cachedResult.ResponseDescription);
        }

        // 2. Call UtilityService
        try
        {
            var result = await _utilityClient.GetErrorCodeAsync(errorCode, ct);
            var json = JsonSerializer.Serialize(result, JsonOptions);
            await db.StringSetAsync(cacheKey, json, CacheTtl);
            return (result.ResponseCode, result.ResponseDescription);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve error code {ErrorCode} from UtilityService. Falling back to static mapping.", errorCode);
        }

        // 3. Fallback to static mapping
        var responseCode = MapErrorToResponseCode(errorCode);
        return (responseCode, errorCode);
    }

    public static string MapErrorToResponseCode(string errorCode) => errorCode switch
    {
        "INVALID_CREDENTIALS" => "01",
        "ACCOUNT_LOCKED" or "ACCOUNT_INACTIVE" => "02",
        "INSUFFICIENT_PERMISSIONS" or "DEPARTMENT_ACCESS_DENIED" or "ORGANIZATION_MISMATCH"
            or "ORGADMIN_REQUIRED" or "DEPTLEAD_REQUIRED" or "PLATFORM_ADMIN_REQUIRED" => "03",
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
