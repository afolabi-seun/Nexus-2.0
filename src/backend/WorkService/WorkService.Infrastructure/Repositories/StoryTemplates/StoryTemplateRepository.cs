using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.StoryTemplates;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;

namespace WorkService.Infrastructure.Repositories.StoryTemplates;

public class StoryTemplateRepository : GenericRepository<StoryTemplate>, IStoryTemplateRepository
{
    private readonly WorkDbContext _db;

    public StoryTemplateRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<StoryTemplate> Items, int TotalCount)> ListByOrganizationAsync(
        Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.StoryTemplates
            .Where(t => t.OrganizationId == organizationId && t.IsActive)
            .OrderBy(t => t.Name);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<StoryTemplate?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default)
    {
        return await _db.StoryTemplates
            .FirstOrDefaultAsync(t => t.OrganizationId == organizationId && t.Name == name && t.IsActive, ct);
    }
}
