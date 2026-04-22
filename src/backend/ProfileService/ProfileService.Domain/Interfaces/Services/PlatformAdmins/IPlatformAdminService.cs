using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.PlatformAdmins;

public interface IPlatformAdminService
{
    Task<ServiceResult<object>> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdatePasswordAsync(Guid platformAdminId, string passwordHash, CancellationToken ct = default);
}
