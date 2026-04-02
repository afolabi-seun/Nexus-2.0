using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.StoryLinks;

public interface IStoryLinkRepository : IGenericRepository<StoryLink>
{
    Task<IEnumerable<StoryLink>> ListByStoryAsync(Guid storyId, CancellationToken ct = default);
    Task<StoryLink?> FindInverseAsync(Guid targetStoryId, Guid sourceStoryId, string linkType, CancellationToken ct = default);
}
