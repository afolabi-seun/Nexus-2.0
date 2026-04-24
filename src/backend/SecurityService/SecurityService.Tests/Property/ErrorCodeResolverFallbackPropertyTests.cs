using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Infrastructure.Services.ErrorCodeResolver;
using SecurityService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for static fallback resolution.
/// Feature: architecture-hardening, Property 5: Static fallback resolution
/// **Validates: Requirements 4.5**
/// </summary>
public class ErrorCodeResolverFallbackPropertyTests
{
    /// <summary>
    /// Known error codes that exercise specific branches of the MapErrorToResponseCode switch expression.
    /// </summary>
    private static readonly string[] KnownErrorCodes =
    [
        "INVALID_CREDENTIALS",
        "ACCOUNT_LOCKED",
        "ACCOUNT_INACTIVE",
        "INSUFFICIENT_PERMISSIONS",
        "DEPARTMENT_ACCESS_DENIED",
        "ORGANIZATION_MISMATCH",
        "ORGADMIN_REQUIRED",
        "DEPTLEAD_REQUIRED",
        "PLATFORM_ADMIN_REQUIRED",
        "OTP_EXPIRED",
        "OTP_INVALID",
        "PASSWORD_TOO_SHORT",
        "PASSWORD_MISMATCH",
        "DUPLICATE_EMAIL",
        "NAME_CONFLICT",
        "USER_NOT_FOUND",
        "RESOURCE_NOT_FOUND",
        "RATE_LIMIT_EXCEEDED",
        "INVALID_TOKEN",
        "INVALID_FORMAT",
        "VALIDATION_ERROR",
        "INTERNAL_ERROR",
        "SOME_RANDOM_CODE"
    ];

    private static string GenerateErrorCode(Random rng)
    {
        // 70% known codes, 30% random codes
        if (rng.Next(10) < 7)
            return KnownErrorCodes[rng.Next(KnownErrorCodes.Length)];

        // Generate random unknown codes
        var prefixes = new[] { "CUSTOM_", "ERR_", "SYS_", "APP_", "" };
        var suffixes = new[] { "FAILURE", "TIMEOUT", "UNKNOWN", rng.Next(10000).ToString() };
        return prefixes[rng.Next(prefixes.Length)] + suffixes[rng.Next(suffixes.Length)];
    }

    /// <summary>
    /// When all tiers fail (Redis returns null, HTTP throws), ResolveAsync returns
    /// (MapErrorToResponseCode(errorCode), errorCode) — the static fallback.
    /// Feature: architecture-hardening, Property 5: Static fallback resolution
    /// **Validates: Requirements 4.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AllTiersFail_ResolveAsync_ReturnsStaticFallback(ushort seed)
    {
        var rng = new Random(seed);
        var errorCode = GenerateErrorCode(rng);

        // Mock Redis — always returns null (miss)
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        // Mock HTTP — always throws (UtilityService unavailable)
        var mockUtility = new Mock<IUtilityServiceClient>();
        mockUtility.Setup(u => u.GetErrorCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("UtilityService unavailable"));

        var logger = new Mock<ILogger<ErrorCodeResolverService>>();
        var service = new ErrorCodeResolverService(mockUtility.Object, mockRedis.Object, logger.Object);

        var result = service.ResolveAsync(errorCode).GetAwaiter().GetResult();

        var expectedResponseCode = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);

        return result.ResponseCode == expectedResponseCode
            && result.ResponseDescription == errorCode;
    }

    /// <summary>
    /// When Redis throws an exception (not just a miss) and HTTP also throws,
    /// ResolveAsync still falls back to the static mapping.
    /// Feature: architecture-hardening, Property 5: Static fallback resolution
    /// **Validates: Requirements 4.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool RedisThrows_HttpThrows_ResolveAsync_ReturnsStaticFallback(ushort seed)
    {
        var rng = new Random(seed);
        var errorCode = GenerateErrorCode(rng);

        // Mock Redis — throws exception (simulating Redis down)
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        // Mock HTTP — throws
        var mockUtility = new Mock<IUtilityServiceClient>();
        mockUtility.Setup(u => u.GetErrorCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unreachable"));

        var logger = new Mock<ILogger<ErrorCodeResolverService>>();
        var service = new ErrorCodeResolverService(mockUtility.Object, mockRedis.Object, logger.Object);

        var result = service.ResolveAsync(errorCode).GetAwaiter().GetResult();

        var expectedResponseCode = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);

        return result.ResponseCode == expectedResponseCode
            && result.ResponseDescription == errorCode;
    }
}
