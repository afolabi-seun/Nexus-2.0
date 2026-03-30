using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.Organizations;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly ProfileDbContext _context;

    public OrganizationRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await _context.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == organizationId, ct);
    }

    public async Task<Organization?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.Organizations.FirstOrDefaultAsync(o => o.OrganizationName == name, ct);
    }

    public async Task<Organization?> GetByStoryIdPrefixAsync(string prefix, CancellationToken ct = default)
    {
        return await _context.Organizations.FirstOrDefaultAsync(o => o.StoryIdPrefix == prefix, ct);
    }

    public async Task<Organization> AddAsync(Organization organization, CancellationToken ct = default)
    {
        await _context.Organizations.AddAsync(organization, ct);
        await _context.SaveChangesAsync(ct);
        return organization;
    }

    public async Task UpdateAsync(Organization organization, CancellationToken ct = default)
    {
        _context.Organizations.Update(organization);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<Organization> Items, int TotalCount)> ListAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Organizations.AsQueryable();
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(o => o.OrganizationName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, totalCount);
    }
}
