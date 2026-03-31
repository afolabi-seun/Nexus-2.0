namespace WorkService.Domain.Interfaces.Services;

public interface ITaskService
{
    Task<object> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid taskId, CancellationToken ct = default);
    Task<object> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid taskId, Guid actorId, object request, CancellationToken ct = default);
    Task DeleteAsync(Guid taskId, CancellationToken ct = default);
    Task<object> TransitionStatusAsync(Guid taskId, Guid actorId, string newStatus, CancellationToken ct = default);
    Task<object> AssignAsync(Guid taskId, Guid actorId, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default);
    Task<object> SelfAssignAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task UnassignAsync(Guid taskId, Guid actorId, CancellationToken ct = default);
    Task LogHoursAsync(Guid taskId, Guid actorId, decimal hours, string? description, CancellationToken ct = default);
    Task<object> SuggestAssigneeAsync(string taskType, Guid organizationId, CancellationToken ct = default);
}
