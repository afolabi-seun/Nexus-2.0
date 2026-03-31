using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Tasks;

public interface ITaskRepository
{
    Task<Entities.Task?> GetByIdAsync(Guid taskId, CancellationToken ct = default);
    Task<Entities.Task> AddAsync(Entities.Task task, CancellationToken ct = default);
    Task UpdateAsync(Entities.Task task, CancellationToken ct = default);
    Task<IEnumerable<Entities.Task>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<int> CountActiveByAssigneeAsync(Guid assigneeId, CancellationToken ct = default);
    Task<IEnumerable<Entities.Task>> ListBySprintAsync(Guid sprintId, CancellationToken ct = default);
    Task<IEnumerable<Entities.Task>> ListByDepartmentAsync(Guid organizationId, Guid? sprintId, CancellationToken ct = default);
}
