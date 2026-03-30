using System.Text.Json;
using BillingService.Application.DTOs;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Domain.Interfaces.Services;
using BillingService.Infrastructure.Services.ServiceClients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.BackgroundServices;

public class TrialExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrialExpiryHostedService> _logger;

    public TrialExpiryHostedService(IServiceScopeFactory scopeFactory, ILogger<TrialExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredTrials(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired trials");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessExpiredTrials(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var planRepo = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var profileClient = scope.ServiceProvider.GetRequiredService<IProfileServiceClient>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var expiredTrials = await subscriptionRepo.GetExpiredTrialsAsync(DateTime.UtcNow, ct);
        _logger.LogInformation("Found {Count} expired trials to process", expiredTrials.Count);

        foreach (var subscription in expiredTrials)
        {
            try
            {
                var hasPaymentMethod = !string.IsNullOrEmpty(subscription.ExternalCustomerId);

                if (hasPaymentMethod)
                {
                    subscription.Status = SubscriptionStatus.Active;
                    subscription.TrialEndDate = null;
                }
                else
                {
                    subscription.Status = SubscriptionStatus.Expired;
                    subscription.TrialEndDate = null;

                    var freePlan = await planRepo.GetByCodeAsync("free", ct);
                    if (freePlan is not null)
                    {
                        subscription.PlanId = freePlan.PlanId;
                        subscription.Plan = freePlan;

                        var db = redis.GetDatabase();
                        var cacheValue = JsonSerializer.Serialize(new
                        {
                            freePlan.PlanCode, freePlan.PlanName, freePlan.TierLevel,
                            freePlan.MaxTeamMembers, freePlan.MaxDepartments, freePlan.MaxStoriesPerMonth,
                            freePlan.FeaturesJson
                        });
                        await db.StringSetAsync($"plan:{subscription.OrganizationId}", cacheValue, TimeSpan.FromMinutes(60));

                        try { await profileClient.UpdateOrganizationPlanTierAsync(subscription.OrganizationId, "free", ct); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Failed to notify ProfileService for trial expiry"); }
                    }

                    await outboxService.PublishAsync(new OutboxMessage
                    {
                        OrganizationId = subscription.OrganizationId,
                        Action = "TrialExpired",
                        EntityType = "Subscription",
                        EntityId = subscription.SubscriptionId.ToString(),
                        NewValue = subscription.Plan?.PlanName ?? "Unknown",
                        MessageType = "TrialExpired"
                    }, ct);
                }

                await subscriptionRepo.UpdateAsync(subscription, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trial expiry for subscription {SubId}", subscription.SubscriptionId);
            }
        }
    }
}
