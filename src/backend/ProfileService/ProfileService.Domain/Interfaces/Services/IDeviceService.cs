namespace ProfileService.Domain.Interfaces.Services;

public interface IDeviceService
{
    Task<IEnumerable<object>> ListAsync(Guid memberId, CancellationToken ct = default);
    Task SetPrimaryAsync(Guid memberId, Guid deviceId, CancellationToken ct = default);
    Task RemoveAsync(Guid memberId, Guid deviceId, CancellationToken ct = default);
}
