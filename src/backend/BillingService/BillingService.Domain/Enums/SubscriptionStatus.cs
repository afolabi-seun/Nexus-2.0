namespace BillingService.Domain.Enums;

public static class SubscriptionStatus
{
    public const string Active = "Active";
    public const string Trialing = "Trialing";
    public const string PastDue = "PastDue";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}
