using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
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
using WorkService.Domain.Interfaces.Services.TimeEntries;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.TimeEntries;

public class TimeEntryService : ITimeEntryService
{
    private readonly ITimeEntryRepository _timeEntryRepo;
    private readonly ITimePolicyRepository _timePolicyRepo;
    private readonly ICostRateRepository _costRateRepo;
    private readonly ITimeApprovalRepository _timeApprovalRepo;
    private readonly ICostRateResolver _costRateResolver;
    private readonly IStoryRepository _storyRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ISprintStoryRepository _sprintStoryRepo;
    private readonly IOutboxService _outbox;
    private readonly WorkDbContext _dbContext;
    private readonly ILogger<TimeEntryService> _logger;

    public TimeEntryService(
        ITimeEntryRepository timeEntryRepo,
        ITimePolicyRepository timePolicyRepo,
        ICostRateRepository costRateRepo,
        ITimeApprovalRepository timeApprovalRepo,
        ICostRateResolver costRateResolver,
        IStoryRepository storyRepo,
        IProjectRepository projectRepo,
        ISprintStoryRepository sprintStoryRepo,
        IOutboxService outbox,
        WorkDbContext dbContext,
        ILogger<TimeEntryService> logger)
    {
        _timeEntryRepo = timeEntryRepo;
        _timePolicyRepo = timePolicyRepo;
        _costRateRepo = costRateRepo;
        _timeApprovalRepo = timeApprovalRepo;
        _costRateResolver = costRateResolver;
        _storyRepo = storyRepo;
        _projectRepo = projectRepo;
        _sprintStoryRepo = sprintStoryRepo;
        _outbox = outbox;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default)
    {
        var req = (CreateTimeEntryRequest)request;

        var story = await _storyRepo.GetByIdAsync(req.StoryId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(4001, "STORY_NOT_FOUND", $"Story with ID '{req.StoryId}' was not found.", 404);

        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct)
            ?? new TimePolicy();

        var dailyTotalMinutes = await _timeEntryRepo.GetDailyTotalMinutesAsync(userId, req.Date, ct);
        var newTotalMinutes = dailyTotalMinutes + req.DurationMinutes;
        var maxDailyMinutes = (int)(policy.MaxDailyHours * 60);

        if (newTotalMinutes > maxDailyMinutes)
            return ServiceResult<object>.Fail(4056, "DAILY_HOURS_EXCEEDED", $"Adding this entry would exceed the daily maximum of {policy.MaxDailyHours} hours.", 400);

        var overtimeThresholdMinutes = (int)(policy.OvertimeThresholdHoursPerDay * 60);
        var isOvertime = newTotalMinutes > overtimeThresholdMinutes;

        var status = policy.ApprovalRequired ? "Pending" : "Approved";

        var entry = new TimeEntry
        {
            OrganizationId = orgId,
            StoryId = req.StoryId,
            MemberId = userId,
            DurationMinutes = req.DurationMinutes,
            Date = req.Date,
            IsBillable = req.IsBillable,
            IsOvertime = isOvertime,
            Status = status,
            Notes = req.Notes
        };

        await _timeEntryRepo.AddAsync(entry, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _outbox.PublishAsync(new
        {
            MessageType = "AuditEvent",
            Action = "TimeEntryCreated",
            EntityType = "TimeEntry",
            EntityId = entry.TimeEntryId.ToString(),
            OrganizationId = orgId,
            UserId = userId
        }, ct);

        return ServiceResult<object>.Created(MapToResponse(entry), "Time entry created successfully.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid timeEntryId, Guid userId, object request, CancellationToken ct = default)
    {
        var req = (UpdateTimeEntryRequest)request;

        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct);
        if (entry == null)
            return ServiceResult<object>.Fail(4052, "TIME_ENTRY_NOT_FOUND", $"Time entry with ID '{timeEntryId}' was not found.", 404);

        if (entry.MemberId != userId)
            throw new InsufficientPermissionsException();

        if (req.DurationMinutes.HasValue) entry.DurationMinutes = req.DurationMinutes.Value;
        if (req.Date.HasValue) entry.Date = req.Date.Value;
        if (req.IsBillable.HasValue) entry.IsBillable = req.IsBillable.Value;
        if (req.Notes != null) entry.Notes = req.Notes;

        if (entry.Status == "Approved")
            entry.Status = "Pending";

        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.Ok(MapToResponse(entry), "Time entry updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid timeEntryId, Guid userId, CancellationToken ct = default)
    {
        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct);
        if (entry == null)
            return ServiceResult<object>.Fail(4052, "TIME_ENTRY_NOT_FOUND", $"Time entry with ID '{timeEntryId}' was not found.", 404);

        if (entry.MemberId != userId)
            throw new InsufficientPermissionsException();

        entry.FlgStatus = "D";
        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Time entry deleted.");
    }

    public async Task<ServiceResult<object>> ListAsync(Guid orgId, Guid? storyId, Guid? projectId, Guid? sprintId,
        Guid? memberId, DateTime? dateFrom, DateTime? dateTo, bool? isBillable,
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _timeEntryRepo.ListAsync(
            orgId, storyId, projectId, sprintId, memberId,
            dateFrom, dateTo, isBillable, status, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();

        return ServiceResult<object>.Ok(new PaginatedResponse<TimeEntryResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }, "Time entries retrieved.");
    }

    public async Task<ServiceResult<object>> ApproveAsync(Guid timeEntryId, Guid approverId, string approverRole,
        Guid approverDeptId, CancellationToken ct = default)
    {
        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct);
        if (entry == null)
            return ServiceResult<object>.Fail(4052, "TIME_ENTRY_NOT_FOUND", $"Time entry with ID '{timeEntryId}' was not found.", 404);

        var policy = await _timePolicyRepo.GetByOrganizationAsync(entry.OrganizationId, ct)
            ?? new TimePolicy();

        await ValidateApproverAuthorization(entry, policy, approverId, approverRole, approverDeptId, ct);

        entry.Status = "Approved";
        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);

        var approval = new TimeApproval
        {
            OrganizationId = entry.OrganizationId,
            TimeEntryId = timeEntryId,
            ApproverId = approverId,
            Action = "Approved"
        };
        await _timeApprovalRepo.AddAsync(approval, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _outbox.PublishAsync(new
        {
            MessageType = "NotificationRequest",
            Action = "TimeEntryApproved",
            EntityType = "TimeEntry",
            EntityId = timeEntryId.ToString(),
            NotificationType = "TimeEntryApproved"
        }, ct);

        return ServiceResult<object>.Ok(MapToResponse(entry), "Time entry approved.");
    }

    public async Task<ServiceResult<object>> RejectAsync(Guid timeEntryId, Guid approverId, string approverRole,
        Guid approverDeptId, string reason, CancellationToken ct = default)
    {
        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct);
        if (entry == null)
            return ServiceResult<object>.Fail(4052, "TIME_ENTRY_NOT_FOUND", $"Time entry with ID '{timeEntryId}' was not found.", 404);

        var policy = await _timePolicyRepo.GetByOrganizationAsync(entry.OrganizationId, ct)
            ?? new TimePolicy();

        await ValidateApproverAuthorization(entry, policy, approverId, approverRole, approverDeptId, ct);

        entry.Status = "Rejected";
        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);

        var approval = new TimeApproval
        {
            OrganizationId = entry.OrganizationId,
            TimeEntryId = timeEntryId,
            ApproverId = approverId,
            Action = "Rejected",
            Reason = reason
        };
        await _timeApprovalRepo.AddAsync(approval, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _outbox.PublishAsync(new
        {
            MessageType = "NotificationRequest",
            Action = "TimeEntryRejected",
            EntityType = "TimeEntry",
            EntityId = timeEntryId.ToString(),
            NotificationType = "TimeEntryRejected"
        }, ct);

        return ServiceResult<object>.Ok(MapToResponse(entry), "Time entry rejected.");
    }

    public async Task<ServiceResult<object>> GetProjectCostSummaryAsync(Guid projectId, DateTime? dateFrom,
        DateTime? dateTo, CancellationToken ct = default)
    {
        var entries = await _timeEntryRepo.GetApprovedBillableByProjectAsync(projectId, dateFrom, dateTo, ct);
        var entryList = entries.ToList();

        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;

        decimal totalCost = 0;
        decimal totalBillableHours = 0;

        var memberCosts = new Dictionary<Guid, MemberCostDetail>();
        var deptCosts = new Dictionary<Guid, DepartmentCostDetail>();

        foreach (var entry in entryList)
        {
            var memberRates = await _costRateRepo.GetActiveRatesForMemberAsync(orgId, entry.MemberId, entry.Date, ct);
            var story = await _storyRepo.GetByIdAsync(entry.StoryId, ct);
            var deptId = story?.DepartmentId ?? Guid.Empty;

            var roleDeptRates = deptId != Guid.Empty
                ? await _costRateRepo.GetActiveRatesForRoleDepartmentAsync(orgId, string.Empty, deptId, entry.Date, ct)
                : Enumerable.Empty<CostRate>();
            var orgDefault = await _costRateRepo.GetOrgDefaultAsync(orgId, entry.Date, ct);

            var rate = _costRateResolver.Resolve(entry.MemberId, string.Empty, deptId, entry.Date,
                memberRates, roleDeptRates, orgDefault);

            var hours = entry.DurationMinutes / 60.0m;
            var cost = hours * rate;

            totalCost += cost;
            totalBillableHours += hours;

            if (!memberCosts.ContainsKey(entry.MemberId))
                memberCosts[entry.MemberId] = new MemberCostDetail { MemberId = entry.MemberId };
            memberCosts[entry.MemberId].Hours += hours;
            memberCosts[entry.MemberId].Cost += cost;

            if (deptId != Guid.Empty)
            {
                if (!deptCosts.ContainsKey(deptId))
                    deptCosts[deptId] = new DepartmentCostDetail { DepartmentId = deptId };
                deptCosts[deptId].Hours += hours;
                deptCosts[deptId].Cost += cost;
            }
        }

        var allApprovedEntries = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, dateFrom, dateTo, false, "Approved", 1, int.MaxValue, ct);
        var totalNonBillableHours = allApprovedEntries.Items.Sum(e => e.DurationMinutes / 60.0m);

        return ServiceResult<object>.Ok(new ProjectCostSummaryResponse
        {
            TotalCost = totalCost,
            TotalBillableHours = totalBillableHours,
            TotalNonBillableHours = totalNonBillableHours,
            CostByMember = memberCosts.Values.ToList(),
            CostByDepartment = deptCosts.Values.ToList()
        }, "Project cost summary retrieved.");
    }

    public async Task<ServiceResult<object>> GetProjectUtilizationAsync(Guid projectId, DateTime? dateFrom,
        DateTime? dateTo, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;

        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct)
            ?? new TimePolicy();

        var (entries, _) = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, dateFrom, dateTo, null, null, 1, int.MaxValue, ct);
        var entryList = entries.ToList();

        var start = dateFrom ?? entryList.MinBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var end = dateTo ?? entryList.MaxBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var workingDays = CountWorkingDays(start, end);
        var expectedHours = policy.RequiredHoursPerDay * workingDays;

        var memberGroups = entryList.GroupBy(e => e.MemberId);
        var members = new List<MemberUtilizationDetail>();

        foreach (var group in memberGroups)
        {
            var totalMinutes = group.Sum(e => e.DurationMinutes);
            var totalLoggedHours = totalMinutes / 60.0m;
            var billableMinutes = group.Where(e => e.IsBillable).Sum(e => e.DurationMinutes);
            var nonBillableMinutes = group.Where(e => !e.IsBillable).Sum(e => e.DurationMinutes);
            var utilizationPercentage = expectedHours > 0
                ? Math.Round(totalLoggedHours / expectedHours * 100, 2)
                : 0;

            members.Add(new MemberUtilizationDetail
            {
                MemberId = group.Key,
                TotalLoggedHours = totalLoggedHours,
                ExpectedHours = expectedHours,
                UtilizationPercentage = utilizationPercentage,
                BillableHours = billableMinutes / 60.0m,
                NonBillableHours = nonBillableMinutes / 60.0m
            });
        }

        return ServiceResult<object>.Ok(new ResourceUtilizationResponse
        {
            Members = members
        }, "Resource utilization retrieved.");
    }

    public async Task<ServiceResult<object>> GetSprintVelocityAsync(Guid sprintId, CancellationToken ct = default)
    {
        var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprintId, ct);
        var storyIds = sprintStories
            .Where(ss => ss.RemovedDate == null)
            .Select(ss => ss.StoryId)
            .ToList();

        var totalStoryPoints = 0;
        var completedStoryCount = 0;

        foreach (var storyId in storyIds)
        {
            var story = await _storyRepo.GetByIdAsync(storyId, ct);
            if (story?.Status == "Done")
            {
                completedStoryCount++;
                totalStoryPoints += story.StoryPoints ?? 0;
            }
        }

        var approvedEntries = await _timeEntryRepo.GetApprovedBySprintAsync(sprintId, ct);
        var totalLoggedHours = approvedEntries.Sum(e => e.DurationMinutes) / 60.0m;

        decimal? averageHoursPerPoint = totalStoryPoints > 0
            ? Math.Round(totalLoggedHours / totalStoryPoints, 2)
            : null;

        return ServiceResult<object>.Ok(new SprintVelocityResponse
        {
            TotalStoryPoints = totalStoryPoints,
            TotalLoggedHours = totalLoggedHours,
            AverageHoursPerPoint = averageHoursPerPoint,
            CompletedStoryCount = completedStoryCount
        }, "Sprint velocity retrieved.");
    }

    private async System.Threading.Tasks.Task ValidateApproverAuthorization(
        TimeEntry entry, TimePolicy policy, Guid approverId, string approverRole,
        Guid approverDeptId, CancellationToken ct)
    {
        if (policy.ApprovalWorkflow == "None")
            return;

        if (approverRole == "OrgAdmin")
            return;

        if (policy.ApprovalWorkflow == "DeptLeadApproval")
        {
            if (approverRole != "DeptLead")
                throw new InsufficientPermissionsException();
        }
        else if (policy.ApprovalWorkflow == "ProjectLeadApproval")
        {
            var story = await _storyRepo.GetByIdAsync(entry.StoryId, ct);
            if (story != null)
            {
                var project = await _projectRepo.GetByIdAsync(story.ProjectId, ct);
                if (project?.LeadId != approverId)
                    throw new InsufficientPermissionsException();
            }
        }
    }

    private static int CountWorkingDays(DateTime start, DateTime end)
    {
        var days = 0;
        var current = start.Date;
        var endDate = end.Date;

        while (current <= endDate)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                days++;
            current = current.AddDays(1);
        }

        return days;
    }

    private static TimeEntryResponse MapToResponse(TimeEntry entry)
    {
        return new TimeEntryResponse
        {
            TimeEntryId = entry.TimeEntryId,
            OrganizationId = entry.OrganizationId,
            StoryId = entry.StoryId,
            MemberId = entry.MemberId,
            DurationMinutes = entry.DurationMinutes,
            Date = entry.Date,
            IsBillable = entry.IsBillable,
            IsOvertime = entry.IsOvertime,
            Status = entry.Status,
            Notes = entry.Notes,
            FlgStatus = entry.FlgStatus,
            DateCreated = entry.DateCreated,
            DateUpdated = entry.DateUpdated
        };
    }
}
