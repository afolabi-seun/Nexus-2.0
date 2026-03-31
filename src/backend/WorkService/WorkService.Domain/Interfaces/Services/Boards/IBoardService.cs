namespace WorkService.Domain.Interfaces.Services.Boards;

public interface IBoardService
{
    Task<object> GetKanbanBoardAsync(Guid organizationId, Guid? projectId, Guid? sprintId, Guid? departmentId, Guid? assigneeId, string? priority, List<string>? labels, CancellationToken ct = default);
    Task<object> GetSprintBoardAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default);
    Task<object> GetBacklogAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default);
    Task<object> GetDepartmentBoardAsync(Guid organizationId, Guid? projectId, Guid? sprintId, CancellationToken ct = default);
}
