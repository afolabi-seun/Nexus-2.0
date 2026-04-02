using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.TimeEntries;

public interface ITimeEntryRepository : IGenericRepository<TimeEntry>
{
    Task<(IEnumerable<TimeEntry> Items, int TotalCount)> ListAsync(
        Guid organizationId, Guid? storyId, Guid? projectId, Guid? sprintId,
        Guid? memberId, DateTime? dateFrom, DateTime? dateTo,
        bool? isBillable, string? status, int page, int pageSize,
        CancellationToken ct = default);
    Task<int> GetDailyTotalMinutesAsync(Guid memberId, DateTime date, CancellationToken ct = default);
    Task<IEnumerable<TimeEntry>> GetApprovedBillableByProjectAsync(
        Guid projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<IEnumerable<TimeEntry>> GetApprovedBySprintAsync(
        Guid sprintId, CancellationToken ct = default);
}
