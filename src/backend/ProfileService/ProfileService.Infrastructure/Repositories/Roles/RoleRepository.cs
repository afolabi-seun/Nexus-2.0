using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.Roles;

public class RoleRepository : IRoleRepository
{
    private readonly ProfileDbContext _context;

    public RoleRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken ct = default)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, ct);
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken ct = default)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, ct);
    }

    public async Task<IEnumerable<Role>> ListAsync(CancellationToken ct = default)
    {
        return await _context.Roles.OrderByDescending(r => r.PermissionLevel).ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken ct = default)
    {
        await _context.Roles.AddRangeAsync(roles, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string roleName, CancellationToken ct = default)
    {
        return await _context.Roles.AnyAsync(r => r.RoleName == roleName, ct);
    }
}
