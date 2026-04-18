using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Projects;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    private readonly WorkDbContext _db;

    public ProjectRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Project?> GetByKeyAsync(string projectKey, CancellationToken ct = default)
        => await _db.Projects.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.ProjectKey == projectKey, ct);

    public async Task<Project?> GetByNameAsync(Guid organizationId, string projectName, CancellationToken ct = default)
        => await _db.Projects
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.ProjectName == projectName, ct);

    public async Task<(IEnumerable<Project> Items, int TotalCount)> ListAsync(
        Guid organizationId, int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var query = _db.Projects.Where(p => p.OrganizationId == organizationId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.FlgStatus == status);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> GetStoryCountAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Stories.CountAsync(s => s.ProjectId == projectId, ct);

    public async Task<int> GetSprintCountAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Sprints.CountAsync(s => s.ProjectId == projectId, ct);

    public async Task<(IEnumerable<Project> Items, int TotalCount)> SearchAsync(
        Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default)
    {
        var tsQuery = string.Join(" & ", query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var searchQuery = _db.Projects
            .Where(p => p.OrganizationId == organizationId)
            .Where(p => EF.Functions.ToTsVector("english",
                (p.ProjectKey ?? "") + " " + (p.ProjectName ?? "") + " " + (p.Description ?? ""))
                .Matches(EF.Functions.ToTsQuery("english", tsQuery)));

        var totalCount = await searchQuery.CountAsync(ct);
        var items = await searchQuery
            .OrderByDescending(p => p.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
