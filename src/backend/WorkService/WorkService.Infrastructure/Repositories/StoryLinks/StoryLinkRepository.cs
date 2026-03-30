using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.StoryLinks;

public class StoryLinkRepository : IStoryLinkRepository
{
    private readonly WorkDbContext _db;

    public StoryLinkRepository(WorkDbContext db) => _db = db;

    public async Task<StoryLink?> GetByIdAsync(Guid linkId, CancellationToken ct = default)
        => await _db.StoryLinks.FirstOrDefaultAsync(l => l.StoryLinkId == linkId, ct);

    public async Task<StoryLink> AddAsync(StoryLink link, CancellationToken ct = default)
    {
        _db.StoryLinks.Add(link);
        await _db.SaveChangesAsync(ct);
        return link;
    }

    public async Task RemoveAsync(StoryLink link, CancellationToken ct = default)
    {
        _db.StoryLinks.Remove(link);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<StoryLink>> ListByStoryAsync(Guid storyId, CancellationToken ct = default)
        => await _db.StoryLinks
            .Where(l => l.SourceStoryId == storyId || l.TargetStoryId == storyId)
            .ToListAsync(ct);

    public async Task<StoryLink?> FindInverseAsync(Guid targetStoryId, Guid sourceStoryId, string linkType, CancellationToken ct = default)
        => await _db.StoryLinks
            .FirstOrDefaultAsync(l => l.SourceStoryId == targetStoryId && l.TargetStoryId == sourceStoryId && l.LinkType == linkType, ct);
}
