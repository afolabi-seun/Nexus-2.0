using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using ProfileService.Application.DTOs.Preferences;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;
using ProfileService.Infrastructure.Services.Preferences;
using StackExchange.Redis;

namespace ProfileService.Tests.Services;

public class PreferenceResolverTests
{
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IDepartmentRepository> _deptRepo = new();
    private readonly Mock<IUserPreferencesRepository> _userPrefsRepo = new();
    private readonly PreferenceResolver _resolver;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PreferenceResolverTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);

        // Default: all cache misses
        _redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _resolver = new PreferenceResolver(
            _redis.Object,
            _orgRepo.Object,
            _deptRepo.Object,
            _userPrefsRepo.Object,
            Mock.Of<ILogger<PreferenceResolver>>());
    }

    [Fact]
    public async Task ResolveAsync_UserPrefOverridesOrgDefault()
    {
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        // Org has DefaultBoardView = "Sprint"
        var org = new Organization
        {
            OrganizationId = orgId,
            TimeZone = "US/Eastern",
            SettingsJson = JsonSerializer.Serialize(new OrganizationSettings
            {
                DefaultBoardView = "Sprint"
            }, JsonOptions)
        };
        _orgRepo.Setup(r => r.GetByIdAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);

        // Dept has no special prefs
        _deptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { DepartmentId = deptId });

        // User overrides with "Backlog"
        _userPrefsRepo.Setup(r => r.GetByMemberIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPreferences
            {
                TeamMemberId = userId,
                DefaultBoardView = "Backlog",
                Theme = "Dark"
            });

        var result = (ResolvedPreferencesResponse)await _resolver.ResolveAsync(userId, deptId, orgId);

        Assert.Equal("Backlog", result.DefaultBoardView); // user overrides org
        Assert.Equal("Dark", result.Theme);               // user pref used
    }

    [Fact]
    public async Task ResolveAsync_OrgDefaultUsedWhenUserPrefIsNull()
    {
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var org = new Organization
        {
            OrganizationId = orgId,
            TimeZone = "US/Pacific",
            SettingsJson = JsonSerializer.Serialize(new OrganizationSettings
            {
                DefaultBoardView = "Sprint",
                DigestFrequency = "Daily"
            }, JsonOptions)
        };
        _orgRepo.Setup(r => r.GetByIdAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(org);
        _deptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { DepartmentId = deptId });

        // User has no preferences
        _userPrefsRepo.Setup(r => r.GetByMemberIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);

        var result = (ResolvedPreferencesResponse)await _resolver.ResolveAsync(userId, deptId, orgId);

        Assert.Equal("Sprint", result.DefaultBoardView); // org default
        Assert.Equal("Daily", result.DigestFrequency);   // org default
        Assert.Equal("US/Pacific", result.Timezone);      // org timezone
    }

    [Fact]
    public async Task ResolveAsync_SystemDefaultsUsedWhenAllLevelsNull()
    {
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        // No org found
        _orgRepo.Setup(r => r.GetByIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);
        _deptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);
        _userPrefsRepo.Setup(r => r.GetByMemberIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);

        var result = (ResolvedPreferencesResponse)await _resolver.ResolveAsync(userId, deptId, orgId);

        Assert.Equal(SystemDefaults.Theme, result.Theme);
        Assert.Equal(SystemDefaults.DefaultBoardView, result.DefaultBoardView);
        Assert.Equal(SystemDefaults.DateFormat, result.DateFormat);
        Assert.Equal(SystemDefaults.TimeFormat, result.TimeFormat);
        Assert.Equal(SystemDefaults.Timezone, result.Timezone);
        Assert.Equal(SystemDefaults.DigestFrequency, result.DigestFrequency);
        Assert.Equal(SystemDefaults.StoryPointScale, result.StoryPointScale);
    }
}
