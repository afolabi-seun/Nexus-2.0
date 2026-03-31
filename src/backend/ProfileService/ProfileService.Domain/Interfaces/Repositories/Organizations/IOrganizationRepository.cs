using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.Organizations;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken ct = default);
    Task<Organization?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Organization?> GetByStoryIdPrefixAsync(string prefix, CancellationToken ct = default);
    Task<Organization> AddAsync(Organization organization, CancellationToken ct = default);
    Task UpdateAsync(Organization organization, CancellationToken ct = default);
    Task<(IEnumerable<Organization> Items, int TotalCount)> ListAllAsync(int page, int pageSize, CancellationToken ct = default);
}
