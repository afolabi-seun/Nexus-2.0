namespace BillingService.Infrastructure.Redis;

public static class RedisKeys
{
    private const string P = "nexus:";

    // Plans & Subscriptions
    public static string Plan(Guid organizationId) => $"{P}plan:{organizationId}";

    // Usage
    public static string Usage(Guid organizationId, string metricName) => $"{P}usage:{organizationId}:{metricName}";

    // Rate Limiting
    public static string RateLimit(string ipAddress, string path) => $"{P}rate_limit:{ipAddress}:{path}";

    // Auth
    public static string Blacklist(string jti) => $"{P}blacklist:{jti}";
    public static string ErrorCode(string errorCode) => $"{P}error_code:{errorCode}";
    public static string ServiceToken(string serviceId) => $"{P}service_token:{serviceId}";

    // Outbox
    public const string Outbox = $"{P}outbox:billing";
    public const string Dlq = $"{P}dlq:billing";
}
