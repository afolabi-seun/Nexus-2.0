using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.NotificationSettings;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.NotificationSettings;

public class NotificationSettingRepository : GenericRepository<NotificationSetting>, INotificationSettingRepository
{
    private readonly ProfileDbContext _db;

    public NotificationSettingRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<NotificationSetting>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _db.NotificationSettings
            .Include(ns => ns.NotificationType)
            .Where(ns => ns.TeamMemberId == memberId)
            .ToListAsync(ct);
    }

    public async Task<NotificationSetting?> GetAsync(Guid memberId, Guid notificationTypeId, CancellationToken ct = default)
    {
        return await _db.NotificationSettings
            .FirstOrDefaultAsync(ns => ns.TeamMemberId == memberId && ns.NotificationTypeId == notificationTypeId, ct);
    }
}
