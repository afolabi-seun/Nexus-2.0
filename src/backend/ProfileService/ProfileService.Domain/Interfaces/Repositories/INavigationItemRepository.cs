using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories;

public interface INavigationItemRepository
{
    Task<List<NavigationItem>> GetAllAsync(CancellationToken ct = default);
    Task<NavigationItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NavigationItem> CreateAsync(NavigationItem item, CancellationToken ct = default);
    Task<NavigationItem> UpdateAsync(NavigationItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
