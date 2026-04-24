using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkService.Application.Contracts;
using WorkService.Domain.Interfaces.Services.ErrorCodeResolver;
using WorkService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.ErrorCodeResolver;

public class ErrorCodeResolverService : IErrorCodeResolverService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<string, (string ResponseCode, string ResponseDescription)>
        _memoryCache = new();

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
        // Tier 1: In-memory cache
        if (_memoryCache.TryGetValue(errorCode, out var memoryCached))
            return memoryCached;

        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.ErrorCode(errorCode);

        // Tier 2: Redis cache
        try
        {
            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                var cachedResult = JsonSerializer.Deserialize<ErrorCodeResponse>(cached!, JsonOptions);
                if (cachedResult is not null)
                {
                    var redisValue = (cachedResult.ResponseCode, cachedResult.Description);
                    _memoryCache.TryAdd(errorCode, redisValue);
                    return redisValue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache lookup failed for error code {ErrorCode}.", errorCode);
        }

        // Tier 3: HTTP call to UtilityService
        try
        {
            var result = await _utilityClient.GetErrorCodeAsync(errorCode, ct);
            var httpValue = (result.ResponseCode, result.Description);
            _memoryCache.TryAdd(errorCode, httpValue);

            try
            {
                var json = JsonSerializer.Serialize(result, JsonOptions);
                await db.StringSetAsync(cacheKey, json, CacheTtl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to populate Redis cache for error code {ErrorCode}.", errorCode);
            }

            return httpValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve error code {ErrorCode} from UtilityService. Falling back to static mapping.",
                errorCode);
        }

        // Tier 4: Static fallback
        var responseCode = MapErrorToResponseCode(errorCode);
        return (responseCode, errorCode);
    }

    public async Task RefreshCacheAsync(CancellationToken ct = default)
    {
        var allCodes = await _utilityClient.GetAllErrorCodesAsync(ct);

        _memoryCache.Clear();
        foreach (var (code, value) in allCodes)
        {
            _memoryCache.TryAdd(code, value);
        }

        try
        {
            var db = _redis.GetDatabase();
            foreach (var (code, (responseCode, description)) in allCodes)
            {
                var response = new ErrorCodeResponse
                {
                    ResponseCode = responseCode,
                    Description = description
                };
                var json = JsonSerializer.Serialize(response, JsonOptions);
                await db.StringSetAsync(RedisKeys.ErrorCode(code), json, CacheTtl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate Redis cache during refresh.");
        }
    }

    public void ClearMemoryCache() => _memoryCache.Clear();

    public static string MapErrorToResponseCode(string errorCode) => errorCode switch
    {
        _ when errorCode.Contains("DUPLICATE") || errorCode.Contains("CONFLICT") || errorCode.Contains("ALREADY") => "06",
        _ when errorCode.Contains("NOT_FOUND") => "07",
        "ORGANIZATION_MISMATCH" or "INSUFFICIENT_PERMISSIONS" or "DEPARTMENT_ACCESS_DENIED"
            or "ORGADMIN_REQUIRED" or "DEPTLEAD_REQUIRED" or "PLATFORM_ADMIN_REQUIRED" => "03",
        "RATE_LIMIT_EXCEEDED" => "08",
        _ when errorCode.StartsWith("INVALID_") => "09",
        _ when errorCode.Contains("IMMUTABLE") || errorCode.Contains("CANNOT") || errorCode.Contains("REQUIRES") => "10",
        "VALIDATION_ERROR" => "96",
        "INTERNAL_ERROR" => "98",
        _ => "99"
    };
}
