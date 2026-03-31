using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Project?> GetByKeyAsync(string projectKey, CancellationToken ct = default);
    Task<Project?> GetByNameAsync(Guid organizationId, string projectName, CancellationToken ct = default);
    Task<Project> AddAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task<(IEnumerable<Project> Items, int TotalCount)> ListAsync(Guid organizationId, int page, int pageSize, string? status, CancellationToken ct = default);
    Task<int> GetStoryCountAsync(Guid projectId, CancellationToken ct = default);
    Task<int> GetSprintCountAsync(Guid projectId, CancellationToken ct = default);
}
