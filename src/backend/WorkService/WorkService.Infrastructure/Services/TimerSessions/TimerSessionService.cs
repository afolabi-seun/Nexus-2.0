using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkService.Application.DTOs.TimeEntries;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Services.TimeEntries;
using WorkService.Domain.Interfaces.Services.TimerSessions;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.TimerSessions;

public class TimerSessionService : ITimerSessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ITimeEntryService _timeEntryService;
    private readonly IStoryRepository _storyRepo;
    private readonly ILogger<TimerSessionService> _logger;

    private static readonly TimeSpan TimerTtl = TimeSpan.FromHours(24);

    public TimerSessionService(
        IConnectionMultiplexer redis,
        ITimeEntryService timeEntryService,
        IStoryRepository storyRepo,
        ILogger<TimerSessionService> logger)
    {
        _redis = redis;
        _timeEntryService = timeEntryService;
        _storyRepo = storyRepo;
        _logger = logger;
    }

    public async Task<object> StartAsync(Guid userId, Guid storyId, Guid orgId, CancellationToken ct = default)
    {
        IDatabase db;
        try
        {
            db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis unavailable for timer start");
            throw new DomainException(503, "SERVICE_UNAVAILABLE",
                "Timer service is temporarily unavailable.", HttpStatusCode.ServiceUnavailable);
        }

        // Check for existing active timer
        var existingKey = await ScanForActiveTimerKey(db, userId);
        if (existingKey != null)
            throw new TimerAlreadyActiveException(userId);

        // Validate story exists
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);

        var key = RedisKeys.Timer(userId, storyId);
        var payload = JsonSerializer.Serialize(new
        {
            storyId,
            startTime = DateTime.UtcNow,
            organizationId = orgId
        });

        await db.StringSetAsync(key, payload, TimerTtl);

        return new TimerStatusResponse
        {
            StoryId = storyId,
            StartTime = DateTime.UtcNow,
            ElapsedSeconds = 0
        };
    }

    public async Task<object> StopAsync(Guid userId, Guid orgId, CancellationToken ct = default)
    {
        IDatabase db;
        try
        {
            db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis unavailable for timer stop");
            throw new DomainException(503, "SERVICE_UNAVAILABLE",
                "Timer service is temporarily unavailable.", HttpStatusCode.ServiceUnavailable);
        }

        var activeKey = await ScanForActiveTimerKey(db, userId);
        if (activeKey == null)
            throw new NoActiveTimerException(userId);

        var value = await db.StringGetAsync(activeKey);
        await db.KeyDeleteAsync(activeKey);

        if (value.IsNullOrEmpty)
            throw new NoActiveTimerException(userId);

        var session = JsonSerializer.Deserialize<TimerSessionData>(value!);
        if (session == null)
            throw new NoActiveTimerException(userId);

        var elapsed = DateTime.UtcNow - session.startTime;
        var durationMinutes = (int)Math.Max(1, Math.Round(elapsed.TotalMinutes));

        var createRequest = new CreateTimeEntryRequest
        {
            StoryId = session.storyId,
            DurationMinutes = durationMinutes,
            Date = DateTime.UtcNow.Date,
            IsBillable = true
        };

        return await _timeEntryService.CreateAsync(orgId, userId, createRequest, ct);
    }

    public async Task<object?> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        IDatabase db;
        try
        {
            db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis unavailable for timer status");
            throw new DomainException(503, "SERVICE_UNAVAILABLE",
                "Timer service is temporarily unavailable.", HttpStatusCode.ServiceUnavailable);
        }

        var activeKey = await ScanForActiveTimerKey(db, userId);
        if (activeKey == null)
            return null;

        var value = await db.StringGetAsync(activeKey);
        if (value.IsNullOrEmpty)
            return null;

        var session = JsonSerializer.Deserialize<TimerSessionData>(value!);
        if (session == null)
            return null;

        var elapsed = DateTime.UtcNow - session.startTime;

        return new TimerStatusResponse
        {
            StoryId = session.storyId,
            StartTime = session.startTime,
            ElapsedSeconds = (long)elapsed.TotalSeconds
        };
    }

    private static async Task<string?> ScanForActiveTimerKey(IDatabase db, Guid userId)
    {
        var server = db.Multiplexer.GetServers().FirstOrDefault();
        if (server == null)
            return null;

        var pattern = RedisKeys.TimerPattern(userId);
        await foreach (var key in server.KeysAsync(database: db.Database, pattern: pattern, pageSize: 10))
        {
            return key.ToString();
        }

        return null;
    }

    private record TimerSessionData(Guid storyId, DateTime startTime, Guid organizationId);
}
