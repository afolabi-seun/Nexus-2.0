using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.RiskRegisters;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.RiskRegisters;

public class RiskRegisterRepository : GenericRepository<RiskRegister>, IRiskRegisterRepository
{
    private readonly WorkDbContext _db;

    public RiskRegisterRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<RiskRegister> Items, int TotalCount)> ListAsync(
        Guid organizationId, Guid projectId, Guid? sprintId,
        string? severity, string? mitigationStatus,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.RiskRegisters
            .Where(r => r.OrganizationId == organizationId && r.ProjectId == projectId);

        if (sprintId.HasValue)
            query = query.Where(r => r.SprintId == sprintId.Value);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(r => r.Severity == severity);

        if (!string.IsNullOrEmpty(mitigationStatus))
            query = query.Where(r => r.MitigationStatus == mitigationStatus);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountActiveByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _db.RiskRegisters.CountAsync(r => r.ProjectId == projectId, ct);

    public async Task<IEnumerable<RiskRegister>> GetActiveByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _db.RiskRegisters
            .Where(r => r.ProjectId == projectId)
            .ToListAsync(ct);
}
