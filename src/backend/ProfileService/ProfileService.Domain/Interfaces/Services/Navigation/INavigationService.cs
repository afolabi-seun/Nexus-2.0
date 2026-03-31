using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Services.Navigation;

public interface INavigationService
{
    Task<List<NavigationItem>> GetNavigationAsync(int userPermissionLevel, CancellationToken ct = default);
    Task<List<NavigationItem>> GetAllNavigationItemsAsync(CancellationToken ct = default);
    Task<NavigationItem> CreateAsync(NavigationItem item, CancellationToken ct = default);
    Task<NavigationItem> UpdateAsync(NavigationItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
