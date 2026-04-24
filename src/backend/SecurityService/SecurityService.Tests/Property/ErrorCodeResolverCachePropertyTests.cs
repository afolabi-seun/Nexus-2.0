using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Application.Contracts;
using SecurityService.Infrastructure.Services.ErrorCodeResolver;
using SecurityService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for tiered cache resolution with promotion.
/// Feature: architecture-hardening, Property 4: Tiered cache resolution with promotion
/// **Validates: Requirements 4.2, 4.3, 4.4**
/// </summary>
public class ErrorCodeResolverCachePropertyTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Scenario 1: When Redis has the value (Tier 2 hit), ResolveAsync returns it
    /// and promotes to in-memory (Tier 1). A second call for the same code should
    /// NOT hit Redis again — it should be served from in-memory.
    /// **Validates: Requirements 4.2, 4.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RedisHit_PromotesToInMemory_SecondCallSkipsRedis(ushort seed)
    {
        var rng = new Random(seed);
        var errorCode = $"ERR_{rng.Next(10000)}";
        var responseCode = rng.Next(1, 100).ToString("D2");
        var responseDescription = $"desc-{rng.Next(10000)}";

        var cachedResponse = new ErrorCodeResponse
        {
            ResponseCode = responseCode,
            ResponseDescription = responseDescription
        };
        var json = JsonSerializer.Serialize(cachedResponse, JsonOptions);

        // Mock Redis to return the value
        var mockDb = new Mock<IDatabase>();
        var redisGetCallCount = 0;
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags flags) =>
            {
                Interlocked.Increment(ref redisGetCallCount);
                return (RedisValue)json;
            });

        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        // Mock HTTP client — should NOT be called since Redis hits
        var mockUtility = new Mock<IUtilityServiceClient>();
        mockUtility.Setup(u => u.GetErrorCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Should not be called"));

        var logger = new Mock<ILogger<ErrorCodeResolverService>>();
        var service = new ErrorCodeResolverService(mockUtility.Object, mockRedis.Object, logger.Object);

        // First call — should hit Redis and promote to in-memory
        var result1 = service.ResolveAsync(errorCode).GetAwaiter().GetResult();
        if (result1.ResponseCode != responseCode) return false;
        if (result1.ResponseDescription != responseDescription) return false;
        if (redisGetCallCount != 1) return false;

        // Second call — should come from in-memory, NOT Redis
        var result2 = service.ResolveAsync(errorCode).GetAwaiter().GetResult();
        if (result2.ResponseCode != responseCode) return false;
        if (result2.ResponseDescription != responseDescription) return false;
        if (redisGetCallCount != 1) return false; // Still 1 — no second Redis call

        return true;
    }

    /// <summary>
    /// Scenario 2: When Redis misses and HTTP returns a value (Tier 3 hit),
    /// ResolveAsync promotes to both in-memory and Redis. Verify Redis StringSetAsync
    /// was called, and a second call does not hit Redis or HTTP.
    /// **Validates: Requirements 4.3, 4.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool HttpHit_PromotesToInMemoryAndRedis(ushort seed)
    {
        var rng = new Random(seed);
        var errorCode = $"ERR_{rng.Next(10000)}";
        var responseCode = rng.Next(1, 100).ToString("D2");
        var responseDescription = $"desc-{rng.Next(10000)}";

        // Mock Redis — miss on get, accept any set call
        var mockDb = new Mock<IDatabase>(MockBehavior.Loose);
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        // Mock HTTP — returns value, track call count
        var httpCallCount = 0;
        var mockUtility = new Mock<IUtilityServiceClient>();
        mockUtility.Setup(u => u.GetErrorCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken ct) =>
            {
                Interlocked.Increment(ref httpCallCount);
                return new ErrorCodeResponse
                {
                    ResponseCode = responseCode,
                    ResponseDescription = responseDescription
                };
            });

        var logger = new Mock<ILogger<ErrorCodeResolverService>>();
        var service = new ErrorCodeResolverService(mockUtility.Object, mockRedis.Object, logger.Object);

        // First call — Redis miss, HTTP hit → should promote to in-memory and Redis
        var result1 = service.ResolveAsync(errorCode).GetAwaiter().GetResult();
        if (result1.ResponseCode != responseCode) return false;
        if (result1.ResponseDescription != responseDescription) return false;
        if (httpCallCount != 1) return false;

        // Verify Redis promotion: at least one StringSetAsync call was made (any overload)
        var invocations = mockDb.Invocations
            .Where(i => i.Method.Name == "StringSetAsync")
            .ToList();
        if (invocations.Count == 0) return false;

        // Second call — should come from in-memory, no more HTTP calls
        var result2 = service.ResolveAsync(errorCode).GetAwaiter().GetResult();
        if (result2.ResponseCode != responseCode) return false;
        if (result2.ResponseDescription != responseDescription) return false;
        if (httpCallCount != 1) return false; // Still 1 — served from in-memory

        return true;
    }

    /// <summary>
    /// Scenario 3: When all tiers miss (Redis miss, HTTP throws), ResolveAsync
    /// falls back to the static MapErrorToResponseCode and returns the error code
    /// as the description.
    /// **Validates: Requirements 4.2, 4.3, 4.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AllTiersMiss_FallsBackToStaticMapping(ushort seed)
    {
        var rng = new Random(seed);
        var errorCode = $"ERR_{rng.Next(10000)}";

        // Mock Redis — miss
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        // Mock HTTP — throws (simulating UtilityService down)
        var mockUtility = new Mock<IUtilityServiceClient>();
        mockUtility.Setup(u => u.GetErrorCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("UtilityService unavailable"));

        var logger = new Mock<ILogger<ErrorCodeResolverService>>();
        var service = new ErrorCodeResolverService(mockUtility.Object, mockRedis.Object, logger.Object);

        var result = service.ResolveAsync(errorCode).GetAwaiter().GetResult();

        // Should match static fallback
        var expectedResponseCode = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);
        if (result.ResponseCode != expectedResponseCode) return false;
        if (result.ResponseDescription != errorCode) return false;

        return true;
    }
}
