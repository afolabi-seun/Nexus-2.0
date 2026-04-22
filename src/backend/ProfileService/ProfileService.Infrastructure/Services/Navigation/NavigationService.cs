using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories.NavigationItems;
using ProfileService.Domain.Interfaces.Services.Navigation;
using ProfileService.Domain.Results;

namespace ProfileService.Infrastructure.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly INavigationItemRepository _repository;

    public NavigationService(INavigationItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<object>> GetNavigationAsync(int userPermissionLevel, CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);

        var filtered = items
            .Where(i => i.IsEnabled && i.MinPermissionLevel <= userPermissionLevel)
            .Select(i => FilterChildren(i, userPermissionLevel))
            .ToList();

        return ServiceResult<object>.Ok(filtered, "Navigation items retrieved.");
    }

    public async Task<ServiceResult<object>> GetAllNavigationItemsAsync(CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);
        return ServiceResult<object>.Ok(items, "All navigation items retrieved.");
    }

    public async Task<ServiceResult<object>> CreateAsync(NavigationItem item, CancellationToken ct = default)
    {
        var created = await _repository.CreateAsync(item, ct);
        return ServiceResult<object>.Created(created, "Navigation item created.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(NavigationItem item, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(item.NavigationItemId, ct)
            ?? throw new NotFoundException($"Navigation item {item.NavigationItemId} not found");

        existing.Label = item.Label;
        existing.Path = item.Path;
        existing.Icon = item.Icon;
        existing.SortOrder = item.SortOrder;
        existing.MinPermissionLevel = item.MinPermissionLevel;
        existing.IsEnabled = item.IsEnabled;

        var updated = await _repository.UpdateAsync(existing, ct);
        return ServiceResult<object>.Ok(updated, "Navigation item updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
        return ServiceResult<object>.Ok(null!, "Navigation item deleted.");
    }

    private static NavigationItem FilterChildren(NavigationItem item, int userPermissionLevel)
    {
        item.Children = item.Children
            .Where(c => c.IsEnabled && c.MinPermissionLevel <= userPermissionLevel)
            .OrderBy(c => c.SortOrder)
            .Select(c => FilterChildren(c, userPermissionLevel))
            .ToList();
        return item;
    }
}
