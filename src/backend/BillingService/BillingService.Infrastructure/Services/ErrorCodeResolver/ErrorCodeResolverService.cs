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
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.ErrorCode(errorCode);

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
            _logger.LogWarning(ex,
                "Failed to resolve error code {ErrorCode} from UtilityService. Falling back to static mapping.",
                errorCode);
        }

        // 3. Fallback to static mapping
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
}
