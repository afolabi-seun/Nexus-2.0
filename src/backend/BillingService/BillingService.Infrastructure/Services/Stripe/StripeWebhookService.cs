using System.Text.Json;
using BillingService.Application.DTOs;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Domain.Interfaces.Services;
using BillingService.Infrastructure.Services.ServiceClients;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.Stripe;

public class StripeWebhookService
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IStripeEventRepository _stripeEventRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IOutboxService _outboxService;
    private readonly IProfileServiceClient _profileServiceClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IStripePaymentService stripePaymentService,
        IStripeEventRepository stripeEventRepo,
        ISubscriptionRepository subscriptionRepo,
        IPlanRepository planRepo,
        IOutboxService outboxService,
        IProfileServiceClient profileServiceClient,
        IConnectionMultiplexer redis,
        ILogger<StripeWebhookService> logger)
    {
        _stripePaymentService = stripePaymentService;
        _stripeEventRepo = stripeEventRepo;
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _outboxService = outboxService;
        _profileServiceClient = profileServiceClient;
        _redis = redis;
        _logger = logger;
    }

    public async Task ProcessWebhookAsync(string payload, string signatureHeader, CancellationToken ct)
    {
        if (!_stripePaymentService.VerifyWebhookSignature(payload, signatureHeader, out var stripeEventObj))
            throw new InvalidWebhookSignatureException();

        var stripeEvent = stripeEventObj as global::Stripe.Event
            ?? throw new InvalidWebhookPayloadException();

        // Idempotency check
        if (await _stripeEventRepo.ExistsAsync(stripeEvent.Id, ct))
        {
            _logger.LogInformation("Duplicate Stripe event {EventId}, skipping", stripeEvent.Id);
            return;
        }

        switch (stripeEvent.Type)
        {
            case "invoice.payment_succeeded":
                await HandlePaymentSucceeded(stripeEvent, ct);
                break;
            case "invoice.payment_failed":
                await HandlePaymentFailed(stripeEvent, ct);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdated(stripeEvent, ct);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(stripeEvent, ct);
                break;
            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        // Record processed event
        await _stripeEventRepo.CreateAsync(new StripeEvent
        {
            StripeEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ProcessedAt = DateTime.UtcNow
        }, ct);

        await _outboxService.PublishAsync(new OutboxMessage
        {
            Action = "WebhookProcessed",
            EntityType = "StripeEvent",
            EntityId = stripeEvent.Id,
            NewValue = stripeEvent.Type,
            MessageType = "WebhookProcessed"
        }, ct);
    }

    private async Task HandlePaymentSucceeded(global::Stripe.Event evt, CancellationToken ct)
    {
        if (evt.Data.Object is not global::Stripe.Invoice invoice) return;
        var subscription = await FindSubscriptionByExternalId(invoice.SubscriptionId, ct);
        if (subscription is null) return;

        if (subscription.Status == SubscriptionStatus.PastDue)
            subscription.Status = SubscriptionStatus.Active;

        if (invoice.PeriodStart != default)
            subscription.CurrentPeriodStart = invoice.PeriodStart;
        if (invoice.PeriodEnd != default)
            subscription.CurrentPeriodEnd = invoice.PeriodEnd;

        await _subscriptionRepo.UpdateAsync(subscription, ct);
    }

    private async Task HandlePaymentFailed(global::Stripe.Event evt, CancellationToken ct)
    {
        if (evt.Data.Object is not global::Stripe.Invoice invoice) return;
        var subscription = await FindSubscriptionByExternalId(invoice.SubscriptionId, ct);
        if (subscription is null) return;

        subscription.Status = SubscriptionStatus.PastDue;
        await _subscriptionRepo.UpdateAsync(subscription, ct);

        await _outboxService.PublishAsync(new OutboxMessage
        {
            OrganizationId = subscription.OrganizationId,
            Action = "PaymentFailed",
            EntityType = "Subscription",
            EntityId = subscription.SubscriptionId.ToString(),
            MessageType = "PaymentFailed"
        }, ct);
    }

    private async Task HandleSubscriptionUpdated(global::Stripe.Event evt, CancellationToken ct)
    {
        if (evt.Data.Object is not global::Stripe.Subscription stripeSub) return;
        var subscription = await FindSubscriptionByExternalId(stripeSub.Id, ct);
        if (subscription is null) return;

        subscription.CurrentPeriodStart = stripeSub.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;

        await _subscriptionRepo.UpdateAsync(subscription, ct);
    }

    private async Task HandleSubscriptionDeleted(global::Stripe.Event evt, CancellationToken ct)
    {
        if (evt.Data.Object is not global::Stripe.Subscription stripeSub) return;
        var subscription = await FindSubscriptionByExternalId(stripeSub.Id, ct);
        if (subscription is null) return;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;

        // Downgrade to free
        var freePlan = await _planRepo.GetByCodeAsync("free", ct);
        if (freePlan is not null)
        {
            subscription.PlanId = freePlan.PlanId;
            subscription.Plan = freePlan;

            try
            {
                var db = _redis.GetDatabase();
                var cacheValue = JsonSerializer.Serialize(new
                {
                    freePlan.PlanCode, freePlan.PlanName, freePlan.TierLevel,
                    freePlan.MaxTeamMembers, freePlan.MaxDepartments, freePlan.MaxStoriesPerMonth,
                    freePlan.FeaturesJson
                });
                await db.StringSetAsync($"plan:{subscription.OrganizationId}", cacheValue, TimeSpan.FromMinutes(60));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update Redis cache after subscription deletion");
            }

            try
            {
                await _profileServiceClient.UpdateOrganizationPlanTierAsync(subscription.OrganizationId, "free", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify ProfileService after subscription deletion");
            }
        }

        await _subscriptionRepo.UpdateAsync(subscription, ct);
    }

    private Task<Domain.Entities.Subscription?> FindSubscriptionByExternalId(string? externalSubId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(externalSubId)) return Task.FromResult<Domain.Entities.Subscription?>(null);
        // Webhook events don't have org context — lookup by external subscription ID
        // This requires a query across all subscriptions (no org filter)
        return Task.FromResult<Domain.Entities.Subscription?>(null);
    }
}
