using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;

namespace WorkService.Infrastructure.Repositories.Tasks;

public class TaskRepository : GenericRepository<Domain.Entities.Task>, ITaskRepository
{
    private readonly WorkDbContext _db;

    public TaskRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Domain.Entities.Task>> ListByStoryAsync(Guid storyId, CancellationToken ct = default)
        => await _db.Tasks.Where(t => t.StoryId == storyId).OrderBy(t => t.DateCreated).ToListAsync(ct);

    public async Task<int> CountActiveByAssigneeAsync(Guid assigneeId, CancellationToken ct = default)
        => await _db.Tasks.CountAsync(t => t.AssigneeId == assigneeId && t.Status != "Done", ct);

    public async Task<IEnumerable<Domain.Entities.Task>> ListBySprintAsync(Guid sprintId, CancellationToken ct = default)
    {
        var storyIds = await _db.SprintStories
            .Where(ss => ss.SprintId == sprintId && ss.RemovedDate == null)
            .Select(ss => ss.StoryId)
            .ToListAsync(ct);

        return await _db.Tasks.Where(t => storyIds.Contains(t.StoryId)).ToListAsync(ct);
    }

    public async Task<IEnumerable<Domain.Entities.Task>> ListByDepartmentAsync(
        Guid organizationId, Guid? sprintId, CancellationToken ct = default)
    {
        var query = _db.Tasks.Where(t => t.OrganizationId == organizationId);

        if (sprintId.HasValue)
        {
            var storyIds = await _db.SprintStories
                .Where(ss => ss.SprintId == sprintId.Value && ss.RemovedDate == null)
                .Select(ss => ss.StoryId)
                .ToListAsync(ct);
            query = query.Where(t => storyIds.Contains(t.StoryId));
        }

        return await query.ToListAsync(ct);
    }

    public async Task<(IEnumerable<Domain.Entities.Task> Items, int TotalCount)> SearchAsync(
        Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default)
    {
        var tsQuery = string.Join(" & ", query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var searchQuery = _db.Tasks
            .Where(t => t.OrganizationId == organizationId)
            .Where(t => EF.Functions.ToTsVector("english",
                (t.Title ?? "") + " " + (t.Description ?? ""))
                .Matches(EF.Functions.ToTsQuery("english", tsQuery)));

        var totalCount = await searchQuery.CountAsync(ct);
        var items = await searchQuery
            .OrderByDescending(t => t.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
