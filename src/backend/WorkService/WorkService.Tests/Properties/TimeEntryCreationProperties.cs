using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Application.DTOs.TimeEntries;
using WorkService.Application.Validators;
using WorkService.Domain.Entities;
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
using WorkService.Tests.Helpers;

namespace WorkService.Tests.Properties;

/// <summary>
/// Feature: time-tracking-cost
/// Property 1: Time entry creation preserves all fields and applies defaults
/// Property 2: Non-positive duration is always rejected
/// **Validates: Requirements 1.1, 1.3, 1.4, 13.1, 13.2**
/// </summary>
public class TimeEntryCreationProperties
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _storyId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    private TimeEntryService BuildService(
        Mock<ITimeEntryRepository> timeEntryRepo,
        Mock<ITimePolicyRepository> timePolicyRepo,
        Mock<IStoryRepository> storyRepo)
    {
        var costRateRepo = new Mock<ICostRateRepository>();
        var timeApprovalRepo = new Mock<ITimeApprovalRepository>();
        var costRateResolver = new Mock<ICostRateResolver>();
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
    /// Property 1: Time entry creation preserves all fields and applies defaults.
    /// For any valid CreateTimeEntryRequest with positive durationMinutes, existing storyId,
    /// and valid date, creating a time entry produces a record where all submitted fields
    /// match the request, isBillable defaults to true, and status is Pending when
    /// ApprovalRequired is true or Approved when false.
    /// **Validates: Requirements 1.1, 1.4, 13.1, 13.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CreationPreservesFieldsAndAppliesDefaults(PositiveInt duration, bool approvalRequired)
    {
        var durationMinutes = Math.Min(duration.Get, 1440); // cap at 24h to avoid daily limit

        var timeEntryRepo = new Mock<ITimeEntryRepository>();
        TimeEntry? captured = null;
        timeEntryRepo.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, _) => captured = e)
            .ReturnsAsync((TimeEntry e, CancellationToken _) => e);
        timeEntryRepo.Setup(r => r.GetDailyTotalMinutesAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var timePolicyRepo = new Mock<ITimePolicyRepository>();
        timePolicyRepo.Setup(r => r.GetByOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimePolicy
            {
                ApprovalRequired = approvalRequired,
                MaxDailyHours = 24m,
                OvertimeThresholdHoursPerDay = 10m
            });

        var storyRepo = new Mock<IStoryRepository>();
        storyRepo.Setup(r => r.GetByIdAsync(_storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Story
            {
                StoryId = _storyId,
                OrganizationId = _orgId,
                ProjectId = _projectId,
                StoryKey = "TEST-1",
                ReporterId = Guid.NewGuid()
            });

        var sut = BuildService(timeEntryRepo, timePolicyRepo, storyRepo);

        var request = new CreateTimeEntryRequest
        {
            StoryId = _storyId,
            DurationMinutes = durationMinutes,
            Date = DateTime.UtcNow.Date.AddDays(-1),
            IsBillable = true,
            Notes = "property test notes"
        };

        var result = sut.CreateAsync(_orgId, _userId, request).GetAwaiter().GetResult();

        if (!result.IsSuccess || captured == null)
            return false;

        var expectedStatus = approvalRequired ? "Pending" : "Approved";

        return captured.StoryId == _storyId
            && captured.MemberId == _userId
            && captured.OrganizationId == _orgId
            && captured.DurationMinutes == durationMinutes
            && captured.IsBillable == true
            && captured.Status == expectedStatus
            && captured.Notes == "property test notes"
            && result.StatusCode == 201;
    }

    /// <summary>
    /// Property 2: Non-positive duration is always rejected.
    /// For any integer durationMinutes where durationMinutes &lt;= 0, the FluentValidation
    /// validator rejects the request. This tests the pure validation logic directly.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NonPositiveDuration_IsRejectedByValidator(int rawDuration)
    {
        // Constrain to non-positive values: 0 and negatives
        var durationMinutes = rawDuration > 0 ? -rawDuration : rawDuration;

        var validator = new CreateTimeEntryRequestValidator();

        var request = new CreateTimeEntryRequest
        {
            StoryId = Guid.NewGuid(),
            DurationMinutes = durationMinutes,
            Date = DateTime.UtcNow.Date.AddDays(-1),
            IsBillable = true
        };

        var validationResult = validator.Validate(request);

        // Non-positive duration must always fail validation
        return !validationResult.IsValid
            && validationResult.Errors.Any(e => e.PropertyName == "DurationMinutes");
    }
}
