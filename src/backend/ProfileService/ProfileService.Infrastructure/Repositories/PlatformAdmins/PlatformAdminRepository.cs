using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.PlatformAdmins;

public class PlatformAdminRepository : IPlatformAdminRepository
{
    private readonly ProfileDbContext _context;

    public PlatformAdminRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformAdmin?> GetByIdAsync(Guid platformAdminId, CancellationToken ct = default)
    {
        return await _context.PlatformAdmins.FirstOrDefaultAsync(pa => pa.PlatformAdminId == platformAdminId, ct);
    }

    public async Task<PlatformAdmin?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _context.PlatformAdmins.FirstOrDefaultAsync(pa => pa.Username == username, ct);
    }

    public async Task UpdateAsync(PlatformAdmin admin, CancellationToken ct = default)
    {
        _context.PlatformAdmins.Update(admin);
        await _context.SaveChangesAsync(ct);
    }
}
