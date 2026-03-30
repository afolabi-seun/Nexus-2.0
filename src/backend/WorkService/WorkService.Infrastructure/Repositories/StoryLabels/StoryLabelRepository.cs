using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.StoryLabels;

public class StoryLabelRepository : IStoryLabelRepository
{
    private readonly WorkDbContext _db;

    public StoryLabelRepository(WorkDbContext db) => _db = db;

    public async Task<StoryLabel?> GetAsync(Guid storyId, Guid labelId, CancellationToken ct = default)
        => await _db.StoryLabels.FirstOrDefaultAsync(sl => sl.StoryId == storyId && sl.LabelId == labelId, ct);

    public async Task<StoryLabel> AddAsync(StoryLabel storyLabel, CancellationToken ct = default)
    {
        _db.StoryLabels.Add(storyLabel);
        await _db.SaveChangesAsync(ct);
        return storyLabel;
    }

    public async Task RemoveAsync(StoryLabel storyLabel, CancellationToken ct = default)
    {
        _db.StoryLabels.Remove(storyLabel);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CountByStoryAsync(Guid storyId, CancellationToken ct = default)
        => await _db.StoryLabels.CountAsync(sl => sl.StoryId == storyId, ct);

    public async Task<IEnumerable<StoryLabel>> ListByStoryAsync(Guid storyId, CancellationToken ct = default)
        => await _db.StoryLabels.Where(sl => sl.StoryId == storyId).ToListAsync(ct);
}
