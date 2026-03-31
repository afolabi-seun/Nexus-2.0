using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.TimeApprovals;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Repositories.TimeApprovals;

public class TimeApprovalRepository : ITimeApprovalRepository
{
    private readonly WorkDbContext _db;

    public TimeApprovalRepository(WorkDbContext db) => _db = db;

    public async Task<TimeApproval> AddAsync(TimeApproval approval, CancellationToken ct = default)
    {
        _db.TimeApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);
        return approval;
    }

    public async Task<IEnumerable<TimeApproval>> GetByTimeEntryAsync(Guid timeEntryId, CancellationToken ct = default)
        => await _db.TimeApprovals
            .Where(a => a.TimeEntryId == timeEntryId)
            .OrderByDescending(a => a.DateCreated)
            .ToListAsync(ct);
}
