namespace ProfileService.Domain.Interfaces.Services;

public interface IPlatformAdminService
{
    Task<object> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task UpdatePasswordAsync(Guid platformAdminId, string passwordHash, CancellationToken ct = default);
}
