using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.TimeEntries;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.TimeEntries;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly WorkDbContext _db;

    public TimeEntryRepository(WorkDbContext db) => _db = db;

    public async Task<TimeEntry?> GetByIdAsync(Guid timeEntryId, CancellationToken ct = default)
        => await _db.TimeEntries.FirstOrDefaultAsync(e => e.TimeEntryId == timeEntryId, ct);

    public async Task<TimeEntry> AddAsync(TimeEntry entry, CancellationToken ct = default)
    {
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task UpdateAsync(TimeEntry entry, CancellationToken ct = default)
    {
        _db.TimeEntries.Update(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<TimeEntry> Items, int TotalCount)> ListAsync(
        Guid organizationId, Guid? storyId, Guid? projectId, Guid? sprintId,
        Guid? memberId, DateTime? dateFrom, DateTime? dateTo,
        bool? isBillable, string? status, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.TimeEntries.Where(e => e.OrganizationId == organizationId);

        if (storyId.HasValue)
            query = query.Where(e => e.StoryId == storyId.Value);

        if (projectId.HasValue)
            query = query.Where(e =>
                _db.Stories.Where(s => s.ProjectId == projectId.Value)
                    .Select(s => s.StoryId)
                    .Contains(e.StoryId));

        if (sprintId.HasValue)
            query = query.Where(e =>
                _db.SprintStories
                    .Where(ss => ss.SprintId == sprintId.Value && ss.RemovedDate == null)
                    .Select(ss => ss.StoryId)
                    .Contains(e.StoryId));

        if (memberId.HasValue)
            query = query.Where(e => e.MemberId == memberId.Value);

        if (dateFrom.HasValue)
            query = query.Where(e => e.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.Date <= dateTo.Value);

        if (isBillable.HasValue)
            query = query.Where(e => e.IsBillable == isBillable.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> GetDailyTotalMinutesAsync(Guid memberId, DateTime date, CancellationToken ct = default)
        => await _db.TimeEntries
            .IgnoreQueryFilters()
            .Where(e => e.MemberId == memberId && e.Date == date.Date && e.FlgStatus == "A")
            .SumAsync(e => e.DurationMinutes, ct);

    public async Task<IEnumerable<TimeEntry>> GetApprovedBillableByProjectAsync(
        Guid projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var storyIds = _db.Stories
            .Where(s => s.ProjectId == projectId)
            .Select(s => s.StoryId);

        var query = _db.TimeEntries
            .Where(e => storyIds.Contains(e.StoryId)
                        && e.Status == "Approved"
                        && e.IsBillable == true);

        if (dateFrom.HasValue)
            query = query.Where(e => e.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.Date <= dateTo.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<IEnumerable<TimeEntry>> GetApprovedBySprintAsync(
        Guid sprintId, CancellationToken ct = default)
    {
        var storyIds = _db.SprintStories
            .Where(ss => ss.SprintId == sprintId && ss.RemovedDate == null)
            .Select(ss => ss.StoryId);

        return await _db.TimeEntries
            .Where(e => storyIds.Contains(e.StoryId) && e.Status == "Approved")
            .ToListAsync(ct);
    }
}
