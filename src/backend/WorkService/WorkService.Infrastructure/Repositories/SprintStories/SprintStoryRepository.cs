using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.SprintStories;

public class SprintStoryRepository : GenericRepository<SprintStory>, ISprintStoryRepository
{
    private readonly WorkDbContext _db;

    public SprintStoryRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<SprintStory?> GetAsync(Guid sprintId, Guid storyId, CancellationToken ct = default)
        => await _db.SprintStories
            .FirstOrDefaultAsync(ss => ss.SprintId == sprintId && ss.StoryId == storyId && ss.RemovedDate == null, ct);

    public async Task<IEnumerable<SprintStory>> ListBySprintAsync(Guid sprintId, CancellationToken ct = default)
        => await _db.SprintStories
            .Where(ss => ss.SprintId == sprintId && ss.RemovedDate == null)
            .ToListAsync(ct);
}
