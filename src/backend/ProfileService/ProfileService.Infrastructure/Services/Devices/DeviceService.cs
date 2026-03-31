using ProfileService.Application.DTOs.Devices;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories.Devices;
using ProfileService.Domain.Interfaces.Services.Devices;

namespace ProfileService.Infrastructure.Services.Devices;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepo;

    public DeviceService(IDeviceRepository deviceRepo)
    {
        _deviceRepo = deviceRepo;
    }

    public async Task<IEnumerable<object>> ListAsync(Guid memberId, CancellationToken ct = default)
    {
        var devices = await _deviceRepo.ListByMemberAsync(memberId, ct);
        return devices.Select(d => new DeviceResponse
        {
            DeviceId = d.DeviceId,
            DeviceName = d.DeviceName,
            DeviceType = d.DeviceType,
            IsPrimary = d.IsPrimary,
            IpAddress = d.IpAddress,
            UserAgent = d.UserAgent,
            LastActiveDate = d.LastActiveDate,
            FlgStatus = d.FlgStatus
        });
    }

    public async Task SetPrimaryAsync(Guid memberId, Guid deviceId, CancellationToken ct = default)
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
    }

    public async Task RemoveAsync(Guid memberId, Guid deviceId, CancellationToken ct = default)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId, ct)
            ?? throw new NotFoundException($"Device {deviceId} not found");

        if (device.TeamMemberId != memberId)
            throw new NotFoundException($"Device {deviceId} not found for member {memberId}");

        await _deviceRepo.RemoveAsync(device, ct);
    }
}
