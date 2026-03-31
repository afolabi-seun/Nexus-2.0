using ProfileService.Application.DTOs.PlatformAdmins;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories.PlatformAdmins;
using ProfileService.Domain.Interfaces.Services.PlatformAdmins;

namespace ProfileService.Infrastructure.Services.PlatformAdmins;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly IPlatformAdminRepository _adminRepo;

    public PlatformAdminService(IPlatformAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<object> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var admin = await _adminRepo.GetByUsernameAsync(username, ct)
            ?? throw new NotFoundException($"PlatformAdmin with username '{username}' not found");

        return new PlatformAdminInternalResponse
        {
            PlatformAdminId = admin.PlatformAdminId,
            PasswordHash = admin.PasswordHash,
            FlgStatus = admin.FlgStatus,
            IsFirstTimeUser = admin.IsFirstTimeUser,
            Email = admin.Email
        };
    }

    public async Task UpdatePasswordAsync(Guid platformAdminId, string passwordHash, CancellationToken ct = default)
    {
        var admin = await _adminRepo.GetByIdAsync(platformAdminId, ct)
            ?? throw new NotFoundException($"PlatformAdmin {platformAdminId} not found");

        admin.PasswordHash = passwordHash;
        admin.DateUpdated = DateTime.UtcNow;
        await _adminRepo.UpdateAsync(admin, ct);
    }
}
