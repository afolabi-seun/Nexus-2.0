using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.NotificationTypes;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.NotificationTypes;

public class NotificationTypeRepository : INotificationTypeRepository
{
    private readonly ProfileDbContext _context;

    public NotificationTypeRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotificationType>> ListAsync(CancellationToken ct = default)
    {
        return await _context.NotificationTypes.OrderBy(nt => nt.TypeName).ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<NotificationType> types, CancellationToken ct = default)
    {
        await _context.NotificationTypes.AddRangeAsync(types, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string typeName, CancellationToken ct = default)
    {
        return await _context.NotificationTypes.AnyAsync(nt => nt.TypeName == typeName, ct);
    }
}
