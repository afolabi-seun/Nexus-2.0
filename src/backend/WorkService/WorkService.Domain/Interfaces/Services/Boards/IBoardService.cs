using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Boards;

public interface IBoardService
{
    Task<ServiceResult<object>> GetKanbanBoardAsync(Guid organizationId, Guid? projectId, Guid? sprintId, Guid? departmentId, Guid? assigneeId, string? priority, List<string>? labels, CancellationToken ct = default);
    Task<ServiceResult<object>> GetSprintBoardAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetBacklogAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetDepartmentBoardAsync(Guid organizationId, Guid? projectId, Guid? sprintId, CancellationToken ct = default);
}
