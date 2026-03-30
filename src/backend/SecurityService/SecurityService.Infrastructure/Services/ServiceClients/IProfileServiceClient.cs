using SecurityService.Application.Contracts;

namespace SecurityService.Infrastructure.Services.ServiceClients;

public interface IProfileServiceClient
{
    Task<ProfileUserResponse> GetTeamMemberByEmailAsync(string email, CancellationToken ct = default);
    Task UpdatePasswordHashAsync(Guid memberId, string passwordHash, CancellationToken ct = default);
    Task SetIsFirstTimeUserAsync(Guid memberId, bool isFirstTimeUser, CancellationToken ct = default);
    Task<PlatformAdminResponse> GetPlatformAdminByUsernameAsync(string username, CancellationToken ct = default);
    Task UpdatePlatformAdminPasswordAsync(Guid platformAdminId, string passwordHash, CancellationToken ct = default);
}
