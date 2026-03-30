namespace BillingService.Application.DTOs.Subscriptions;

public record DowngradeSubscriptionRequest(Guid NewPlanId);
