using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProfileService.Application.DTOs.Preferences;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;
using ProfileService.Domain.Interfaces.Services.Preferences;
using ProfileService.Domain.Results;
using StackExchange.Redis;
using ProfileService.Infrastructure.Redis;

namespace ProfileService.Infrastructure.Services.Preferences;

public class PreferenceResolver : IPreferenceResolver
{
    private static readonly TimeSpan OrgSettingsTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DeptPrefsTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan UserPrefsTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResolvedTtl = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IUserPreferencesRepository _userPrefsRepo;
    private readonly ILogger<PreferenceResolver> _logger;

    public PreferenceResolver(
        IConnectionMultiplexer redis,
        IOrganizationRepository orgRepo,
        IDepartmentRepository deptRepo,
        IUserPreferencesRepository userPrefsRepo,
        ILogger<PreferenceResolver> logger)
    {
        _redis = redis;
        _orgRepo = orgRepo;
        _deptRepo = deptRepo;
        _userPrefsRepo = userPrefsRepo;
        _logger = logger;
    }

    public async Task<ServiceResult<object>> ResolveAsync(Guid userId, Guid departmentId, Guid organizationId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // 1. Check resolved cache
        var cachedResolved = await db.StringGetAsync(RedisKeys.ResolvedPrefs(userId));
        if (cachedResolved.HasValue)
        {
            var cached = JsonSerializer.Deserialize<ResolvedPreferencesResponse>(cachedResolved!, JsonOptions);
            if (cached is not null) return ServiceResult<object>.Ok(cached);
        }

        // 2. Load all three levels with tiered caching
        var (orgSettings, orgTimeZone) = await GetOrgSettingsCachedAsync(db, organizationId, ct);
        var deptPrefs = await GetDeptPrefsCachedAsync(db, departmentId, ct);
        var userPrefs = await GetUserPrefsCachedAsync(db, userId, ct);

        // 3. Merge: User > Department > Organization > System Default
        var resolved = new ResolvedPreferencesResponse
        {
            Theme = userPrefs?.Theme ?? SystemDefaults.Theme,
            Language = userPrefs?.Language ?? SystemDefaults.Language,
            Timezone = userPrefs?.TimezoneOverride ?? orgTimeZone ?? SystemDefaults.Timezone,
            DefaultBoardView = userPrefs?.DefaultBoardView ?? orgSettings?.DefaultBoardView ?? SystemDefaults.DefaultBoardView,
            DigestFrequency = userPrefs?.EmailDigestFrequency ?? orgSettings?.DigestFrequency ?? SystemDefaults.DigestFrequency,
            NotificationChannels = deptPrefs?.NotificationChannelOverrides?.ToString()
                ?? orgSettings?.DefaultNotificationChannels
                ?? SystemDefaults.NotificationChannels,
            KeyboardShortcutsEnabled = userPrefs?.KeyboardShortcutsEnabled ?? SystemDefaults.KeyboardShortcutsEnabled,
            DateFormat = userPrefs?.DateFormat ?? SystemDefaults.DateFormat,
            TimeFormat = userPrefs?.TimeFormat ?? SystemDefaults.TimeFormat,
            StoryPointScale = orgSettings?.StoryPointScale ?? SystemDefaults.StoryPointScale,
            AutoAssignmentEnabled = orgSettings?.AutoAssignmentEnabled ?? SystemDefaults.AutoAssignmentEnabled,
            AutoAssignmentStrategy = orgSettings?.AutoAssignmentStrategy ?? SystemDefaults.AutoAssignmentStrategy,
            WipLimitsEnabled = orgSettings?.WipLimitsEnabled ?? SystemDefaults.WipLimitsEnabled,
            DefaultWipLimit = orgSettings?.DefaultWipLimit ?? SystemDefaults.DefaultWipLimit,
            AuditRetentionDays = orgSettings?.AuditRetentionDays ?? SystemDefaults.AuditRetentionDays,
            MaxConcurrentTasksDefault = deptPrefs?.MaxConcurrentTasksDefault ?? SystemDefaults.MaxConcurrentTasksDefault,
        };

        // 4. Cache resolved result
        await db.StringSetAsync(
            RedisKeys.ResolvedPrefs(userId),
            JsonSerializer.Serialize(resolved, JsonOptions),
            ResolvedTtl);

        return ServiceResult<object>.Ok(resolved, "Preferences resolved.");
    }

    private async Task<(OrganizationSettings? Settings, string? TimeZone)> GetOrgSettingsCachedAsync(IDatabase db, Guid organizationId, CancellationToken ct)
    {
        var cacheKey = RedisKeys.OrgSettings(organizationId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedData = JsonSerializer.Deserialize<OrgSettingsCache>(cached!, JsonOptions);
            if (cachedData is not null)
                return (cachedData.Settings, cachedData.TimeZone);
        }

        var org = await _orgRepo.GetByIdAsync(organizationId, ct);
        if (org is null) return (null, null);

        var settings = !string.IsNullOrEmpty(org.SettingsJson)
            ? JsonSerializer.Deserialize<OrganizationSettings>(org.SettingsJson, JsonOptions)
            : new OrganizationSettings();

        var cacheData = new OrgSettingsCache { Settings = settings, TimeZone = org.TimeZone };
        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(cacheData, JsonOptions), OrgSettingsTtl);

        return (settings, org.TimeZone);
    }

    private async Task<DepartmentPreferences?> GetDeptPrefsCachedAsync(IDatabase db, Guid departmentId, CancellationToken ct)
    {
        var cacheKey = RedisKeys.DeptPrefs(departmentId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<DepartmentPreferences>(cached!, JsonOptions);
        }

        var dept = await _deptRepo.GetByIdAsync(departmentId, ct);
        if (dept is null) return null;

        var prefs = !string.IsNullOrEmpty(dept.PreferencesJson)
            ? JsonSerializer.Deserialize<DepartmentPreferences>(dept.PreferencesJson, JsonOptions)
            : new DepartmentPreferences();

        if (prefs is not null)
        {
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(prefs, JsonOptions), DeptPrefsTtl);
        }

        return prefs;
    }

    private async Task<UserPreferences?> GetUserPrefsCachedAsync(IDatabase db, Guid userId, CancellationToken ct)
    {
        var cacheKey = RedisKeys.UserPrefs(userId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<UserPreferences>(cached!, JsonOptions);
        }

        var prefs = await _userPrefsRepo.GetByMemberIdAsync(userId, ct);
        if (prefs is not null)
        {
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(prefs, JsonOptions), UserPrefsTtl);
        }

        return prefs;
    }

    private class OrgSettingsCache
    {
        public OrganizationSettings? Settings { get; set; }
        public string? TimeZone { get; set; }
    }
}
