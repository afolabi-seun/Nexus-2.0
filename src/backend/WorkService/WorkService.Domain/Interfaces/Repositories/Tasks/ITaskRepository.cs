using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Tasks;

public interface ITaskRepository : IGenericRepository<Entities.Task>
{
    Task<IEnumerable<Entities.Task>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<int> CountActiveByAssigneeAsync(Guid assigneeId, CancellationToken ct = default);
    Task<IEnumerable<Entities.Task>> ListBySprintAsync(Guid sprintId, CancellationToken ct = default);
    Task<IEnumerable<Entities.Task>> ListByDepartmentAsync(Guid organizationId, Guid? sprintId, CancellationToken ct = default);
}
