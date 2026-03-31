using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories;

public interface IStoryLabelRepository
{
    Task<StoryLabel?> GetAsync(Guid storyId, Guid labelId, CancellationToken ct = default);
    Task<StoryLabel> AddAsync(StoryLabel storyLabel, CancellationToken ct = default);
    Task RemoveAsync(StoryLabel storyLabel, CancellationToken ct = default);
    Task<int> CountByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<IEnumerable<StoryLabel>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
}
