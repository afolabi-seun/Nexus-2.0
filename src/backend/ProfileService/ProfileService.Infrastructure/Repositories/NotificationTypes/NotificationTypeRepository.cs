using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.NotificationTypes;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.NotificationTypes;

public class NotificationTypeRepository : GenericRepository<NotificationType>, INotificationTypeRepository
{
    private readonly ProfileDbContext _db;

    public NotificationTypeRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<NotificationType>> ListAsync(CancellationToken ct = default)
    {
        return await _db.NotificationTypes.OrderBy(nt => nt.TypeName).ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(string typeName, CancellationToken ct = default)
    {
        return await _db.NotificationTypes.AnyAsync(nt => nt.TypeName == typeName, ct);
    }
}
