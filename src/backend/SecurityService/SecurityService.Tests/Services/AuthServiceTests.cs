using Moq;
using Microsoft.Extensions.Logging;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Helpers;
using SecurityService.Domain.Interfaces.Services.AnomalyDetection;
using SecurityService.Domain.Interfaces.Services.Auth;
using SecurityService.Domain.Interfaces.Services.Jwt;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Domain.Interfaces.Services.Password;
using SecurityService.Domain.Interfaces.Services.RateLimiter;
using SecurityService.Domain.Interfaces.Services.Session;
using SecurityService.Application.Contracts;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Services.Auth;
using SecurityService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace SecurityService.Tests.Services;

/// <summary>
/// Unit tests for AuthService login flow.
/// Validates: REQ-001, REQ-010, REQ-014
/// </summary>
public class AuthServiceTests
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
    private readonly AppSettings _appSettings;
    private readonly AuthService _authService;

    private const string TestEmail = "user@test.com";
    private const string TestPassword = "CorrectP@ss1";
    private readonly string _passwordHash;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _deptId = Guid.NewGuid();

    public AuthServiceTests()
    {
        _jwtServiceMock = new Mock<IJwtService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _rateLimiterMock = new Mock<IRateLimiterService>();
        _anomalyMock = new Mock<IAnomalyDetectionService>();
        _passwordMock = new Mock<IPasswordService>();
        _outboxMock = new Mock<IOutboxService>();
        _profileClientMock = new Mock<IProfileServiceClient>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

        _appSettings = new AppSettings
        {
            AccountLockoutMaxAttempts = 10,
            AccountLockoutWindowHours = 24,
            AccountLockoutDurationMinutes = 60,
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };

        _passwordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword);

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

    private void SetupNotLocked()
    {
        _dbMock.Setup(d => d.KeyExistsAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("nexus:lockout:locked:")),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
    }

    private void SetupActiveUser()
    {
        _profileClientMock.Setup(p => p.GetTeamMemberByEmailAsync(TestEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileUserResponse
            {
                TeamMemberId = _userId,
                Email = TestEmail,
                PasswordHash = _passwordHash,
                FlgStatus = EntityStatuses.Active,
                OrganizationId = _orgId,
                PrimaryDepartmentId = _deptId,
                RoleName = "Member",
                DepartmentRole = "Contributor",
                IsFirstTimeUser = false
            });
    }

    private void SetupTokenGeneration()
    {
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("test-refresh-token");
        _jwtServiceMock.Setup(j => j.GetJti(It.IsAny<string>())).Returns("test-jti");
        _jwtServiceMock.Setup(j => j.GetTokenExpiry(It.IsAny<string>())).Returns(DateTime.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task SuccessfulLogin_ReturnsTokensAndCreatesSession()
    {
        SetupNotLocked();
        SetupActiveUser();
        SetupTokenGeneration();

        _anomalyMock.Setup(a => a.CheckLoginAnomalyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await _authService.LoginAsync(TestEmail, TestPassword, "127.0.0.1", "device-1");

        Assert.NotNull(result);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("test-refresh-token", result.RefreshToken);
        Assert.False(result.IsFirstTimeUser);

        // Verify session was created
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            _userId, "device-1", "127.0.0.1", "test-jti",
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidCredentials_ThrowsAndIncrementsLockout()
    {
        SetupNotLocked();
        SetupActiveUser();

        _dbMock.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _dbMock.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _authService.LoginAsync(TestEmail, "WrongPassword!", "127.0.0.1", "device-1"));

        // Verify lockout counter was incremented
        _dbMock.Verify(d => d.StringIncrementAsync(
            It.Is<RedisKey>(k => k.ToString() == $"nexus:lockout:{TestEmail}"),
            It.IsAny<long>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task LockedAccount_ThrowsAccountLockedException_WithoutCredentialCheck()
    {
        _dbMock.Setup(d => d.KeyExistsAsync(
            It.Is<RedisKey>(k => k.ToString() == $"nexus:lockout:locked:{TestEmail}"),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<AccountLockedException>(
            () => _authService.LoginAsync(TestEmail, TestPassword, "127.0.0.1", "device-1"));

        // Verify no credential check was performed
        _profileClientMock.Verify(p => p.GetTeamMemberByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InactiveAccount_ThrowsAccountInactiveException()
    {
        SetupNotLocked();

        _profileClientMock.Setup(p => p.GetTeamMemberByEmailAsync(TestEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileUserResponse
            {
                TeamMemberId = _userId,
                Email = TestEmail,
                PasswordHash = _passwordHash,
                FlgStatus = EntityStatuses.Suspended,
                OrganizationId = _orgId,
                PrimaryDepartmentId = _deptId,
                RoleName = "Member"
            });

        await Assert.ThrowsAsync<AccountInactiveException>(
            () => _authService.LoginAsync(TestEmail, TestPassword, "127.0.0.1", "device-1"));
    }
}
