using StackExchange.Redis;
using UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;
using UtilityService.Domain.Interfaces.Services.ErrorCodeResolver;
using DomainErrorCodes = UtilityService.Domain.Exceptions.ErrorCodes;
using UtilityService.Infrastructure.Redis;

namespace UtilityService.Infrastructure.Services.ErrorCodeResolver;

public class ErrorCodeResolverService : IErrorCodeResolverService
{
    private readonly IErrorCodeEntryRepository _repo;
    private readonly IConnectionMultiplexer _redis;
    private static readonly string CacheKey = RedisKeys.ErrorCodesRegistry;

    private static readonly Dictionary<string, (string ResponseCode, string Description)> StaticFallback = new()
    {
        [DomainErrorCodes.ValidationError] = ("01", "Validation error"),
        [DomainErrorCodes.AuditLogImmutable] = ("02", "Audit log is immutable"),
        [DomainErrorCodes.ErrorCodeDuplicate] = ("03", "Error code already exists"),
        [DomainErrorCodes.ErrorCodeNotFound] = ("04", "Error code not found"),
        [DomainErrorCodes.NotificationDispatchFailed] = ("05", "Notification dispatch failed"),
        [DomainErrorCodes.ReferenceDataNotFound] = ("06", "Reference data not found"),
        [DomainErrorCodes.OrganizationMismatch] = ("07", "Organization mismatch"),
        [DomainErrorCodes.TemplateNotFound] = ("08", "Template not found"),
        [DomainErrorCodes.NotFound] = ("09", "Resource not found"),
        [DomainErrorCodes.Conflict] = ("10", "Conflict"),
        [DomainErrorCodes.ServiceUnavailable] = ("11", "Service unavailable"),
        [DomainErrorCodes.InvalidNotificationType] = ("12", "Invalid notification type"),
        [DomainErrorCodes.InvalidChannel] = ("13", "Invalid channel"),
        [DomainErrorCodes.RetentionPeriodInvalid] = ("14", "Retention period invalid"),
        [DomainErrorCodes.ReferenceDataDuplicate] = ("15", "Reference data duplicate"),
        [DomainErrorCodes.OutboxProcessingFailed] = ("16", "Outbox processing failed"),
        ["INSUFFICIENT_PERMISSIONS"] = ("17", "You don't have permission to perform this action."),
        ["ORGADMIN_REQUIRED"] = ("18", "OrgAdmin access required."),
        ["DEPTLEAD_REQUIRED"] = ("19", "DeptLead or higher access required."),
        ["PLATFORM_ADMIN_REQUIRED"] = ("20", "PlatformAdmin access required."),
        [DomainErrorCodes.InternalError] = ("98", "An unexpected error occurred"),
    };

    public ErrorCodeResolverService(IErrorCodeEntryRepository repo, IConnectionMultiplexer redis)
    {
        _repo = repo;
        _redis = redis;
    }

    public async Task<(string ResponseCode, string ResponseDescription)> ResolveAsync(string errorCode, CancellationToken ct = default)
    {
        // Tier 1: Redis cache
        var db = _redis.GetDatabase();
        var cached = await db.HashGetAsync(CacheKey, errorCode);
        if (cached.HasValue)
        {
            var parts = cached.ToString().Split('|', 2);
            if (parts.Length == 2) return (parts[0], parts[1]);
        }

        // Tier 2: Database
        var entry = await _repo.GetByCodeAsync(errorCode, ct);
        if (entry != null)
        {
            await db.HashSetAsync(CacheKey, errorCode, $"{entry.ResponseCode}|{entry.Description}");
            await db.KeyExpireAsync(CacheKey, TimeSpan.FromHours(24));
            return (entry.ResponseCode, entry.Description);
        }

        // Tier 3: Static fallback
        if (StaticFallback.TryGetValue(errorCode, out var fallback))
            return fallback;

        return ("99", errorCode);
    }
}
