using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkService.Application.Contracts;
using WorkService.Domain.Interfaces.Services;
using WorkService.Infrastructure.Configuration;
using StackExchange.Redis;

namespace WorkService.Infrastructure.Services.ErrorCodeResolver;

public class ErrorCodeResolverService : IErrorCodeResolverService
{
    private static readonly ConcurrentDictionary<string, (string ResponseCode, string Description)> InMemoryCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<ErrorCodeResolverService> _logger;

    public ErrorCodeResolverService(
        IHttpClientFactory httpClientFactory,
        IConnectionMultiplexer redis,
        AppSettings appSettings,
        ILogger<ErrorCodeResolverService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<(string ResponseCode, string ResponseDescription)> ResolveAsync(
        string errorCode, CancellationToken ct = default)
    {
        // 1. Check in-memory cache
        if (InMemoryCache.TryGetValue(errorCode, out var cached))
            return cached;

        // 2. Check Redis cache
        var db = _redis.GetDatabase();
        var cacheKey = $"error_code:{errorCode}";
        var redisCached = await db.StringGetAsync(cacheKey);
        if (redisCached.HasValue)
        {
            var cachedResult = JsonSerializer.Deserialize<ErrorCodeResponse>(redisCached!, JsonOptions);
            if (cachedResult is not null)
            {
                var result = (cachedResult.ResponseCode, cachedResult.Description);
                InMemoryCache.TryAdd(errorCode, result);
                return result;
            }
        }

        // 3. Call UtilityService
        try
        {
            var client = _httpClientFactory.CreateClient("UtilityService");
            if (client.BaseAddress == null)
                client.BaseAddress = new Uri(_appSettings.UtilityServiceBaseUrl);

            var response = await client.GetAsync($"/api/v1/error-codes/{errorCode}", ct);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ErrorCodeResponse>(JsonOptions, ct);
                if (apiResponse is not null)
                {
                    var json = JsonSerializer.Serialize(apiResponse, JsonOptions);
                    await db.StringSetAsync(cacheKey, json, CacheTtl);
                    var result = (apiResponse.ResponseCode, apiResponse.Description);
                    InMemoryCache.TryAdd(errorCode, result);
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve error code {ErrorCode} from UtilityService. Falling back to static mapping.",
                errorCode);
        }

        // 4. Fallback to static mapping
        var responseCode = MapErrorToResponseCode(errorCode);
        return (responseCode, errorCode);
    }

    public static string MapErrorToResponseCode(string errorCode) => errorCode switch
    {
        _ when errorCode.Contains("DUPLICATE") || errorCode.Contains("CONFLICT") || errorCode.Contains("ALREADY") => "06",
        _ when errorCode.Contains("NOT_FOUND") => "07",
        "ORGANIZATION_MISMATCH" or "INSUFFICIENT_PERMISSIONS" or "DEPARTMENT_ACCESS_DENIED" => "03",
        "RATE_LIMIT_EXCEEDED" => "08",
        _ when errorCode.StartsWith("INVALID_") => "09",
        _ when errorCode.Contains("IMMUTABLE") || errorCode.Contains("CANNOT") || errorCode.Contains("REQUIRES") => "10",
        "VALIDATION_ERROR" => "96",
        "INTERNAL_ERROR" => "98",
        _ => "99"
    };
}
