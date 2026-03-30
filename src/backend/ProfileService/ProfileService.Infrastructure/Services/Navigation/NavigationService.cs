using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Domain.Interfaces.Services;

namespace ProfileService.Infrastructure.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly INavigationItemRepository _repository;

    public NavigationService(INavigationItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<NavigationItem>> GetNavigationAsync(int userPermissionLevel, CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(ct);

        return items
            .Where(i => i.IsEnabled && i.MinPermissionLevel <= userPermissionLevel)
            .Select(i => FilterChildren(i, userPermissionLevel))
            .ToList();
    }

    public async Task<List<NavigationItem>> GetAllNavigationItemsAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(ct);
    }

    public async Task<NavigationItem> CreateAsync(NavigationItem item, CancellationToken ct = default)
    {
        return await _repository.CreateAsync(item, ct);
    }

    public async Task<NavigationItem> UpdateAsync(NavigationItem item, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(item.NavigationItemId, ct)
            ?? throw new NotFoundException($"Navigation item {item.NavigationItemId} not found");

        existing.Label = item.Label;
        existing.Path = item.Path;
        existing.Icon = item.Icon;
        existing.SortOrder = item.SortOrder;
        existing.MinPermissionLevel = item.MinPermissionLevel;
        existing.IsEnabled = item.IsEnabled;

        return await _repository.UpdateAsync(existing, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
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
