using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Repositories.TimeApprovals;

public interface ITimeApprovalRepository
{
    Task<TimeApproval> AddAsync(TimeApproval approval, CancellationToken ct = default);
    Task<IEnumerable<TimeApproval>> GetByTimeEntryAsync(Guid timeEntryId, CancellationToken ct = default);
}
