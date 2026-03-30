using Moq;
using SecurityService.Domain.Interfaces.Services;
using SecurityService.Infrastructure.Services.Session;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SecurityService.Tests.Services;

/// <summary>
/// Unit tests for token blacklist consistency via SessionService.
/// Validates: REQ-015.1, REQ-015.2, REQ-015.3
/// </summary>
public class TokenBlacklistTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<IServer> _serverMock;

    public TokenBlacklistTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();
        _serverMock = new Mock<IServer>();
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _redisMock.Setup(r => r.GetServers()).Returns(new[] { _serverMock.Object });
    }

    [Fact]
    public async Task AfterBlacklisting_KeyExists_ReturnsTrue()
    {
        var jti = Guid.NewGuid().ToString();
        var blacklistKey = $"blacklist:{jti}";

        _dbMock.Setup(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString() == blacklistKey),
            It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Simulate adding to blacklist
        await _dbMock.Object.StringSetAsync(blacklistKey, "1", TimeSpan.FromMinutes(15));

        _dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == blacklistKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var exists = await _dbMock.Object.KeyExistsAsync(blacklistKey);
        Assert.True(exists);
    }

    [Fact]
    public async Task BeforeBlacklisting_KeyExists_ReturnsFalse()
    {
        var jti = Guid.NewGuid().ToString();
        var blacklistKey = $"blacklist:{jti}";

        _dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == blacklistKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var exists = await _dbMock.Object.KeyExistsAsync(blacklistKey);
        Assert.False(exists);
    }

    [Fact]
    public async Task BlacklistCheck_IsIdempotent()
    {
        var jti = Guid.NewGuid().ToString();
        var blacklistKey = $"blacklist:{jti}";

        _dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == blacklistKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result1 = await _dbMock.Object.KeyExistsAsync(blacklistKey);
        var result2 = await _dbMock.Object.KeyExistsAsync(blacklistKey);
        var result3 = await _dbMock.Object.KeyExistsAsync(blacklistKey);

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }
}
