using ProfileService.Application.DTOs.Roles;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Domain.Interfaces.Services;

namespace ProfileService.Infrastructure.Services.Roles;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepo;

    public RoleService(IRoleRepository roleRepo)
    {
        _roleRepo = roleRepo;
    }

    public async Task<IEnumerable<object>> ListAsync(CancellationToken ct = default)
    {
        var roles = await _roleRepo.ListAsync(ct);
        return roles.Select(r => new RoleResponse
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName,
            Description = r.Description,
            PermissionLevel = r.PermissionLevel,
            IsSystemRole = r.IsSystemRole
        });
    }

    public async Task<object> GetByIdAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, ct)
            ?? throw new NotFoundException($"Role {roleId} not found");

        return new RoleResponse
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description,
            PermissionLevel = role.PermissionLevel,
            IsSystemRole = role.IsSystemRole
        };
    }

    public async Task<object?> GetByNameAsync(string roleName, CancellationToken ct = default)
    {
        var role = await _roleRepo.GetByNameAsync(roleName, ct);
        if (role == null) return null;

        return new RoleResponse
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description,
            PermissionLevel = role.PermissionLevel,
            IsSystemRole = role.IsSystemRole
        };
    }
}
