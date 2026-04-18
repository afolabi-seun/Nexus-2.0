namespace SecurityService.Infrastructure.Redis;

public static class RedisKeys
{
    private const string P = "nexus:";

    // Auth
    public static string Lockout(string email) => $"{P}lockout:{email}";
    public static string LockoutLocked(string email) => $"{P}lockout:locked:{email}";
    public static string Refresh(Guid userId, string deviceId) => $"{P}refresh:{userId}:{deviceId}";
    public static string RefreshPattern(string deviceId) => $"{P}refresh:*:{deviceId}";
    public static string Blacklist(string jti) => $"{P}blacklist:{jti}";
    public static string Otp(string identity) => $"{P}otp:{identity}";

    // Session
    public static string Session(Guid userId, string deviceId) => $"{P}session:{userId}:{deviceId}";
    public static string SessionById(string sessionId) => $"{P}session:{sessionId}";

    // Rate Limiting
    public static string RateLimit(string identity, string endpoint) => $"{P}rate_limit:{identity}:{endpoint}";

    // Anomaly Detection
    public static string TrustedIps(Guid userId) => $"{P}trusted_ips:{userId}";

    // Service Clients
    public static string UserCache(string email) => $"{P}user_cache:{email}";
    public static string UserCachePattern => $"{P}user_cache:*";
    public static string ErrorCode(string errorCode) => $"{P}error_code:{errorCode}";
    public static string ServiceToken(string serviceId) => $"{P}service_token:{serviceId}";

    // Session patterns
    public static string SessionPattern(Guid userId) => $"{P}session:{userId}:*";
    public static string SessionForDevice(Guid userId, string currentDeviceId) => $"{P}session:{userId}:{currentDeviceId}";

    // Outbox
    public const string Outbox = $"{P}outbox:security";
    public const string Dlq = $"{P}dlq:security";
}
