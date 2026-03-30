using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken ct = default);
    Task<IEnumerable<Device>> ListByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<int> CountByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<Device> AddAsync(Device device, CancellationToken ct = default);
    Task UpdateAsync(Device device, CancellationToken ct = default);
    Task RemoveAsync(Device device, CancellationToken ct = default);
    Task ClearPrimaryAsync(Guid memberId, CancellationToken ct = default);
}
