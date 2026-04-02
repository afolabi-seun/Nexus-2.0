using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.Devices;

public interface IDeviceRepository : IGenericRepository<Device>
{
    Task<IEnumerable<Device>> ListByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<int> CountByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task ClearPrimaryAsync(Guid memberId, CancellationToken ct = default);
}
