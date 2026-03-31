using System.Text.Json;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Admin;
using BillingService.Application.DTOs.Plans;
using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Repositories.UsageRecords;
using BillingService.Domain.Interfaces.Services.AdminBilling;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Services.AdminBilling;

public class AdminBillingService : IAdminBillingService
{
    private readonly BillingDbContext _dbContext;
    private readonly IPlanRepository _planRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IUsageRecordRepository _usageRecordRepo;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<AdminBillingService> _logger;

    public AdminBillingService(
        BillingDbContext dbContext,
        IPlanRepository planRepo,
        ISubscriptionRepository subscriptionRepo,
        IUsageRecordRepository usageRecordRepo,
        IStripePaymentService stripePaymentService,
        IOutboxService outboxService,
        ILogger<AdminBillingService> logger)
    {
        _dbContext = dbContext;
        _planRepo = planRepo;
        _subscriptionRepo = subscriptionRepo;
        _usageRecordRepo = usageRecordRepo;
        _stripePaymentService = stripePaymentService;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task<object> GetAllSubscriptionsAsync(string? status, string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Search by organizationId string as fallback since we don't have ProfileService dependency
            query = query.Where(s => s.OrganizationId.ToString().Contains(search.ToLower()));
        }

        var totalCount = await query.CountAsync(ct);

        var subscriptions = await query
            .OrderByDescending(s => s.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = subscriptions.Select(s => new AdminSubscriptionListItem(
            s.SubscriptionId,
            s.OrganizationId,
            s.OrganizationId.ToString(), // Use organizationId as fallback for org name
            s.PlanId,
            s.Plan?.PlanName ?? "Unknown",
            s.Status,
            s.CurrentPeriodStart,
            s.CurrentPeriodEnd,
            s.TrialEndDate
        )).ToList();

        return new PaginatedResponse<AdminSubscriptionListItem>(items, totalCount, page, pageSize);
    }

    public async Task<object> GetOrganizationBillingAsync(Guid organizationId, CancellationToken ct)
    {
        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Include(s => s.ScheduledPlan)
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct)
            ?? throw new SubscriptionNotFoundException();

        var plan = subscription.Plan ?? throw new PlanNotFoundException();

        // Get current usage records for this org
        var usageRecords = await _dbContext.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId && u.PeriodStart >= subscription.CurrentPeriodStart)
            .ToListAsync(ct);

        var metrics = BuildUsageMetrics(usageRecords, plan);

        var subscriptionResponse = new SubscriptionResponse(
            subscription.SubscriptionId,
            subscription.OrganizationId,
            subscription.PlanId,
            plan.PlanName,
            plan.PlanCode,
            subscription.Status,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialEndDate,
            subscription.CancelledAt,
            subscription.ScheduledPlanId,
            subscription.ScheduledPlan?.PlanName);

        var planResponse = new PlanResponse(
            plan.PlanId, plan.PlanName, plan.PlanCode, plan.TierLevel,
            plan.MaxTeamMembers, plan.MaxDepartments, plan.MaxStoriesPerMonth,
            plan.FeaturesJson, plan.PriceMonthly, plan.PriceYearly);

        var usageResponse = new UsageResponse(metrics.Select(m =>
            new UsageMetric(m.MetricName, m.CurrentValue, (int)m.Limit, m.PercentUsed)).ToList());

        return new AdminOrganizationBillingResponse(subscriptionResponse, planResponse, usageResponse);
    }

    public async Task<object> OverrideSubscriptionAsync(Guid organizationId, Guid planId, string? reason, Guid adminId, CancellationToken ct)
    {
        var plan = await _planRepo.GetByIdAsync(planId, ct)
            ?? throw new PlanNotFoundException();

        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);

        string? oldPlanJson = null;

        if (subscription is not null)
        {
            oldPlanJson = JsonSerializer.Serialize(new { planId = subscription.PlanId, planName = subscription.Plan?.PlanName });

            subscription.PlanId = plan.PlanId;
            subscription.Plan = plan;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStart = DateTime.UtcNow;
            subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
            subscription.CancelledAt = null;
            subscription.ScheduledPlanId = null;
            subscription.DateUpdated = DateTime.UtcNow;

            await _subscriptionRepo.UpdateAsync(subscription, ct);
        }
        else
        {
            subscription = new Subscription
            {
                OrganizationId = organizationId,
                PlanId = plan.PlanId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            };

            subscription = await _subscriptionRepo.CreateAsync(subscription, ct);
        }

        var newValueJson = JsonSerializer.Serialize(new
        {
            planId = plan.PlanId,
            planName = plan.PlanName,
            reason,
            adminId
        });

        await _outboxService.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            OrganizationId = organizationId,
            Action = "SubscriptionOverride",
            EntityType = "Subscription",
            EntityId = subscription.SubscriptionId.ToString(),
            OldValue = oldPlanJson,
            NewValue = newValueJson,
        }, ct);

        return subscription;
    }

    public async Task<object> AdminCancelSubscriptionAsync(Guid organizationId, string? reason, Guid adminId, CancellationToken ct)
    {
        var subscription = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);

        if (subscription is null || subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Trialing))
            throw new NoActiveSubscriptionException();

        var oldStatus = subscription.Status;
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.DateUpdated = DateTime.UtcNow;

        // Cancel Stripe subscription if external ID exists
        if (!string.IsNullOrEmpty(subscription.ExternalSubscriptionId))
        {
            try
            {
                await _stripePaymentService.CancelSubscriptionAtPeriodEndAsync(subscription.ExternalSubscriptionId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel Stripe subscription {ExternalSubscriptionId} for org {OrganizationId}. Local cancellation will proceed.",
                    subscription.ExternalSubscriptionId, organizationId);
            }
        }

        await _subscriptionRepo.UpdateAsync(subscription, ct);

        var newValueJson = JsonSerializer.Serialize(new
        {
            reason,
            adminId,
            previousStatus = oldStatus
        });

        await _outboxService.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            OrganizationId = organizationId,
            Action = "AdminCancellation",
            EntityType = "Subscription",
            EntityId = subscription.SubscriptionId.ToString(),
            OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
            NewValue = newValueJson,
        }, ct);

        return subscription;
    }

    public async Task<object> GetUsageSummaryAsync(CancellationToken ct)
    {
        // Get all subscriptions for plan tier breakdown
        var subscriptions = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
            .ToListAsync(ct);

        // Get all current period usage records across all orgs
        // Use the earliest current period start from active subscriptions, or fallback to start of current month
        var periodStart = subscriptions.Any()
            ? subscriptions.Min(s => s.CurrentPeriodStart)
            : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usageRecords = await _dbContext.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.PeriodStart >= periodStart)
            .ToListAsync(ct);

        var totalActiveMembers = usageRecords
            .Where(u => u.MetricName == "active_members")
            .Sum(u => u.MetricValue);

        var totalStoriesCreated = usageRecords
            .Where(u => u.MetricName == "stories_created")
            .Sum(u => u.MetricValue);

        var totalStorageBytes = usageRecords
            .Where(u => u.MetricName == "storage_bytes")
            .Sum(u => u.MetricValue);

        var byPlanTier = subscriptions
            .Where(s => s.Plan is not null)
            .GroupBy(s => new { s.Plan!.PlanName, s.Plan.PlanCode })
            .Select(g => new PlanTierBreakdown(g.Key.PlanName, g.Key.PlanCode, g.Count()))
            .OrderBy(b => b.PlanName)
            .ToList();

        return new AdminUsageSummaryResponse(totalActiveMembers, totalStoriesCreated, totalStorageBytes, byPlanTier);
    }

    public async Task<object> GetOrganizationUsageListAsync(int? threshold, int page, int pageSize, CancellationToken ct)
    {
        // Get all active/trialing subscriptions with plans
        var subscriptions = await _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
            .ToListAsync(ct);

        // Get all current period usage records
        var periodStart = subscriptions.Any()
            ? subscriptions.Min(s => s.CurrentPeriodStart)
            : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usageRecords = await _dbContext.UsageRecords
            .IgnoreQueryFilters()
            .Where(u => u.PeriodStart >= periodStart)
            .ToListAsync(ct);

        var usageByOrg = usageRecords.GroupBy(u => u.OrganizationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var orgUsageItems = new List<AdminOrganizationUsageItem>();

        foreach (var sub in subscriptions)
        {
            if (sub.Plan is null) continue;

            var orgRecords = usageByOrg.GetValueOrDefault(sub.OrganizationId, new List<UsageRecord>());
            var metrics = BuildUsageMetrics(orgRecords, sub.Plan);

            // Apply threshold filter if provided
            if (threshold.HasValue && !metrics.Any(m => m.PercentUsed >= threshold.Value))
                continue;

            orgUsageItems.Add(new AdminOrganizationUsageItem(
                sub.OrganizationId,
                sub.OrganizationId.ToString(), // Use organizationId as fallback for org name
                sub.Plan.PlanName,
                metrics
            ));
        }

        var totalCount = orgUsageItems.Count;
        var pagedItems = orgUsageItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponse<AdminOrganizationUsageItem>(pagedItems, totalCount, page, pageSize);
    }

    private static List<UsageMetricWithLimit> BuildUsageMetrics(List<UsageRecord> records, Plan plan)
    {
        var metricDefinitions = new (string Name, Func<Plan, long> GetLimit)[]
        {
            ("active_members", p => p.MaxTeamMembers),
            ("stories_created", p => p.MaxStoriesPerMonth),
            ("storage_bytes", p => 0) // No storage limit defined in plan
        };

        return metricDefinitions.Select(def =>
        {
            var currentValue = records
                .Where(r => r.MetricName == def.Name)
                .Sum(r => r.MetricValue);
            var limit = def.GetLimit(plan);
            var percentUsed = limit > 0
                ? Math.Round((double)currentValue / limit * 100, 2)
                : 0;

            return new UsageMetricWithLimit(def.Name, currentValue, limit, percentUsed);
        }).ToList();
    }
}
