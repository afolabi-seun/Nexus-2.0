using BillingService.Domain.Interfaces.Services;

namespace BillingService.Infrastructure.Services.ErrorCodeResolver;

public class ErrorCodeResolverService : IErrorCodeResolverService
{
    public Task<(string responseCode, string responseDescription)> ResolveAsync(string errorCode, CancellationToken ct)
    {
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
            "VALIDATION_ERROR" => ("96", "Validation error."),
            "INTERNAL_ERROR" => ("98", "An unexpected error occurred."),
            _ => ("99", "Unknown error.")
        };

        return Task.FromResult((code, description));
    }
}
