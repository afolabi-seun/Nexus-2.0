using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.Roles;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<Role?> GetByNameAsync(string roleName, CancellationToken ct = default);
    Task<IEnumerable<Role>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string roleName, CancellationToken ct = default);
}
