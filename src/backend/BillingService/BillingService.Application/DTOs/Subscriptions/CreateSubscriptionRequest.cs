namespace BillingService.Application.DTOs.Subscriptions;

public record CreateSubscriptionRequest(Guid PlanId, string? PaymentMethodToken);
