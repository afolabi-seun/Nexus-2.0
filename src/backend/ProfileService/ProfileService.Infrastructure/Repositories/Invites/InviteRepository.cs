using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.Invites;

public class InviteRepository : IInviteRepository
{
    private readonly ProfileDbContext _context;

    public InviteRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Invite?> GetByIdAsync(Guid inviteId, CancellationToken ct = default)
    {
        return await _context.Invites
            .Include(i => i.Department)
            .Include(i => i.Role)
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.InviteId == inviteId, ct);
    }

    public async Task<Invite?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.Invites
            .IgnoreQueryFilters()
            .Include(i => i.Department)
            .Include(i => i.Role)
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token, ct);
    }

    public async Task<Invite> AddAsync(Invite invite, CancellationToken ct = default)
    {
        await _context.Invites.AddAsync(invite, ct);
        await _context.SaveChangesAsync(ct);
        return invite;
    }

    public async Task UpdateAsync(Invite invite, CancellationToken ct = default)
    {
        _context.Invites.Update(invite);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<Invite> Items, int TotalCount)> ListPendingAsync(Guid organizationId, Guid? departmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Invites
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
