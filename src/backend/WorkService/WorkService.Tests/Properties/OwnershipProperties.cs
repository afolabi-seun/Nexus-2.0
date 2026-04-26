using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Application.DTOs.TimeEntries;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.CostRates;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.TimeApprovals;
using WorkService.Domain.Interfaces.Repositories.TimeEntries;
using WorkService.Domain.Interfaces.Repositories.TimePolicies;
using WorkService.Domain.Interfaces.Services.CostRates;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Infrastructure.Services.TimeEntries;
using WorkService.Tests.Generators;
using WorkService.Tests.Helpers;

namespace WorkService.Tests.Properties;

/// <summary>
/// Feature: time-tracking-cost
/// Property 6: Time entry ownership enforcement
/// Property 7: Updating an approved entry resets status to Pending
/// Property 8: Soft-delete sets FlgStatus to D
/// **Validates: Requirements 3.2, 3.3, 3.4, 3.5**
/// </summary>
public class OwnershipProperties
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _storyId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    private TimeEntryService BuildService(
        Mock<ITimeEntryRepository> timeEntryRepo)
    {
        var timePolicyRepo = new Mock<ITimePolicyRepository>();
        var costRateRepo = new Mock<ICostRateRepository>();
        var timeApprovalRepo = new Mock<ITimeApprovalRepository>();
        var costRateResolver = new Mock<ICostRateResolver>();
        var storyRepo = new Mock<IStoryRepository>();
        var projectRepo = new Mock<IProjectRepository>();
        var sprintStoryRepo = new Mock<ISprintStoryRepository>();
        var outbox = new Mock<IOutboxService>();
        var logger = new Mock<ILogger<TimeEntryService>>();
        var dbContext = TestWorkDbContextFactory.Create();

        return new TimeEntryService(
            timeEntryRepo.Object, timePolicyRepo.Object, costRateRepo.Object,
            timeApprovalRepo.Object, costRateResolver.Object, storyRepo.Object,
            projectRepo.Object, sprintStoryRepo.Object, outbox.Object,
            dbContext, logger.Object);
    }

    /// <summary>
    /// Property 6: Time entry ownership enforcement.
    /// For any time entry owned by member A, when member B (B ≠ A) attempts to update
    /// that entry, the operation throws InsufficientPermissionsException.
    /// **Validates: Requirements 3.2, 3.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NonOwnerUpdate_ThrowsInsufficientPermissions(PositiveInt duration)
    {
        var ownerId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var entry = TimeEntryGenerators.CreateEntry(_orgId, _storyId, ownerId,
            durationMinutes: duration.Get, status: "Pending");
        entry.TimeEntryId = entryId;

        var timeEntryRepo = new Mock<ITimeEntryRepository>();
        timeEntryRepo.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var sut = BuildService(timeEntryRepo);

        var updateRequest = new UpdateTimeEntryRequest { Notes = "updated by non-owner" };

        try
        {
            sut.UpdateAsync(entryId, nonOwnerId, updateRequest).GetAwaiter().GetResult();
            return false; // Should have thrown
        }
        catch (InsufficientPermissionsException)
        {
            return true; // Expected
        }
    }

    /// <summary>
    /// Property 6b: Non-owner delete also throws InsufficientPermissionsException.
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NonOwnerDelete_ThrowsInsufficientPermissions(PositiveInt duration)
    {
        var ownerId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var entry = TimeEntryGenerators.CreateEntry(_orgId, _storyId, ownerId,
            durationMinutes: duration.Get, status: "Pending");
        entry.TimeEntryId = entryId;

        var timeEntryRepo = new Mock<ITimeEntryRepository>();
        timeEntryRepo.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var sut = BuildService(timeEntryRepo);

        try
        {
            sut.DeleteAsync(entryId, nonOwnerId).GetAwaiter().GetResult();
            return false; // Should have thrown
        }
        catch (InsufficientPermissionsException)
        {
            return true; // Expected
        }
    }

    /// <summary>
    /// Property 7: Updating an approved entry resets status to Pending.
    /// For any time entry with status "Approved", when the owning member updates any field,
    /// the status is reset to "Pending".
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UpdatingApprovedEntry_ResetsStatusToPending(PositiveInt newDuration)
    {
        var ownerId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var entry = TimeEntryGenerators.CreateEntry(_orgId, _storyId, ownerId,
            durationMinutes: 60, status: "Approved");
        entry.TimeEntryId = entryId;

        TimeEntry? captured = null;
        var timeEntryRepo = new Mock<ITimeEntryRepository>();
        timeEntryRepo.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        timeEntryRepo.Setup(r => r.UpdateAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, _) => captured = e)
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        var sut = BuildService(timeEntryRepo);

        var updateRequest = new UpdateTimeEntryRequest
        {
            DurationMinutes = Math.Max(1, newDuration.Get % 1440)
        };

        var result = sut.UpdateAsync(entryId, ownerId, updateRequest).GetAwaiter().GetResult();

        return result.IsSuccess && captured != null && captured.Status == "Pending";
    }

    /// <summary>
    /// Property 8: Soft-delete sets FlgStatus to D.
    /// For any active time entry (FlgStatus = "A"), when the owning member deletes it,
    /// FlgStatus is set to "D".
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_SetsFlgStatusToD(PositiveInt duration)
    {
        var ownerId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var entry = TimeEntryGenerators.CreateEntry(_orgId, _storyId, ownerId,
            durationMinutes: duration.Get, status: "Pending", flgStatus: "A");
        entry.TimeEntryId = entryId;

        TimeEntry? captured = null;
        var timeEntryRepo = new Mock<ITimeEntryRepository>();
        timeEntryRepo.Setup(r => r.GetByIdAsync(entryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        timeEntryRepo.Setup(r => r.UpdateAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, _) => captured = e)
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        var sut = BuildService(timeEntryRepo);

        var result = sut.DeleteAsync(entryId, ownerId).GetAwaiter().GetResult();

        return result.IsSuccess
            && result.StatusCode == 204
            && captured != null
            && captured.FlgStatus == "D";
    }
}
