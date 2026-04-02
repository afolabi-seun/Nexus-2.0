using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;

namespace WorkService.Domain.Interfaces.Repositories.TimeApprovals;

public interface ITimeApprovalRepository : IGenericRepository<TimeApproval>
{
    Task<IEnumerable<TimeApproval>> GetByTimeEntryAsync(Guid timeEntryId, CancellationToken ct = default);
}
