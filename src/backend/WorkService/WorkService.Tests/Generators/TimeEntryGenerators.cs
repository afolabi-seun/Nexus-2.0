using WorkService.Application.DTOs.TimeEntries;
using WorkService.Domain.Entities;

namespace WorkService.Tests.Generators;

public static class TimeEntryGenerators
{
    public static CreateTimeEntryRequest CreateValidRequest(
        Guid storyId, int durationMinutes = 60, bool isBillable = true, string? notes = null) => new()
    {
        StoryId = storyId,
        DurationMinutes = durationMinutes,
        Date = DateTime.UtcNow.Date.AddDays(-1),
        IsBillable = isBillable,
        Notes = notes
    };

    public static TimeEntry CreateEntry(
        Guid orgId, Guid storyId, Guid memberId,
        int durationMinutes = 60, string status = "Pending",
        bool isBillable = true, string flgStatus = "A") => new()
    {
        TimeEntryId = Guid.NewGuid(),
        OrganizationId = orgId,
        StoryId = storyId,
        MemberId = memberId,
        DurationMinutes = durationMinutes,
        Date = DateTime.UtcNow.Date.AddDays(-1),
        IsBillable = isBillable,
        IsOvertime = false,
        Status = status,
        FlgStatus = flgStatus,
        DateCreated = DateTime.UtcNow,
        DateUpdated = DateTime.UtcNow
    };
}
