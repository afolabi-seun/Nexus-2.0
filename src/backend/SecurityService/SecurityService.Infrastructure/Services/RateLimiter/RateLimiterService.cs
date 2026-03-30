using Microsoft.Extensions.Logging;
using SecurityService.Domain.Interfaces.Services;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.RateLimiter;

public class RateLimiterService : IRateLimiterService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimiterService> _logger;

    private const string LuaScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local windowStart = tonumber(ARGV[2])
        local maxRequests = tonumber(ARGV[3])

        redis.call('ZREMRANGEBYSCORE', key, '-inf', windowStart)
        local currentCount = redis.call('ZCARD', key)

        if currentCount < maxRequests then
            redis.call('ZADD', key, now, now .. ':' .. math.random(100000))
            redis.call('EXPIRE', key, ARGV[4])
            return {1, 0}
        end

        local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
        local retryAfter = 0
        if #oldest >= 2 then
            retryAfter = tonumber(oldest[2]) + tonumber(ARGV[4]) - now
            if retryAfter < 0 then retryAfter = 0 end
        end

        return {0, retryAfter}
    ";

    public RateLimiterService(IConnectionMultiplexer redis, ILogger<RateLimiterService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<(bool IsAllowed, int RetryAfterSeconds)> CheckRateLimitAsync(
        string identity, string endpoint, int maxRequests, TimeSpan window, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"rate:{identity}:{endpoint}";
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStartMs = nowMs - (long)window.TotalMilliseconds;
        var windowSeconds = (int)window.TotalSeconds;

        var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
            LuaScript,
            new RedisKey[] { key },
            new RedisValue[] { nowMs, windowStartMs, maxRequests, windowSeconds });

        if (result is null || result.Length < 2)
        {
            _logger.LogWarning("Rate limiter Lua script returned unexpected result for key {Key}", key);
            return (true, 0);
        }

        var isAllowed = (int)result[0] == 1;
        var retryAfterMs = (int)result[1];
        var retryAfterSeconds = (int)Math.Ceiling(retryAfterMs / 1000.0);

        if (!isAllowed)
        {
            _logger.LogInformation(
                "Rate limit exceeded for {Identity} on {Endpoint}. Retry after {RetryAfter}s",
                identity, endpoint, retryAfterSeconds);
        }

        return (isAllowed, retryAfterSeconds);
    }
}
