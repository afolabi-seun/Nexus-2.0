namespace ProfileService.Infrastructure.Redis;

public static class RedisKeys
{
    private const string P = "nexus:";

    // Departments
    public static string DeptList(Guid organizationId) => $"{P}dept_list:{organizationId}";
    public static string DeptListPaged(Guid organizationId, int page, int pageSize) => $"{P}dept_list:{organizationId}:{page}:{pageSize}";
    public static string DeptPrefs(Guid departmentId) => $"{P}dept_prefs:{departmentId}";

    // Organizations
    public static string OrgSettings(Guid organizationId) => $"{P}org_settings:{organizationId}";

    // Preferences
    public static string UserPrefs(Guid userId) => $"{P}user_prefs:{userId}";
    public static string ResolvedPrefs(Guid userId) => $"{P}resolved_prefs:{userId}";

    // Members
    public static string MemberProfile(Guid memberId) => $"{P}member_profile:{memberId}";

    // Rate Limiting
    public static string RateLimit(string ipAddress, string path) => $"{P}rate_limit:{ipAddress}:{path}";

    // Auth
    public static string Blacklist(string jti) => $"{P}blacklist:{jti}";
    public static string ErrorCode(string errorCode) => $"{P}error_code:{errorCode}";

    // Outbox
    public const string Outbox = $"{P}outbox:profile";
    public const string Dlq = $"{P}dlq:profile";
}
