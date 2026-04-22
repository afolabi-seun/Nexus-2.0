using ProfileService.Application.DTOs.Preferences;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;
using ProfileService.Domain.Interfaces.Services.Preferences;
using ProfileService.Domain.Results;
using ProfileService.Infrastructure.Data;
using StackExchange.Redis;
using ProfileService.Infrastructure.Redis;

namespace ProfileService.Infrastructure.Services.Preferences;

public class PreferenceService : IPreferenceService
{
    private readonly IUserPreferencesRepository _prefsRepo;
    private readonly ITeamMemberRepository _memberRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ProfileDbContext _dbContext;

    public PreferenceService(
        IUserPreferencesRepository prefsRepo,
        ITeamMemberRepository memberRepo,
        IConnectionMultiplexer redis,
        ProfileDbContext dbContext)
    {
        _prefsRepo = prefsRepo;
        _memberRepo = memberRepo;
        _redis = redis;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> GetAsync(Guid memberId, CancellationToken ct = default)
    {
        var prefs = await _prefsRepo.GetByMemberIdAsync(memberId, ct);
        if (prefs is null)
        {
            return ServiceResult<object>.Ok(new UserPreferencesResponse
            {
                Theme = "System",
                Language = "en",
                KeyboardShortcutsEnabled = true,
                DateFormat = "ISO",
                TimeFormat = "H24"
            });
        }

        return ServiceResult<object>.Ok(MapToResponse(prefs));
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid memberId, object request, CancellationToken ct = default)
    {
        var req = (UserPreferencesRequest)request;
        var prefs = await _prefsRepo.GetByMemberIdAsync(memberId, ct);

        if (prefs is null)
        {
            var member = await _memberRepo.GetByIdAsync(memberId, ct)
                ?? throw new MemberNotFoundException($"Member {memberId} not found");

            prefs = new UserPreferences
            {
                TeamMemberId = memberId,
                OrganizationId = member.OrganizationId
            };
            ApplyUpdates(prefs, req);
            await _prefsRepo.AddAsync(prefs, ct);
        }
        else
        {
            ApplyUpdates(prefs, req);
            prefs.DateUpdated = DateTime.UtcNow;
            await _prefsRepo.UpdateAsync(prefs, ct);
        }

        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.UserPrefs(memberId));
        await db.KeyDeleteAsync(RedisKeys.ResolvedPrefs(memberId));

        return ServiceResult<object>.Ok(MapToResponse(prefs), "Preferences updated.");
    }

    private static void ApplyUpdates(UserPreferences prefs, UserPreferencesRequest req)
    {
        if (req.Theme is not null) prefs.Theme = req.Theme;
        if (req.Language is not null) prefs.Language = req.Language;
        if (req.TimezoneOverride is not null) prefs.TimezoneOverride = req.TimezoneOverride;
        if (req.DefaultBoardView is not null) prefs.DefaultBoardView = req.DefaultBoardView;
        if (req.DefaultBoardFilters is not null)
            prefs.DefaultBoardFilters = System.Text.Json.JsonSerializer.Serialize(req.DefaultBoardFilters);
        if (req.DashboardLayout is not null)
            prefs.DashboardLayout = System.Text.Json.JsonSerializer.Serialize(req.DashboardLayout);
        if (req.EmailDigestFrequency is not null) prefs.EmailDigestFrequency = req.EmailDigestFrequency;
        if (req.KeyboardShortcutsEnabled.HasValue) prefs.KeyboardShortcutsEnabled = req.KeyboardShortcutsEnabled.Value;
        if (req.DateFormat is not null) prefs.DateFormat = req.DateFormat;
        if (req.TimeFormat is not null) prefs.TimeFormat = req.TimeFormat;
    }

    private static UserPreferencesResponse MapToResponse(UserPreferences p) => new()
    {
        Theme = p.Theme,
        Language = p.Language,
        TimezoneOverride = p.TimezoneOverride,
        DefaultBoardView = p.DefaultBoardView,
        DefaultBoardFilters = p.DefaultBoardFilters is not null
            ? System.Text.Json.JsonSerializer.Deserialize<object>(p.DefaultBoardFilters) : null,
        DashboardLayout = p.DashboardLayout is not null
            ? System.Text.Json.JsonSerializer.Deserialize<object>(p.DashboardLayout) : null,
        EmailDigestFrequency = p.EmailDigestFrequency,
        KeyboardShortcutsEnabled = p.KeyboardShortcutsEnabled,
        DateFormat = p.DateFormat,
        TimeFormat = p.TimeFormat
    };
}
