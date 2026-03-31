using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.PlatformAdmins;

public interface IPlatformAdminRepository
{
    Task<PlatformAdmin?> GetByIdAsync(Guid platformAdminId, CancellationToken ct = default);
    Task<PlatformAdmin?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task UpdateAsync(PlatformAdmin admin, CancellationToken ct = default);
}
