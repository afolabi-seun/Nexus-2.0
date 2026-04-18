using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Domain.Exceptions;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Services.Otp;
using StackExchange.Redis;

namespace SecurityService.Tests.Services;

/// <summary>
/// Unit tests for OtpService with mocked Redis.
/// Validates: REQ-009.1, REQ-009.2
/// </summary>
public class OtpServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly AppSettings _appSettings;
    private readonly OtpService _service;

    public OtpServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

        _appSettings = new AppSettings
        {
            OtpExpiryMinutes = 5,
            OtpMaxAttempts = 3
        };

        var logger = new Mock<ILogger<OtpService>>();
        _service = new OtpService(_redisMock.Object, _appSettings, logger.Object);
    }

    [Fact]
    public async Task GenerateOtpAsync_Returns6DigitString()
    {
        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var code = await _service.GenerateOtpAsync("user@test.com");

        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(code.All(char.IsDigit), $"OTP code '{code}' contains non-digit characters");
    }

    [Fact]
    public async Task VerifyOtpAsync_WithCorrectCode_Succeeds()
    {
        var identity = "user@test.com";
        var code = "123456";
        var otpData = JsonSerializer.Serialize(new { code, attempts = 0 },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == $"nexus:otp:{identity}"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(otpData));
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await _service.VerifyOtpAsync(identity, code);

        Assert.True(result);
        _dbMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString() == $"nexus:otp:{identity}"), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithWrongCode_ThrowsOtpVerificationFailedException()
    {
        var identity = "user@test.com";
        var otpData = JsonSerializer.Serialize(new { code = "123456", attempts = 0 },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == $"nexus:otp:{identity}"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(otpData));
        _dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMinutes(4));
        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<OtpVerificationFailedException>(
            () => _service.VerifyOtpAsync(identity, "999999"));
    }

    [Fact]
    public async Task VerifyOtpAsync_AfterMaxAttempts_ThrowsOtpMaxAttemptsException()
    {
        var identity = "user@test.com";
        // attempts = 2 means next wrong attempt (3rd) hits max (OtpMaxAttempts = 3)
        var otpData = JsonSerializer.Serialize(new { code = "123456", attempts = 2 },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == $"nexus:otp:{identity}"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(otpData));
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<OtpMaxAttemptsException>(
            () => _service.VerifyOtpAsync(identity, "999999"));
    }

    [Fact]
    public async Task VerifyOtpAsync_WithExpiredOtp_ThrowsOtpExpiredException()
    {
        var identity = "user@test.com";

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString() == $"nexus:otp:{identity}"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        await Assert.ThrowsAsync<OtpExpiredException>(
            () => _service.VerifyOtpAsync(identity, "123456"));
    }
}
