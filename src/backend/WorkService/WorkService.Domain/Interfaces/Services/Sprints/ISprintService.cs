using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Sprints;

public interface ISprintService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid projectId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid sprintId, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, string? status, Guid? projectId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid sprintId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> StartAsync(Guid sprintId, CancellationToken ct = default);
    Task<ServiceResult<object>> CompleteAsync(Guid sprintId, CancellationToken ct = default);
    Task<ServiceResult<object>> CancelAsync(Guid sprintId, CancellationToken ct = default);
    Task<ServiceResult<object>> AddStoryAsync(Guid sprintId, Guid storyId, CancellationToken ct = default);
    Task<ServiceResult<object>> RemoveStoryAsync(Guid sprintId, Guid storyId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetMetricsAsync(Guid sprintId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetVelocityHistoryAsync(Guid organizationId, int count, CancellationToken ct = default);
    Task<ServiceResult<object?>> GetActiveSprintAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default);
}
