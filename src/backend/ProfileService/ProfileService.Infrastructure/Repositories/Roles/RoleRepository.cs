using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.Roles;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    private readonly ProfileDbContext _db;

    public RoleRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken ct = default)
    {
        return await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, ct);
    }

    public async Task<IEnumerable<Role>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Roles.OrderByDescending(r => r.PermissionLevel).ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(string roleName, CancellationToken ct = default)
    {
        return await _db.Roles.AnyAsync(r => r.RoleName == roleName, ct);
    }
}
