namespace ProfileService.Domain.Interfaces.Services.Roles;

public interface IRoleService
{
    Task<IEnumerable<object>> ListAsync(CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid roleId, CancellationToken ct = default);
    Task<object?> GetByNameAsync(string roleName, CancellationToken ct = default);
}
