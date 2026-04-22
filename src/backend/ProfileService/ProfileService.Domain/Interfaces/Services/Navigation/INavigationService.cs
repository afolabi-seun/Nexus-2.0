using ProfileService.Domain.Entities;
using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Navigation;

public interface INavigationService
{
    Task<ServiceResult<object>> GetNavigationAsync(int userPermissionLevel, CancellationToken ct = default);
    Task<ServiceResult<object>> GetAllNavigationItemsAsync(CancellationToken ct = default);
    Task<ServiceResult<object>> CreateAsync(NavigationItem item, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(NavigationItem item, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid id, CancellationToken ct = default);
}
