using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Roles;

public interface IRoleService
{
    Task<ServiceResult<object>> ListAsync(CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid roleId, CancellationToken ct = default);
    Task<object?> GetByNameAsync(string roleName, CancellationToken ct = default);
}
