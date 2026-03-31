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
/// Unit tests for account lockout behavior.
/// Validates: REQ-010.1, REQ-010.2, REQ-010.3
/// </summary>
public class AccountLockoutTests
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
    private const string TestPassword = "WrongPassword1!";

    public AccountLockoutTests()
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

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

        _appSettings = new AppSettings
        {
            AccountLockoutMaxAttempts = 3,
            AccountLockoutWindowHours = 24,
            AccountLockoutDurationMinutes = 60,
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
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
    public async Task AfterNFailedAttempts_LockoutFlagIsSet()
    {
        var lockedKey = $"lockout:locked:{TestEmail}";
        var lockoutKey = $"lockout:{TestEmail}";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!");

        // Not locked initially
        _dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == lockedKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        _profileClientMock.Setup(p => p.GetTeamMemberByEmailAsync(TestEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileUserResponse
            {
                TeamMemberId = Guid.NewGuid(),
                Email = TestEmail,
                PasswordHash = passwordHash,
                FlgStatus = EntityStatuses.Active,
                OrganizationId = Guid.NewGuid(),
                PrimaryDepartmentId = Guid.NewGuid(),
                RoleName = "Member"
            });

        // Simulate the Nth failed attempt reaching max
        _dbMock.Setup(d => d.StringIncrementAsync(It.Is<RedisKey>(k => k.ToString() == lockoutKey), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(_appSettings.AccountLockoutMaxAttempts);

        _dbMock.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _dbMock.Setup(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString() == lockedKey),
            It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _dbMock.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString() == lockoutKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _authService.LoginAsync(TestEmail, TestPassword, "127.0.0.1", "device-1"));

        // Verify lockout flag was set
        var lockSetInvocations = _dbMock.Invocations
            .Where(i => i.Method.Name == "StringSetAsync"
                && i.Arguments[0].ToString() == lockedKey)
            .ToList();
        Assert.Single(lockSetInvocations);
    }

    [Fact]
    public async Task WhileLocked_Login_ThrowsAccountLockedException()
    {
        var lockedKey = $"lockout:locked:{TestEmail}";

        // Account is locked
        _dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == lockedKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<AccountLockedException>(
            () => _authService.LoginAsync(TestEmail, "AnyPassword1!", "127.0.0.1", "device-1"));

        // Verify no credential check was performed (ProfileService not called)
        _profileClientMock.Verify(p => p.GetTeamMemberByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AfterLockoutExpiry_LoginProceeds()
    {
        var lockedKey = $"lockout:locked:{TestEmail}";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!");
        var userId = Guid.NewGuid();

        // Lockout expired (key doesn't exist)
        _dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == lockedKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        _profileClientMock.Setup(p => p.GetTeamMemberByEmailAsync(TestEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileUserResponse
            {
                TeamMemberId = userId,
                Email = TestEmail,
                PasswordHash = passwordHash,
                FlgStatus = EntityStatuses.Active,
                OrganizationId = Guid.NewGuid(),
                PrimaryDepartmentId = Guid.NewGuid(),
                RoleName = "Member"
            });

        _anomalyMock.Setup(a => a.CheckLoginAnomalyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _jwtServiceMock.Setup(j => j.GenerateAccessToken(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwtServiceMock.Setup(j => j.GetJti(It.IsAny<string>())).Returns("jti-123");
        _jwtServiceMock.Setup(j => j.GetTokenExpiry(It.IsAny<string>())).Returns(DateTime.UtcNow.AddMinutes(15));

        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await _authService.LoginAsync(TestEmail, "CorrectPassword1!", "127.0.0.1", "device-1");

        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
    }
}
