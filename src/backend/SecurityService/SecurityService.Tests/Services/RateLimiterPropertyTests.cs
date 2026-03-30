using FsCheck;
using FsCheck.Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SecurityService.Infrastructure.Services.RateLimiter;
using StackExchange.Redis;

namespace SecurityService.Tests.Services;

/// <summary>
/// Property-based tests for sliding window rate limiter.
/// Property: For N requests within a window of max M, the first M are allowed and request M+1 is denied with RetryAfterSeconds > 0.
/// Validates: REQ-012.1, REQ-012.4
/// </summary>
public class RateLimiterPropertyTests
{
    /// <summary>
    /// Property: Given a max of M requests, the first M calls are allowed (IsAllowed=true, RetryAfterSeconds=0)
    /// and call M+1 is denied (IsAllowed=false, RetryAfterSeconds > 0).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool FirstM_Allowed_Then_Denied(ushort seed)
    {
        var rng = new Random(seed);
        var maxRequests = rng.Next(1, 20); // M in [1, 19]
        var windowSeconds = rng.Next(10, 300); // window in [10, 299] seconds
        var window = TimeSpan.FromSeconds(windowSeconds);

        var callCount = 0;
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>(MockBehavior.Loose);
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        // Simulate the Lua script: track call count, allow first M, deny M+1
        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= maxRequests)
                {
                    // Allowed: {1, 0}
                    return Task.FromResult<RedisResult>(
                        RedisResult.Create(new[] {
                            RedisResult.Create((RedisValue)1),
                            RedisResult.Create((RedisValue)0)
                        }));
                }
                else
                {
                    // Denied: {0, retryAfterMs} — simulate some positive retry-after
                    var retryAfterMs = rng.Next(1000, windowSeconds * 1000);
                    return Task.FromResult<RedisResult>(
                        RedisResult.Create(new[] {
                            RedisResult.Create((RedisValue)0),
                            RedisResult.Create((RedisValue)retryAfterMs)
                        }));
                }
            });

        var logger = new Mock<ILogger<RateLimiterService>>();
        var service = new RateLimiterService(redisMock.Object, logger.Object);

        var identity = $"ip-{seed}";
        var endpoint = "/api/v1/auth/login";

        // Send M+1 requests
        for (int i = 1; i <= maxRequests; i++)
        {
            var (isAllowed, retryAfter) = service.CheckRateLimitAsync(identity, endpoint, maxRequests, window).GetAwaiter().GetResult();
            if (!isAllowed || retryAfter != 0)
                return false; // First M should all be allowed with 0 retry
        }

        // Request M+1 must be denied
        var (denied, retryAfterSeconds) = service.CheckRateLimitAsync(identity, endpoint, maxRequests, window).GetAwaiter().GetResult();
        return !denied && retryAfterSeconds > 0;
    }

    /// <summary>
    /// Property: When the Lua script returns allowed ({1, 0}), IsAllowed is true and RetryAfterSeconds is 0.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AllowedResponse_MapsCorrectly(ushort seed)
    {
        var rng = new Random(seed);
        var maxRequests = rng.Next(1, 50);
        var windowSeconds = rng.Next(10, 600);

        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>(MockBehavior.Loose);
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new[] {
                RedisResult.Create((RedisValue)1),
                RedisResult.Create((RedisValue)0)
            }));

        var logger = new Mock<ILogger<RateLimiterService>>();
        var service = new RateLimiterService(redisMock.Object, logger.Object);

        var (isAllowed, retryAfter) = service.CheckRateLimitAsync(
            $"ip-{seed}", "/api/v1/auth/login", maxRequests, TimeSpan.FromSeconds(windowSeconds))
            .GetAwaiter().GetResult();

        return isAllowed && retryAfter == 0;
    }

    /// <summary>
    /// Property: When the Lua script returns denied ({0, retryMs}), IsAllowed is false and RetryAfterSeconds > 0.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DeniedResponse_HasPositiveRetryAfter(ushort seed)
    {
        var rng = new Random(seed);
        var maxRequests = rng.Next(1, 50);
        var windowSeconds = rng.Next(10, 600);
        var retryAfterMs = rng.Next(1000, windowSeconds * 1000 + 1);

        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>(MockBehavior.Loose);
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new[] {
                RedisResult.Create((RedisValue)0),
                RedisResult.Create((RedisValue)retryAfterMs)
            }));

        var logger = new Mock<ILogger<RateLimiterService>>();
        var service = new RateLimiterService(redisMock.Object, logger.Object);

        var (isAllowed, retryAfter) = service.CheckRateLimitAsync(
            $"ip-{seed}", "/api/v1/auth/login", maxRequests, TimeSpan.FromSeconds(windowSeconds))
            .GetAwaiter().GetResult();

        return !isAllowed && retryAfter > 0;
    }

    /// <summary>
    /// Property: When the Lua script returns a result with fewer than 2 elements, the service gracefully allows the request (fail-open).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ShortLuaResult_FailsOpen(ushort seed)
    {
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>(MockBehavior.Loose);
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        // Return an array with only 1 element — triggers the Length < 2 guard
        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(new[] {
                RedisResult.Create((RedisValue)1)
            }));

        var logger = new Mock<ILogger<RateLimiterService>>();
        var service = new RateLimiterService(redisMock.Object, logger.Object);

        var (isAllowed, retryAfter) = service.CheckRateLimitAsync(
            $"ip-{seed}", "/api/v1/auth/login", 5, TimeSpan.FromMinutes(15))
            .GetAwaiter().GetResult();

        return isAllowed && retryAfter == 0;
    }
}
