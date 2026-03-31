using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Stories;

public interface IStoryRepository
{
    Task<Story?> GetByIdAsync(Guid storyId, CancellationToken ct = default);
    Task<Story?> GetByKeyAsync(Guid organizationId, string storyKey, CancellationToken ct = default);
    Task<Story> AddAsync(Story story, CancellationToken ct = default);
    Task UpdateAsync(Story story, CancellationToken ct = default);
    Task<(IEnumerable<Story> Items, int TotalCount)> ListAsync(Guid organizationId, int page, int pageSize, Guid? projectId, string? status, string? priority, Guid? departmentId, Guid? assigneeId, Guid? sprintId, List<string>? labels, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<(IEnumerable<Story> Items, int TotalCount)> SearchAsync(Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountTasksAsync(Guid storyId, CancellationToken ct = default);
    Task<int> CountCompletedTasksAsync(Guid storyId, CancellationToken ct = default);
    Task<bool> AllDevTasksDoneAsync(Guid storyId, CancellationToken ct = default);
    Task<bool> AllTasksDoneAsync(Guid storyId, CancellationToken ct = default);
    Task<bool> ExistsByProjectAsync(Guid projectId, CancellationToken ct = default);
}
