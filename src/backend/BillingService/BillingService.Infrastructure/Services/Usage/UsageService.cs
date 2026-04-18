using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Services.Usage;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using BillingService.Infrastructure.Redis;

namespace BillingService.Infrastructure.Services.Usage;

public class UsageService : IUsageService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IPlanRepository _planRepo;
    private readonly ILogger<UsageService> _logger;

    public UsageService(
        IConnectionMultiplexer redis,
        ISubscriptionRepository subscriptionRepo,
        IPlanRepository planRepo,
        ILogger<UsageService> logger)
    {
        _redis = redis;
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _logger = logger;
    }

    public async Task<object> GetUsageAsync(Guid organizationId, CancellationToken ct)
    {
        var plan = await GetCurrentPlan(organizationId, ct);
        var db = _redis.GetDatabase();
        var metrics = new List<UsageMetric>();

        foreach (var metricName in MetricName.All)
        {
            long currentValue = 0;
            try
            {
                var val = await db.StringGetAsync(RedisKeys.Usage(organizationId, metricName));
                if (val.HasValue && long.TryParse(val, out var parsed))
                    currentValue = parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read usage counter {Metric} for {OrgId}", metricName, organizationId);
            }

            var limit = GetLimitForMetric(plan, metricName);
            var percentUsed = limit > 0 ? Math.Round((double)currentValue / limit * 100, 2) : 0;
            metrics.Add(new UsageMetric(metricName, currentValue, limit, percentUsed));
        }

        return new UsageResponse(metrics);
    }

    public async Task IncrementAsync(Guid organizationId, string metricName, long value, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Usage(organizationId, metricName);
        await db.StringIncrementAsync(key, value);
        await db.KeyExpireAsync(key, TimeSpan.FromMinutes(5), ExpireWhen.HasNoExpiry);
    }

    private async Task<Plan> GetCurrentPlan(Guid organizationId, CancellationToken ct)
    {
        var subscription = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct);
        if (subscription?.Plan is not null) return subscription.Plan;
        if (subscription is not null)
        {
            var plan = await _planRepo.GetByIdAsync(subscription.PlanId, ct);
            if (plan is not null) return plan;
        }
        // Default to free plan
        return await _planRepo.GetByCodeAsync("free", ct) ?? new Plan
        {
            PlanName = "Free", PlanCode = "free", TierLevel = 0,
            MaxTeamMembers = 5, MaxDepartments = 3, MaxStoriesPerMonth = 50
        };
    }

    private static int GetLimitForMetric(Plan plan, string metricName) => metricName switch
    {
        MetricName.ActiveMembers => plan.MaxTeamMembers,
        MetricName.StoriesCreated => plan.MaxStoriesPerMonth,
        MetricName.StorageBytes => 0, // no storage limit defined in plan
        _ => 0
    };
}
