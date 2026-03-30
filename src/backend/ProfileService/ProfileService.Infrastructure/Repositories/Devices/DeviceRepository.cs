using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.Devices;

public class DeviceRepository : IDeviceRepository
{
    private readonly ProfileDbContext _context;

    public DeviceRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken ct = default)
    {
        return await _context.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId, ct);
    }

    public async Task<IEnumerable<Device>> ListByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.Devices
            .Where(d => d.TeamMemberId == memberId)
            .OrderByDescending(d => d.LastActiveDate)
            .ToListAsync(ct);
    }

    public async Task<int> CountByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.Devices.CountAsync(d => d.TeamMemberId == memberId, ct);
    }

    public async Task<Device> AddAsync(Device device, CancellationToken ct = default)
    {
        await _context.Devices.AddAsync(device, ct);
        await _context.SaveChangesAsync(ct);
        return device;
    }

    public async Task UpdateAsync(Device device, CancellationToken ct = default)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Device device, CancellationToken ct = default)
    {
        _context.Devices.Remove(device);
        await _context.SaveChangesAsync(ct);
    }

    public async Task ClearPrimaryAsync(Guid memberId, CancellationToken ct = default)
    {
        var devices = await _context.Devices
            .Where(d => d.TeamMemberId == memberId && d.IsPrimary)
            .ToListAsync(ct);

        foreach (var device in devices)
        {
            device.IsPrimary = false;
        }

        await _context.SaveChangesAsync(ct);
    }
}
