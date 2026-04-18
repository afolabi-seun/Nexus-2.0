using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityService.Domain.Interfaces.Services.Session;
using StackExchange.Redis;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Infrastructure.Services.Session;

public class SessionService : ISessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SessionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SessionService(IConnectionMultiplexer redis, ILogger<SessionService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task CreateSessionAsync(Guid userId, string deviceId, string ipAddress, string jti,
        DateTime tokenExpiry, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Session(userId, deviceId);

        var sessionData = new SessionData
        {
            SessionId = $"{userId}:{deviceId}",
            UserId = userId.ToString(),
            DeviceId = deviceId,
            IpAddress = ipAddress,
            Jti = jti,
            TokenExpiry = tokenExpiry,
            CreatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(sessionData, JsonOptions);
        var ttl = tokenExpiry - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero) ttl = TimeSpan.FromMinutes(1);

        await db.StringSetAsync(key, json, ttl);
        _logger.LogInformation("Session created for user {UserId} on device {DeviceId}", userId, deviceId);
    }

    public async Task<IEnumerable<SessionInfo>> GetSessionsAsync(Guid userId, int page, int pageSize,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var pattern = RedisKeys.SessionPattern(userId);
        var sessions = new List<SessionInfo>();

        var server = _redis.GetServers().First();
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            var json = await db.StringGetAsync(key);
            if (json.IsNullOrEmpty) continue;

            var data = JsonSerializer.Deserialize<SessionData>(json!, JsonOptions);
            if (data is null) continue;

            sessions.Add(new SessionInfo
            {
                SessionId = data.SessionId,
                DeviceId = data.DeviceId,
                IpAddress = data.IpAddress,
                CreatedAt = data.CreatedAt
            });
        }

        return sessions
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    public async Task RevokeSessionAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // sessionId format is "userId:deviceId"
        var key = RedisKeys.SessionById(sessionId);
        var json = await db.StringGetAsync(key);

        if (!json.IsNullOrEmpty)
        {
            var data = JsonSerializer.Deserialize<SessionData>(json!, JsonOptions);
            if (data is not null && !string.IsNullOrEmpty(data.Jti))
            {
                var remainingTtl = await db.KeyTimeToLiveAsync(key);
                if (remainingTtl.HasValue && remainingTtl.Value > TimeSpan.Zero)
                {
                    await db.StringSetAsync(RedisKeys.Blacklist(data.Jti), "1", remainingTtl.Value);
                }
            }
        }

        await db.KeyDeleteAsync(key);
        _logger.LogInformation("Session {SessionId} revoked for user {UserId}", sessionId, userId);
    }

    public async Task RevokeAllSessionsExceptCurrentAsync(Guid userId, string currentDeviceId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var pattern = RedisKeys.SessionPattern(userId);
        var currentKey = RedisKeys.SessionForDevice(userId, currentDeviceId);

        var server = _redis.GetServers().First();
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            if (key.ToString() == currentKey) continue;
            await RevokeSessionByKeyAsync(db, userId, key.ToString());
        }

        _logger.LogInformation("All sessions except device {DeviceId} revoked for user {UserId}",
            currentDeviceId, userId);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var pattern = RedisKeys.SessionPattern(userId);

        var server = _redis.GetServers().First();
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            await RevokeSessionByKeyAsync(db, userId, key.ToString());
        }

        _logger.LogInformation("All sessions revoked for user {UserId}", userId);
    }

    private async Task RevokeSessionByKeyAsync(IDatabase db, Guid userId, string key)
    {
        var json = await db.StringGetAsync(key);
        if (!json.IsNullOrEmpty)
        {
            var data = JsonSerializer.Deserialize<SessionData>(json!, JsonOptions);
            if (data is not null && !string.IsNullOrEmpty(data.Jti))
            {
                var remainingTtl = await db.KeyTimeToLiveAsync(key);
                if (remainingTtl.HasValue && remainingTtl.Value > TimeSpan.Zero)
                {
                    await db.StringSetAsync(RedisKeys.Blacklist(data.Jti), "1", remainingTtl.Value);
                }
            }
        }

        await db.KeyDeleteAsync(key);
    }

    private sealed class SessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Jti { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
