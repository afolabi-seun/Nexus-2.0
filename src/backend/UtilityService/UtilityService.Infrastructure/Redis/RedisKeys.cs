namespace UtilityService.Infrastructure.Redis;

public static class RedisKeys
{
    private const string P = "nexus:";

    // Error Codes
    public const string ErrorCodesRegistry = $"{P}error_codes_registry";

    // Reference Data
    public static string Ref(string suffix) => $"{P}ref:{suffix}";

    // Notifications
    public static string DueDateNotified(string entry) => $"{P}duedate:notified:{entry}";

    // Auth
    public static string Blacklist(string jti) => $"{P}blacklist:{jti}";

    // Outbox (consumer side)
    public const string OutboxSecurity = $"{P}outbox:security";
    public const string OutboxProfile = $"{P}outbox:profile";
    public const string OutboxWork = $"{P}outbox:work";
    public const string OutboxBilling = $"{P}outbox:billing";

    public static readonly string[] AllOutboxQueues = { OutboxSecurity, OutboxProfile, OutboxWork, OutboxBilling };

    public static string DlqFor(string outboxQueue) => outboxQueue.Replace("outbox:", "dlq:");
}
