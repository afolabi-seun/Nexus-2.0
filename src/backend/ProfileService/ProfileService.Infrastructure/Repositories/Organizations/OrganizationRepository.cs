using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.Organizations;

public class OrganizationRepository : GenericRepository<Organization>, IOrganizationRepository
{
    private readonly ProfileDbContext _db;

    public OrganizationRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Organization?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Organizations.FirstOrDefaultAsync(o => o.OrganizationName == name, ct);
    }

    public async Task<Organization?> GetByStoryIdPrefixAsync(string prefix, CancellationToken ct = default)
    {
        return await _db.Organizations.FirstOrDefaultAsync(o => o.StoryIdPrefix == prefix, ct);
    }

    public async Task<(IEnumerable<Organization> Items, int TotalCount)> ListAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Organizations.AsQueryable();
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(o => o.OrganizationName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, totalCount);
    }
}
