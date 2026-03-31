using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories;

public interface IStoryLinkRepository
{
    Task<StoryLink?> GetByIdAsync(Guid linkId, CancellationToken ct = default);
    Task<StoryLink> AddAsync(StoryLink link, CancellationToken ct = default);
    Task RemoveAsync(StoryLink link, CancellationToken ct = default);
    Task<IEnumerable<StoryLink>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<StoryLink?> FindInverseAsync(Guid targetStoryId, Guid sourceStoryId, string linkType, CancellationToken ct = default);
}
