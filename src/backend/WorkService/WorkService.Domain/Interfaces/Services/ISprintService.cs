namespace WorkService.Domain.Interfaces.Services;

public interface ISprintService
{
    Task<object> CreateAsync(Guid organizationId, Guid projectId, object request, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid sprintId, CancellationToken ct = default);
    Task<object> ListAsync(Guid organizationId, int page, int pageSize, string? status, Guid? projectId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid sprintId, object request, CancellationToken ct = default);
    Task<object> StartAsync(Guid sprintId, CancellationToken ct = default);
    Task<object> CompleteAsync(Guid sprintId, CancellationToken ct = default);
    Task<object> CancelAsync(Guid sprintId, CancellationToken ct = default);
    Task AddStoryAsync(Guid sprintId, Guid storyId, CancellationToken ct = default);
    Task RemoveStoryAsync(Guid sprintId, Guid storyId, CancellationToken ct = default);
    Task<object> GetMetricsAsync(Guid sprintId, CancellationToken ct = default);
    Task<object> GetVelocityHistoryAsync(Guid organizationId, int count, CancellationToken ct = default);
    Task<object?> GetActiveSprintAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default);
}
