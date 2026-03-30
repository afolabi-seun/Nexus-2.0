using Moq;
using Microsoft.Extensions.Logging;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services;
using SecurityService.Application.Contracts;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Services.Auth;
using SecurityService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace SecurityService.Tests.Services;

/// <summary>
/// Unit tests for refresh token rotation.
/// Validates: REQ-003.1, REQ-003.2
/// </summary>
public class RefreshTokenRotationTests
{
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IRateLimiterService> _rateLimiterMock;
    private readonly Mock<IAnomalyDetectionService> _anomalyMock;
    private readonly Mock<IPasswordService> _passwordMock;
    private readonly Mock<IOutboxService> _outboxMock;
    private readonly Mock<IProfileServiceClient> _profileClientMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<IServer> _serverMock;
    private readonly AppSettings _appSettings;
    private readonly AuthService _authService;

    public RefreshTokenRotationTests()
    {
        _jwtServiceMock = new Mock<IJwtService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _rateLimiterMock = new Mock<IRateLimiterService>();
        _anomalyMock = new Mock<IAnomalyDetectionService>();
        _passwordMock = new Mock<IPasswordService>();
        _outboxMock = new Mock<IOutboxService>();
        _profileClientMock = new Mock<IProfileServiceClient>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
        _serverMock = new Mock<IServer>();

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _redisMock.Setup(r => r.GetServers()).Returns(new[] { _serverMock.Object });

        _appSettings = new AppSettings
        {
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
            AccountLockoutMaxAttempts = 10,
            AccountLockoutWindowHours = 24,
            AccountLockoutDurationMinutes = 60
        };

        _authService = new AuthService(
            _jwtServiceMock.Object,
            _sessionServiceMock.Object,
            _rateLimiterMock.Object,
            _anomalyMock.Object,
            _passwordMock.Object,
            _outboxMock.Object,
            _profileClientMock.Object,
            _redisMock.Object,
            _appSettings,
            new Mock<ILogger<AuthService>>().Object);
    }

    [Fact]
    public async Task RefreshToken_Success_DeletesOldTokenAndStoresNew()
    {
        var userId = Guid.NewGuid();
        var deviceId = "device-1";
        var oldRefreshToken = "old-refresh-token";
        var oldRefreshHash = BCrypt.Net.BCrypt.HashPassword(oldRefreshToken);
        var refreshKey = $"refresh:{userId}:{deviceId}";

        // Setup server to return the matching key
        _serverMock.Setup(s => s.KeysAsync(
            It.IsAny<int>(), It.Is<RedisValue>(v => v.ToString().Contains(deviceId)),
            It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(ToAsyncEnumerable(new RedisKey[] { refreshKey }));

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == refreshKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(oldRefreshHash));

        _dbMock.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString() == refreshKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Setup user cache scan to return email
        var userCacheKey = $"user_cache:{userId}";
        _serverMock.Setup(s => s.KeysAsync(
            It.IsAny<int>(), It.Is<RedisValue>(v => v.ToString() == "user_cache:*"),
            It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(ToAsyncEnumerable(new RedisKey[] { userCacheKey }));

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == userCacheKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue($"{{\"teamMemberId\":\"{userId}\",\"email\":\"user@test.com\"}}"));

        var profileUser = new ProfileUserResponse
        {
            TeamMemberId = userId,
            Email = "user@test.com",
            OrganizationId = Guid.NewGuid(),
            PrimaryDepartmentId = Guid.NewGuid(),
            RoleName = "Member",
            DepartmentRole = "Contributor",
            FlgStatus = "A"
        };
        _profileClientMock.Setup(p => p.GetTeamMemberByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileUser);

        _jwtServiceMock.Setup(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("new-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _jwtServiceMock.Setup(j => j.GetJti(It.IsAny<string>())).Returns("new-jti");
        _jwtServiceMock.Setup(j => j.GetTokenExpiry(It.IsAny<string>())).Returns(DateTime.UtcNow.AddMinutes(15));

        _dbMock.Setup(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("refresh:")),
            It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await _authService.RefreshTokenAsync(oldRefreshToken, deviceId);

        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);

        // Verify old token was deleted
        _dbMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString() == refreshKey), It.IsAny<CommandFlags>()), Times.Once);

        // Verify new token was stored (check invocations since StringSetAsync has multiple overloads)
        var setInvocations = _dbMock.Invocations
            .Where(i => i.Method.Name == "StringSetAsync"
                && i.Arguments[0].ToString() == refreshKey)
            .ToList();
        Assert.Single(setInvocations);
    }

    [Fact]
    public async Task RefreshToken_AlreadyRotated_ThrowsRefreshTokenReuseException()
    {
        var deviceId = "device-1";
        var oldRefreshToken = "already-used-token";
        var userId = Guid.NewGuid();
        var refreshKey = $"refresh:{userId}:{deviceId}";

        // First scan: no matching hash (token was already rotated)
        _serverMock.Setup(s => s.KeysAsync(
            It.IsAny<int>(), It.Is<RedisValue>(v => v.ToString().Contains(deviceId)),
            It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(ToAsyncEnumerable(new RedisKey[] { refreshKey }));

        // The stored hash doesn't match the old token (it was rotated)
        var differentHash = BCrypt.Net.BCrypt.HashPassword("different-token");
        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == refreshKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(differentHash));

        await Assert.ThrowsAsync<RefreshTokenReuseException>(
            () => _authService.RefreshTokenAsync(oldRefreshToken, deviceId));

        // Verify all sessions were revoked
        _sessionServiceMock.Verify(s => s.RevokeAllSessionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<RedisKey> ToAsyncEnumerable(RedisKey[] keys)
    {
        foreach (var key in keys)
        {
            yield return key;
        }
        await Task.CompletedTask;
    }
}
