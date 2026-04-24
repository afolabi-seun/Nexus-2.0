using System.Collections.Concurrent;
using StackExchange.Redis;
using UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;
using UtilityService.Domain.Interfaces.Services.ErrorCodeResolver;
using DomainErrorCodes = UtilityService.Domain.Exceptions.ErrorCodes;
using UtilityService.Infrastructure.Redis;
using Microsoft.Extensions.Logging;

namespace UtilityService.Infrastructure.Services.ErrorCodeResolver;

public class ErrorCodeResolverService : IErrorCodeResolverService
{
    private readonly IErrorCodeEntryRepository _repo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ErrorCodeResolverService> _logger;
    private static readonly string CacheKey = RedisKeys.ErrorCodesRegistry;

    private readonly ConcurrentDictionary<string, (string ResponseCode, string ResponseDescription)>
        _memoryCache = new();

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

    public ErrorCodeResolverService(
        IErrorCodeEntryRepository repo,
        IConnectionMultiplexer redis,
        ILogger<ErrorCodeResolverService> logger)
    {
        _repo = repo;
        _redis = redis;
        _logger = logger;
    }

    public async Task<(string ResponseCode, string ResponseDescription)> ResolveAsync(string errorCode, CancellationToken ct = default)
    {
        // Tier 1: In-memory cache
        if (_memoryCache.TryGetValue(errorCode, out var memoryCached))
            return memoryCached;

        // Tier 2: Redis cache
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.HashGetAsync(CacheKey, errorCode);
            if (cached.HasValue)
            {
                var parts = cached.ToString().Split('|', 2);
                if (parts.Length == 2)
                {
                    var redisValue = (parts[0], parts[1]);
                    _memoryCache.TryAdd(errorCode, redisValue);
                    return redisValue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache lookup failed for error code {ErrorCode}.", errorCode);
        }

        // Tier 3: Database
        try
        {
            var entry = await _repo.GetByCodeAsync(errorCode, ct);
            if (entry != null)
            {
                var dbValue = (entry.ResponseCode, entry.Description);
                _memoryCache.TryAdd(errorCode, dbValue);

                try
                {
                    var db = _redis.GetDatabase();
                    await db.HashSetAsync(CacheKey, errorCode, $"{entry.ResponseCode}|{entry.Description}");
                    await db.KeyExpireAsync(CacheKey, TimeSpan.FromHours(24));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to populate Redis cache for error code {ErrorCode}.", errorCode);
                }

                return dbValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database lookup failed for error code {ErrorCode}.", errorCode);
        }

        // Tier 4: Static fallback
        if (StaticFallback.TryGetValue(errorCode, out var fallback))
            return fallback;

        return ("99", errorCode);
    }

    public async Task RefreshCacheAsync(CancellationToken ct = default)
    {
        var entries = await _repo.ListAsync(ct);

        _memoryCache.Clear();
        foreach (var entry in entries)
        {
            _memoryCache.TryAdd(entry.Code, (entry.ResponseCode, entry.Description));
        }

        try
        {
            var db = _redis.GetDatabase();
            var hashEntries = entries.Select(e =>
                new HashEntry(e.Code, $"{e.ResponseCode}|{e.Description}")).ToArray();

            await db.KeyDeleteAsync(CacheKey);
            if (hashEntries.Length > 0)
            {
                await db.HashSetAsync(CacheKey, hashEntries);
                await db.KeyExpireAsync(CacheKey, TimeSpan.FromHours(24));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate Redis cache during refresh.");
        }
    }

    public void ClearMemoryCache() => _memoryCache.Clear();
}
