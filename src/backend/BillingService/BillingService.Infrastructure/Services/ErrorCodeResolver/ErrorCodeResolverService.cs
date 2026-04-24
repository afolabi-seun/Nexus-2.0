using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using BillingService.Application.Contracts;
using BillingService.Domain.Interfaces.Services.ErrorCodeResolver;
using BillingService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;
using BillingService.Infrastructure.Redis;

namespace BillingService.Infrastructure.Services.ErrorCodeResolver;

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

    public async Task<(string responseCode, string responseDescription)> ResolveAsync(
        string errorCode, CancellationToken ct)
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
                    var redisValue = (cachedResult.ResponseCode, cachedResult.ResponseDescription);
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
            var httpValue = (result.ResponseCode, result.ResponseDescription);
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
        var (code, description) = errorCode switch
        {
            "SUBSCRIPTION_ALREADY_EXISTS" => ("06", "Organization already has an active subscription."),
            "PLAN_NOT_FOUND" => ("07", "Specified plan does not exist or is inactive."),
            "SUBSCRIPTION_NOT_FOUND" => ("07", "No subscription found for the organization."),
            "INVALID_UPGRADE_PATH" => ("09", "Target plan is not a higher tier."),
            "NO_ACTIVE_SUBSCRIPTION" => ("09", "Organization has no active or trialing subscription."),
            "INVALID_DOWNGRADE_PATH" => ("09", "Target plan is not a lower tier."),
            "USAGE_EXCEEDS_PLAN_LIMITS" => ("09", "Current usage exceeds target plan limits."),
            "SUBSCRIPTION_ALREADY_CANCELLED" => ("06", "Subscription is already cancelled."),
            "TRIAL_EXPIRED" => ("09", "Trial period has ended."),
            "PAYMENT_PROVIDER_ERROR" => ("99", "Payment provider error."),
            "INVALID_WEBHOOK_SIGNATURE" => ("09", "Stripe webhook signature verification failed."),
            "INVALID_WEBHOOK_PAYLOAD" => ("09", "Webhook payload could not be deserialized."),
            "FEATURE_NOT_AVAILABLE" => ("03", "Feature not included in current plan."),
            "USAGE_LIMIT_REACHED" => ("03", "Usage limit reached for this feature."),
            "INSUFFICIENT_PERMISSIONS" => ("03", "You don't have permission to perform this action."),
            "ORGADMIN_REQUIRED" => ("03", "OrgAdmin access required."),
            "PLATFORM_ADMIN_REQUIRED" => ("03", "PlatformAdmin access required."),
            "VALIDATION_ERROR" => ("96", "Validation error."),
            "INTERNAL_ERROR" => ("98", "An unexpected error occurred."),
            _ => ("99", "Unknown error.")
        };

        return (code, description);
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
                    ResponseDescription = description
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
}
