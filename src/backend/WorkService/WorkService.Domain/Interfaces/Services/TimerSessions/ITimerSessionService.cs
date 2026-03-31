namespace WorkService.Domain.Interfaces.Services.TimerSessions;

public interface ITimerSessionService
{
    Task<object> StartAsync(Guid userId, Guid storyId, Guid orgId, CancellationToken ct = default);
    Task<object> StopAsync(Guid userId, Guid orgId, CancellationToken ct = default);
    Task<object?> GetStatusAsync(Guid userId, CancellationToken ct = default);
}
