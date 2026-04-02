using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.Invites;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.Invites;

public class InviteRepository : GenericRepository<Invite>, IInviteRepository
{
    private readonly ProfileDbContext _db;

    public InviteRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Invite?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _db.Invites
            .IgnoreQueryFilters()
            .Include(i => i.Department)
            .Include(i => i.Role)
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token, ct);
    }

    public async Task<(IEnumerable<Invite> Items, int TotalCount)> ListPendingAsync(Guid organizationId, Guid? departmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Invites
            .Include(i => i.Department)
            .Include(i => i.Role)
            .Where(i => i.OrganizationId == organizationId && i.FlgStatus == InviteStatuses.Active);

        if (departmentId.HasValue)
        {
            query = query.Where(i => i.DepartmentId == departmentId.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
