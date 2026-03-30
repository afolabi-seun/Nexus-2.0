using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string roleName, CancellationToken ct = default);
    Task<IEnumerable<Role>> ListAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken ct = default);
    Task<bool> ExistsAsync(string roleName, CancellationToken ct = default);
}
