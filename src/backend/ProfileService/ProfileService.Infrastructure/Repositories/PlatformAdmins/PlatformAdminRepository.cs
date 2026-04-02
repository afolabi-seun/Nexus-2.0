using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.PlatformAdmins;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.PlatformAdmins;

public class PlatformAdminRepository : GenericRepository<PlatformAdmin>, IPlatformAdminRepository
{
    private readonly ProfileDbContext _db;

    public PlatformAdminRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<PlatformAdmin?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _db.PlatformAdmins.FirstOrDefaultAsync(pa => pa.Username == username, ct);
    }
}
