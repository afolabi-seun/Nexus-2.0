using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.WorkflowStates;

public class WorkflowStateRepository : IWorkflowStateRepository
{
    private readonly UtilityDbContext _context;

    public WorkflowStateRepository(UtilityDbContext context) => _context = context;

    public async Task<IEnumerable<WorkflowState>> ListAsync(CancellationToken ct = default)
        => await _context.WorkflowStates.AsNoTracking().OrderBy(e => e.EntityType).ThenBy(e => e.SortOrder).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<WorkflowState> states, CancellationToken ct = default)
    {
        _context.WorkflowStates.AddRange(states);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string entityType, string stateName, CancellationToken ct = default)
        => await _context.WorkflowStates.AnyAsync(e => e.EntityType == entityType && e.StateName == stateName, ct);
}
