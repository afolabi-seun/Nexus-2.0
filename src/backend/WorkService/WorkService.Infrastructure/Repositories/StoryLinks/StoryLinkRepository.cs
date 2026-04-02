using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.StoryLinks;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.StoryLinks;

public class StoryLinkRepository : GenericRepository<StoryLink>, IStoryLinkRepository
{
    private readonly WorkDbContext _db;

    public StoryLinkRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StoryLink>> ListByStoryAsync(Guid storyId, CancellationToken ct = default)
        => await _db.StoryLinks
            .Where(l => l.SourceStoryId == storyId || l.TargetStoryId == storyId)
            .ToListAsync(ct);

    public async Task<StoryLink?> FindInverseAsync(Guid targetStoryId, Guid sourceStoryId, string linkType, CancellationToken ct = default)
        => await _db.StoryLinks
            .FirstOrDefaultAsync(l => l.SourceStoryId == targetStoryId && l.TargetStoryId == sourceStoryId && l.LinkType == linkType, ct);
}
