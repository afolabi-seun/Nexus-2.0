using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.NavigationItems;

public class NavigationItemRepository : INavigationItemRepository
{
    private readonly ProfileDbContext _context;

    public NavigationItemRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<List<NavigationItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.NavigationItems
            .Include(n => n.Children.OrderBy(c => c.SortOrder))
            .Where(n => n.ParentId == null)
            .OrderBy(n => n.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<NavigationItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.NavigationItems
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.NavigationItemId == id, ct);
    }

    public async Task<NavigationItem> CreateAsync(NavigationItem item, CancellationToken ct = default)
    {
        _context.NavigationItems.Add(item);
        await _context.SaveChangesAsync(ct);
        return item;
    }

    public async Task<NavigationItem> UpdateAsync(NavigationItem item, CancellationToken ct = default)
    {
        item.DateUpdated = DateTime.UtcNow;
        _context.NavigationItems.Update(item);
        await _context.SaveChangesAsync(ct);
        return item;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.NavigationItems.FindAsync(new object[] { id }, ct);
        if (item != null)
        {
            _context.NavigationItems.Remove(item);
            await _context.SaveChangesAsync(ct);
        }
    }
}
