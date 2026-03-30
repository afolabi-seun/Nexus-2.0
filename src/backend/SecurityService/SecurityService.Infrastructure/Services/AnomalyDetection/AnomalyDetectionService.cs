using Microsoft.Extensions.Logging;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.AnomalyDetection;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnomalyDetectionService> _logger;

    private static readonly TimeSpan TrustedIpTtl = TimeSpan.FromDays(90);

    public AnomalyDetectionService(IConnectionMultiplexer redis, ILogger<AnomalyDetectionService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> CheckLoginAnomalyAsync(Guid userId, string ipAddress, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"trusted_ips:{userId}";

        var isTrusted = await db.SetContainsAsync(key, ipAddress);

        if (isTrusted)
        {
            return false; // No anomaly
        }

        // Check if the set has any entries — if it does, this IP is suspicious
        var setLength = await db.SetLengthAsync(key);
        if (setLength > 0)
        {
            _logger.LogWarning(
                "Suspicious login detected for user {UserId} from untrusted IP {IpAddress}",
                userId, ipAddress);
            throw new SuspiciousLoginException();
        }

        // No trusted IPs yet (first login) — not suspicious
        return false;
    }

    public async Task AddTrustedIpAsync(Guid userId, string ipAddress, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"trusted_ips:{userId}";

        await db.SetAddAsync(key, ipAddress);
        await db.KeyExpireAsync(key, TrustedIpTtl);

        _logger.LogInformation("Added trusted IP {IpAddress} for user {UserId}", ipAddress, userId);
    }
}
