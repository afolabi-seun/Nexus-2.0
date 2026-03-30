namespace BillingService.Tests.Property.Generators;

public static class StripeEventGenerator
{
    private static readonly string[] EventTypes =
    [
        "invoice.payment_succeeded",
        "invoice.payment_failed",
        "customer.subscription.updated",
        "customer.subscription.deleted"
    ];

    public static (string EventId, string EventType) Create(int? typeIndex = null)
    {
        var idx = typeIndex ?? Random.Shared.Next(EventTypes.Length);
        return ($"evt_{Guid.NewGuid():N}", EventTypes[idx % EventTypes.Length]);
    }

    public static string RandomEventType() =>
        EventTypes[Random.Shared.Next(EventTypes.Length)];
}
