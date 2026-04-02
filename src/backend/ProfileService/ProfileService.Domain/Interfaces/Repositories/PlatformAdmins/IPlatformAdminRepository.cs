using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.PlatformAdmins;

public interface IPlatformAdminRepository : IGenericRepository<PlatformAdmin>
{
    Task<PlatformAdmin?> GetByUsernameAsync(string username, CancellationToken ct = default);
}
