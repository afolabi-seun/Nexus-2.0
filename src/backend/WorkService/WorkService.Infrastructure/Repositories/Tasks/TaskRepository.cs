using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Repositories.Tasks;

public class TaskRepository : ITaskRepository
{
    private readonly WorkDbContext _db;

    public TaskRepository(WorkDbContext db) => _db = db;

    public async Task<Domain.Entities.Task?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
        => await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId, ct);

    public async Task<Domain.Entities.Task> AddAsync(Domain.Entities.Task task, CancellationToken ct = default)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async System.Threading.Tasks.Task UpdateAsync(Domain.Entities.Task task, CancellationToken ct = default)
    {
        _db.Tasks.Update(task);
        await _db.SaveChangesAsync(ct);
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
}
