using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Stories;

public class StoryRepository : GenericRepository<Story>, IStoryRepository
{
    private readonly WorkDbContext _db;

    public StoryRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Story?> GetByKeyAsync(Guid organizationId, string storyKey, CancellationToken ct = default)
        => await _db.Stories
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.StoryKey == storyKey, ct);

    public async Task<(IEnumerable<Story> Items, int TotalCount)> ListAsync(
        Guid organizationId, int page, int pageSize, Guid? projectId, string? status,
        string? priority, Guid? departmentId, Guid? assigneeId, Guid? sprintId,
        List<string>? labels, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var query = _db.Stories.Where(s => s.OrganizationId == organizationId);

        if (projectId.HasValue) query = query.Where(s => s.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);
        if (!string.IsNullOrEmpty(priority)) query = query.Where(s => s.Priority == priority);
        if (departmentId.HasValue) query = query.Where(s => s.DepartmentId == departmentId.Value);
        if (assigneeId.HasValue) query = query.Where(s => s.AssigneeId == assigneeId.Value);
        if (sprintId.HasValue) query = query.Where(s => s.SprintId == sprintId.Value);
        if (dateFrom.HasValue) query = query.Where(s => s.DateCreated >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(s => s.DateCreated <= dateTo.Value);

        if (labels is { Count: > 0 })
        {
            var storyIdsWithLabels = _db.StoryLabels
                .Join(_db.Labels, sl => sl.LabelId, l => l.LabelId, (sl, l) => new { sl.StoryId, l.Name })
                .Where(x => labels.Contains(x.Name))
                .Select(x => x.StoryId)
                .Distinct();
            query = query.Where(s => storyIdsWithLabels.Contains(s.StoryId));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Story> Items, int TotalCount)> SearchAsync(
        Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default)
    {
        var tsQuery = string.Join(" & ", query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var searchQuery = _db.Stories
            .Where(s => s.OrganizationId == organizationId)
            .Where(s => EF.Functions.ToTsVector("english",
                (s.StoryKey ?? "") + " " + (s.Title ?? "") + " " + (s.Description ?? ""))
                .Matches(EF.Functions.ToTsQuery("english", tsQuery)));

        var totalCount = await searchQuery.CountAsync(ct);
        var items = await searchQuery
            .OrderByDescending(s => s.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountTasksAsync(Guid storyId, CancellationToken ct = default)
        => await _db.Tasks.CountAsync(t => t.StoryId == storyId, ct);

    public async Task<int> CountCompletedTasksAsync(Guid storyId, CancellationToken ct = default)
        => await _db.Tasks.CountAsync(t => t.StoryId == storyId && t.Status == "Done", ct);

    public async Task<bool> AllDevTasksDoneAsync(Guid storyId, CancellationToken ct = default)
    {
        var devTasks = await _db.Tasks
            .Where(t => t.StoryId == storyId && t.TaskType == "Development")
            .ToListAsync(ct);
        return devTasks.Count == 0 || devTasks.All(t => t.Status == "Done");
    }

    public async Task<bool> AllTasksDoneAsync(Guid storyId, CancellationToken ct = default)
    {
        var tasks = await _db.Tasks.Where(t => t.StoryId == storyId).ToListAsync(ct);
        return tasks.Count == 0 || tasks.All(t => t.Status == "Done");
    }

    public async Task<bool> ExistsByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Stories.AnyAsync(s => s.ProjectId == projectId, ct);
}
