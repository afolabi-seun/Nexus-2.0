using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;

namespace WorkService.Domain.Interfaces.Repositories.StoryTemplates;

public interface IStoryTemplateRepository : IGenericRepository<StoryTemplate>
{
    Task<(IEnumerable<StoryTemplate> Items, int TotalCount)> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
    Task<StoryTemplate?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default);
}
