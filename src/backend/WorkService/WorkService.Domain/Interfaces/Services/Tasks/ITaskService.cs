using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Tasks;

public interface ITaskService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid taskId, CancellationToken ct = default);
    Task<ServiceResult<object>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid taskId, Guid actorId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid taskId, CancellationToken ct = default);
    Task<ServiceResult<object>> TransitionStatusAsync(Guid taskId, Guid actorId, string newStatus, CancellationToken ct = default);
    Task<ServiceResult<object>> AssignAsync(Guid taskId, Guid actorId, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default);
    Task<ServiceResult<object>> SelfAssignAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task<ServiceResult<object>> UnassignAsync(Guid taskId, Guid actorId, CancellationToken ct = default);
    Task<ServiceResult<object>> LogHoursAsync(Guid taskId, Guid actorId, decimal hours, string? description, CancellationToken ct = default);
    Task<ServiceResult<object>> SuggestAssigneeAsync(string taskType, Guid organizationId, CancellationToken ct = default);
}
