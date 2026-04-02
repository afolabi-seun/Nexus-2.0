using System.Text.Json;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Plans;
using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Domain.Interfaces.Services.Subscriptions;
using BillingService.Domain.Interfaces.Services.Usage;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Services.ServiceClients;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly BillingDbContext _dbContext;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IUsageService _usageService;
    private readonly IOutboxService _outboxService;
    private readonly IProfileServiceClient _profileServiceClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        BillingDbContext dbContext,
        ISubscriptionRepository subscriptionRepo,
        IPlanRepository planRepo,
        IStripePaymentService stripePaymentService,
        IUsageService usageService,
        IOutboxService outboxService,
        IProfileServiceClient profileServiceClient,
        IConnectionMultiplexer redis,
        ILogger<SubscriptionService> logger)
    {
        _dbContext = dbContext;
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _stripePaymentService = stripePaymentService;
        _usageService = usageService;
        _outboxService = outboxService;
        _profileServiceClient = profileServiceClient;
        _redis = redis;
        _logger = logger;
    }

    public async Task<object> GetCurrentAsync(Guid organizationId, CancellationToken ct)
    {
        var subscription = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct)
            ?? throw new SubscriptionNotFoundException();

        var plan = subscription.Plan ?? await _planRepo.GetByIdAsync(subscription.PlanId, ct)
            ?? throw new PlanNotFoundException();

        var usage = (UsageResponse)await _usageService.GetUsageAsync(organizationId, ct);

        var subResponse = MapToResponse(subscription, plan);
        var planResponse = MapPlanToResponse(plan);

        return new SubscriptionDetailResponse(subResponse, planResponse, usage);
    }

    public async Task<object> CreateAsync(Guid organizationId, object request, CancellationToken ct)
    {
        var req = (CreateSubscriptionRequest)request;

        var existing = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct);
        if (existing is not null && existing.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing)
            throw new SubscriptionAlreadyExistsException();

        var plan = await _planRepo.GetByIdAsync(req.PlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new PlanNotFoundException();

        var subscription = new Subscription
        {
            OrganizationId = organizationId,
            PlanId = plan.PlanId,
            CurrentPeriodStart = DateTime.UtcNow
        };

        if (plan.PlanCode == "free")
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodEnd = null;
        }
        else
        {
            // Paid plan — create Stripe subscription first
            var (customerId, externalSubId) = await _stripePaymentService.CreateSubscriptionAsync(
                organizationId, plan.PlanCode, plan.PriceMonthly, req.PaymentMethodToken, ct);

            subscription.ExternalCustomerId = customerId;
            subscription.ExternalSubscriptionId = externalSubId;
            subscription.Status = SubscriptionStatus.Trialing;
            subscription.TrialEndDate = DateTime.UtcNow.AddDays(14);
            subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
        }

        var created = await _subscriptionRepo.AddAsync(subscription, ct);
        await _dbContext.SaveChangesAsync(ct);
        await RefreshCacheAndNotify(organizationId, plan, ct);
        await PublishAuditEvent(organizationId, "SubscriptionCreated", plan.PlanName, ct);

        return MapToResponse(created, plan);
    }

    public async Task<object> UpgradeAsync(Guid organizationId, object request, CancellationToken ct)
    {
        var req = (UpgradeSubscriptionRequest)request;

        var subscription = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct);
        if (subscription is null || subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Trialing))
            throw new NoActiveSubscriptionException();

        var currentPlan = subscription.Plan ?? await _planRepo.GetByIdAsync(subscription.PlanId, ct)
            ?? throw new PlanNotFoundException();
        var newPlan = await _planRepo.GetByIdAsync(req.NewPlanId, ct)
            ?? throw new PlanNotFoundException();

        if (newPlan.TierLevel <= currentPlan.TierLevel)
            throw new InvalidUpgradePathException();

        // Update Stripe if not free
        if (!string.IsNullOrEmpty(subscription.ExternalSubscriptionId))
        {
            await _stripePaymentService.UpdateSubscriptionAsync(
                subscription.ExternalSubscriptionId, newPlan.PlanCode, newPlan.PriceMonthly, ct);
        }
        else if (newPlan.PlanCode != "free")
        {
            // Upgrading from Free to paid — create Stripe subscription
            var (customerId, externalSubId) = await _stripePaymentService.CreateSubscriptionAsync(
                organizationId, newPlan.PlanCode, newPlan.PriceMonthly, null, ct);
            subscription.ExternalCustomerId = customerId;
            subscription.ExternalSubscriptionId = externalSubId;
        }

        var oldPlanName = currentPlan.PlanName;
        subscription.PlanId = newPlan.PlanId;
        subscription.Plan = newPlan;

        // If trialing, end trial immediately
        if (subscription.Status == SubscriptionStatus.Trialing)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.TrialEndDate = null;
        }

        subscription.ScheduledPlanId = null;
        subscription.ScheduledPlan = null;
        subscription.CurrentPeriodStart = DateTime.UtcNow;
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);

        await _subscriptionRepo.UpdateAsync(subscription, ct);
        await _dbContext.SaveChangesAsync(ct);
        await RefreshCacheAndNotify(organizationId, newPlan, ct);
        await PublishAuditEvent(organizationId, "SubscriptionUpgraded", $"{oldPlanName} -> {newPlan.PlanName}", ct);

        return MapToResponse(subscription, newPlan);
    }

    public async Task<object> DowngradeAsync(Guid organizationId, object request, CancellationToken ct)
    {
        var req = (DowngradeSubscriptionRequest)request;

        var subscription = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct);
        if (subscription is null || subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Trialing))
            throw new NoActiveSubscriptionException();

        var currentPlan = subscription.Plan ?? await _planRepo.GetByIdAsync(subscription.PlanId, ct)
            ?? throw new PlanNotFoundException();
        var newPlan = await _planRepo.GetByIdAsync(req.NewPlanId, ct)
            ?? throw new PlanNotFoundException();

        if (newPlan.TierLevel >= currentPlan.TierLevel)
            throw new InvalidDowngradePathException();

        // Validate usage doesn't exceed new plan limits
        var usage = (UsageResponse)await _usageService.GetUsageAsync(organizationId, ct);
        ValidateUsageAgainstPlan(usage, newPlan);

        subscription.ScheduledPlanId = newPlan.PlanId;
        subscription.ScheduledPlan = newPlan;

        await _subscriptionRepo.UpdateAsync(subscription, ct);
        await _dbContext.SaveChangesAsync(ct);
        await PublishAuditEvent(organizationId, "SubscriptionDowngraded",
            $"{currentPlan.PlanName} -> {newPlan.PlanName} (effective at period end)", ct);

        return MapToResponse(subscription, currentPlan);
    }

    public async Task<object> CancelAsync(Guid organizationId, CancellationToken ct)
    {
        var subscription = await _subscriptionRepo.GetByOrganizationIdAsync(organizationId, ct);
        if (subscription is null)
            throw new NoActiveSubscriptionException();

        if (subscription.Status == SubscriptionStatus.Cancelled)
            throw new SubscriptionAlreadyCancelledException();

        if (subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Trialing))
            throw new NoActiveSubscriptionException();

        var plan = subscription.Plan ?? await _planRepo.GetByIdAsync(subscription.PlanId, ct)
            ?? throw new PlanNotFoundException();

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;

        if (plan.PlanCode != "free" && !string.IsNullOrEmpty(subscription.ExternalSubscriptionId))
        {
            await _stripePaymentService.CancelSubscriptionAtPeriodEndAsync(subscription.ExternalSubscriptionId, ct);
        }

        await _subscriptionRepo.UpdateAsync(subscription, ct);
        await _dbContext.SaveChangesAsync(ct);

        // For free plan, downgrade is immediate
        if (plan.PlanCode == "free")
        {
            await RefreshCacheAndNotify(organizationId, plan, ct);
        }

        await PublishAuditEvent(organizationId, "SubscriptionCancelled", plan.PlanName, ct);

        return MapToResponse(subscription, plan);
    }

    private async Task RefreshCacheAndNotify(Guid organizationId, Plan plan, CancellationToken ct)
    {
        try
        {
            var db = _redis.GetDatabase();
            var cacheValue = JsonSerializer.Serialize(new
            {
                plan.PlanCode,
                plan.PlanName,
                plan.TierLevel,
                plan.MaxTeamMembers,
                plan.MaxDepartments,
                plan.MaxStoriesPerMonth,
                plan.FeaturesJson
            });
            await db.StringSetAsync($"plan:{organizationId}", cacheValue, TimeSpan.FromMinutes(60));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Redis cache for organization {OrganizationId}", organizationId);
        }

        try
        {
            await _profileServiceClient.UpdateOrganizationPlanTierAsync(organizationId, plan.PlanCode, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify ProfileService for organization {OrganizationId}", organizationId);
        }
    }

    private async Task PublishAuditEvent(Guid organizationId, string action, string details, CancellationToken ct)
    {
        var message = new OutboxMessage
        {
            OrganizationId = organizationId,
            Action = action,
            EntityType = "Subscription",
            EntityId = organizationId.ToString(),
            NewValue = details,
            MessageType = action
        };
        await _outboxService.PublishAsync(message, ct);
    }

    private static void ValidateUsageAgainstPlan(UsageResponse usage, Plan plan)
    {
        var violations = new List<string>();
        foreach (var metric in usage.Metrics)
        {
            var limit = metric.MetricName switch
            {
                "active_members" => plan.MaxTeamMembers,
                "stories_created" => plan.MaxStoriesPerMonth,
                "storage_bytes" => 0, // no storage limit in plan
                _ => 0
            };
            if (limit > 0 && metric.CurrentValue > limit)
                violations.Add($"{metric.MetricName}: current={metric.CurrentValue}, limit={limit}");
        }
        if (violations.Count > 0)
            throw new UsageExceedsPlanLimitsException(string.Join("; ", violations));
    }

    private static SubscriptionResponse MapToResponse(Subscription sub, Plan plan) => new(
        sub.SubscriptionId, sub.OrganizationId, sub.PlanId,
        plan.PlanName, plan.PlanCode, sub.Status,
        sub.CurrentPeriodStart, sub.CurrentPeriodEnd,
        sub.TrialEndDate, sub.CancelledAt,
        sub.ScheduledPlanId, sub.ScheduledPlan?.PlanName);

    private static PlanResponse MapPlanToResponse(Plan p) => new(
        p.PlanId, p.PlanName, p.PlanCode, p.TierLevel,
        p.MaxTeamMembers, p.MaxDepartments, p.MaxStoriesPerMonth,
        p.FeaturesJson, p.PriceMonthly, p.PriceYearly);
}
