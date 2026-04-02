using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.Organizations;

public interface IOrganizationRepository : IGenericRepository<Organization>
{
    Task<Organization?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Organization?> GetByStoryIdPrefixAsync(string prefix, CancellationToken ct = default);
    Task<(IEnumerable<Organization> Items, int TotalCount)> ListAllAsync(int page, int pageSize, CancellationToken ct = default);
}
