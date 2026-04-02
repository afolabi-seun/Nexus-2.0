using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.WorkflowStates;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.WorkflowStates;

public class WorkflowStateRepository : GenericRepository<WorkflowState>, IWorkflowStateRepository
{
    private readonly UtilityDbContext _db;

    public WorkflowStateRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<IEnumerable<WorkflowState>> ListAsync(CancellationToken ct = default)
        => await _db.WorkflowStates.AsNoTracking().OrderBy(e => e.EntityType).ThenBy(e => e.SortOrder).ToListAsync(ct);

    public async Task<bool> ExistsAsync(string entityType, string stateName, CancellationToken ct = default)
        => await _db.WorkflowStates.AnyAsync(e => e.EntityType == entityType && e.StateName == stateName, ct);
}
