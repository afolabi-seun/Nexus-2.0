using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.StoryLabels;

public interface IStoryLabelRepository : IGenericRepository<StoryLabel>
{
    Task<StoryLabel?> GetAsync(Guid storyId, Guid labelId, CancellationToken ct = default);
    Task<int> CountByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<IEnumerable<StoryLabel>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
}
