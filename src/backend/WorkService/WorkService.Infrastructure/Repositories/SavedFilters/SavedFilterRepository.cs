using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.SavedFilters;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.SavedFilters;

public class SavedFilterRepository : GenericRepository<SavedFilter>, ISavedFilterRepository
{
    private readonly WorkDbContext _db;

    public SavedFilterRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SavedFilter>> ListByMemberAsync(Guid organizationId, Guid memberId, CancellationToken ct = default)
        => await _db.SavedFilters
            .Where(f => f.OrganizationId == organizationId && f.TeamMemberId == memberId)
            .OrderByDescending(f => f.DateCreated)
            .ToListAsync(ct);
}
