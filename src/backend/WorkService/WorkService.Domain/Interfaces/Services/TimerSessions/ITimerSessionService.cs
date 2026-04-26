using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.TimerSessions;

public interface ITimerSessionService
{
    Task<ServiceResult<object>> StartAsync(Guid userId, Guid storyId, Guid orgId, CancellationToken ct = default);
    Task<ServiceResult<object>> StopAsync(Guid userId, Guid orgId, CancellationToken ct = default);
    Task<ServiceResult<object?>> GetStatusAsync(Guid userId, CancellationToken ct = default);
}
