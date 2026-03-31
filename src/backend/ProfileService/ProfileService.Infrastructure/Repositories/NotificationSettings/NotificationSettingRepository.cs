using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.NotificationSettings;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.NotificationSettings;

public class NotificationSettingRepository : INotificationSettingRepository
{
    private readonly ProfileDbContext _context;

    public NotificationSettingRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotificationSetting>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.NotificationSettings
            .Include(ns => ns.NotificationType)
            .Where(ns => ns.TeamMemberId == memberId)
            .ToListAsync(ct);
    }

    public async Task<NotificationSetting?> GetAsync(Guid memberId, Guid notificationTypeId, CancellationToken ct = default)
    {
        return await _context.NotificationSettings
            .FirstOrDefaultAsync(ns => ns.TeamMemberId == memberId && ns.NotificationTypeId == notificationTypeId, ct);
    }

    public async Task AddAsync(NotificationSetting setting, CancellationToken ct = default)
    {
        await _context.NotificationSettings.AddAsync(setting, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationSetting setting, CancellationToken ct = default)
    {
        _context.NotificationSettings.Update(setting);
        await _context.SaveChangesAsync(ct);
    }
}
