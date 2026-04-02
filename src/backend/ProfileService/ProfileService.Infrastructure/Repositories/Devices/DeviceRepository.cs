using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Devices;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.Devices;

public class DeviceRepository : GenericRepository<Device>, IDeviceRepository
{
    private readonly ProfileDbContext _db;

    public DeviceRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Device>> ListByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _db.Devices
            .Where(d => d.TeamMemberId == memberId)
            .OrderByDescending(d => d.LastActiveDate)
            .ToListAsync(ct);
    }

    public async Task<int> CountByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _db.Devices.CountAsync(d => d.TeamMemberId == memberId, ct);
    }

    public async Task ClearPrimaryAsync(Guid memberId, CancellationToken ct = default)
    {
        var devices = await _db.Devices
            .Where(d => d.TeamMemberId == memberId && d.IsPrimary)
            .ToListAsync(ct);

        foreach (var device in devices)
        {
            device.IsPrimary = false;
        }
    }
}
