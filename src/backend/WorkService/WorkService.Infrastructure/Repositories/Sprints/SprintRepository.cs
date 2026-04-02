using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Sprints;

public class SprintRepository : GenericRepository<Sprint>, ISprintRepository
{
    private readonly WorkDbContext _db;

    public SprintRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<Sprint> Items, int TotalCount)> ListAsync(
        Guid organizationId, int page, int pageSize, string? status, Guid? projectId, CancellationToken ct = default)
    {
        var query = _db.Sprints.Where(s => s.OrganizationId == organizationId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);
        if (projectId.HasValue) query = query.Where(s => s.ProjectId == projectId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Sprint?> GetActiveByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Sprints.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Status == "Active", ct);

    public async Task<IEnumerable<Sprint>> GetCompletedAsync(Guid organizationId, int count, CancellationToken ct = default)
        => await _db.Sprints
            .Where(s => s.OrganizationId == organizationId && s.Status == "Completed")
            .OrderByDescending(s => s.EndDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task<bool> HasOverlappingAsync(
        Guid projectId, DateTime startDate, DateTime endDate, Guid? excludeSprintId, CancellationToken ct = default)
    {
        var query = _db.Sprints.Where(s =>
            s.ProjectId == projectId &&
            s.Status != "Cancelled" &&
            s.StartDate < endDate &&
            s.EndDate > startDate);

        if (excludeSprintId.HasValue)
            query = query.Where(s => s.SprintId != excludeSprintId.Value);

        return await query.AnyAsync(ct);
    }
}
