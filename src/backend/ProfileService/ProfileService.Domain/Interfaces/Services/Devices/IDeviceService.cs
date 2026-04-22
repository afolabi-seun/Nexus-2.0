using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Devices;

public interface IDeviceService
{
    Task<ServiceResult<object>> ListAsync(Guid memberId, CancellationToken ct = default);
    Task<ServiceResult<object>> SetPrimaryAsync(Guid memberId, Guid deviceId, CancellationToken ct = default);
    Task<ServiceResult<object>> RemoveAsync(Guid memberId, Guid deviceId, CancellationToken ct = default);
}
