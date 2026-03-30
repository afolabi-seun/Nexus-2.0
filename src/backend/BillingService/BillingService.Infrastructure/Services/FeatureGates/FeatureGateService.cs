using System.Text.Json;
using BillingService.Application.DTOs.FeatureGates;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.FeatureGates;

public class FeatureGateService : IFeatureGateService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<FeatureGateService> _logger;

    public FeatureGateService(
        ISubscriptionRepository subscriptionRepo,
        IPlanRepository planRepo,
        IConnectionMultiplexer redis,
        ILogger<FeatureGateService> logger)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _redis = redis;
        _logger = logger;
    }

    public async Task<object> CheckFeatureAsync(Guid organizationId, string feature, CancellationToken ct)
    {
        var plan = await GetPlanFromCacheOrDb(organizationId, ct);
        if (plan is null)
        {
            // Default to free plan limits
            plan = await _planRepo.GetByCodeAsync("free", ct);
        }

        var (limit, currentUsage) = await GetFeatureLimitAndUsage(organizationId, feature, plan!, ct);

        // 0 means unlimited
        var allowed = limit == 0 || currentUsage < limit;

        return new FeatureGateResponse(allowed, currentUsage, limit, feature);
    }

    private async Task<Plan?> GetPlanFromCacheOrDb(Guid organizationId, CancellationToken ct)
    {
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync($"plan:{organizationId}");
            if (cached.HasValue)
            {
                var data = JsonSerializer.Deserialize<CachedPlanData>(cached!);
                if (data is not null)
                {
                    return new Plan
                    {
                        PlanCode = data.PlanCode ?? "free",
                        PlanName = data.PlanName ?? "Free",
                        TierLevel = data.TierLevel,
                        MaxTeamMembers = data.MaxTeamMembers,
                        MaxDepartments = data.MaxDepartments,
                        MaxStoriesPerMonth = data.MaxStoriesPerMonth,
                        FeaturesJson = data.FeaturesJson
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read plan cache for organization {OrganizationId}", organizationId);
        }

        // Fallback to DB
        var subscription = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct);
        if (subscription is null) return null;

        var plan = subscription.Plan ?? await _planRepo.GetByIdAsync(subscription.PlanId, ct);
        return plan;
    }

    private async Task<(int limit, long currentUsage)> GetFeatureLimitAndUsage(
        Guid organizationId, string feature, Plan plan, CancellationToken ct)
    {
        var limit = feature switch
        {
            "max_team_members" => plan.MaxTeamMembers,
            "max_departments" => plan.MaxDepartments,
            "max_stories_per_month" => plan.MaxStoriesPerMonth,
            "sprint_analytics" => GetBooleanFeatureLimit(plan.FeaturesJson, "sprintAnalytics"),
            "custom_workflows" => GetBooleanFeatureLimit(plan.FeaturesJson, "customWorkflows"),
            "priority_support" => GetBooleanFeatureLimit(plan.FeaturesJson, "prioritySupport"),
            _ => 0
        };

        long currentUsage = 0;
        try
        {
            var db = _redis.GetDatabase();
            var metricKey = feature switch
            {
                "max_team_members" => MetricName.ActiveMembers,
                "max_departments" => "departments",
                "max_stories_per_month" => MetricName.StoriesCreated,
                _ => null
            };

            if (metricKey is not null)
            {
                var val = await db.StringGetAsync($"usage:{organizationId}:{metricKey}");
                if (val.HasValue && long.TryParse(val, out var parsed))
                    currentUsage = parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read usage from Redis for {Feature}", feature);
        }

        return (limit, currentUsage);
    }

    private static int GetBooleanFeatureLimit(string? featuresJson, string key)
    {
        if (string.IsNullOrEmpty(featuresJson)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(featuresJson);
            if (doc.RootElement.TryGetProperty(key, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True) return 0; // unlimited (feature available)
                if (prop.ValueKind == JsonValueKind.False) return -1; // not available
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var val = prop.GetString();
                    return val == "none" ? -1 : 0; // "none" = not available, "basic"/"full" = available
                }
            }
        }
        catch { }
        return 0;
    }

    private record CachedPlanData(
        string? PlanCode, string? PlanName, int TierLevel,
        int MaxTeamMembers, int MaxDepartments, int MaxStoriesPerMonth,
        string? FeaturesJson);
}
