using ProfileService.Application.DTOs.Devices;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories.Devices;
using ProfileService.Domain.Interfaces.Services.Devices;
using ProfileService.Domain.Results;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Services.Devices;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly ProfileDbContext _dbContext;

    public DeviceService(IDeviceRepository deviceRepo, ProfileDbContext dbContext)
    {
        _deviceRepo = deviceRepo;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> ListAsync(Guid memberId, CancellationToken ct = default)
    {
        var devices = await _deviceRepo.ListByMemberAsync(memberId, ct);
        var data = devices.Select(d => new DeviceResponse
        {
            DeviceId = d.DeviceId,
            DeviceName = d.DeviceName,
            DeviceType = d.DeviceType,
            IsPrimary = d.IsPrimary,
            IpAddress = d.IpAddress,
            UserAgent = d.UserAgent,
            LastActiveDate = d.LastActiveDate,
            FlgStatus = d.FlgStatus
        }).ToList();
        return ServiceResult<object>.Ok(data, "Devices retrieved.");
    }

    public async Task<ServiceResult<object>> SetPrimaryAsync(Guid memberId, Guid deviceId, CancellationToken ct = default)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId, ct)
            ?? throw new NotFoundException($"Device {deviceId} not found");

        if (device.TeamMemberId != memberId)
            throw new NotFoundException($"Device {deviceId} not found for member {memberId}");

        // Clear previous primary
        await _deviceRepo.ClearPrimaryAsync(memberId, ct);

        // Set new primary
        device.IsPrimary = true;
        await _deviceRepo.UpdateAsync(device, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.Ok(null!, "Primary device updated.");
    }

    public async Task<ServiceResult<object>> RemoveAsync(Guid memberId, Guid deviceId, CancellationToken ct = default)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId, ct)
            ?? throw new NotFoundException($"Device {deviceId} not found");

        if (device.TeamMemberId != memberId)
            throw new NotFoundException($"Device {deviceId} not found for member {memberId}");

        await _deviceRepo.DeleteAsync(device, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.Ok(null!, "Device removed.");
    }
}
