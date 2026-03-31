namespace BillingService.Domain.Interfaces.Services.Stripe;

public interface IStripePaymentService
{
    Task<(string customerId, string subscriptionId)> CreateSubscriptionAsync(Guid organizationId, string planCode, decimal priceMonthly, string? paymentMethodToken, CancellationToken ct);
    Task UpdateSubscriptionAsync(string externalSubscriptionId, string newPlanCode, decimal newPriceMonthly, CancellationToken ct);
    Task CancelSubscriptionAtPeriodEndAsync(string externalSubscriptionId, CancellationToken ct);
    bool VerifyWebhookSignature(string payload, string signatureHeader, out object? stripeEvent);
}
