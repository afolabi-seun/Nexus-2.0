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
        _logger = logger;
    }

    public async Task<object> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default)
    {
        var req = (CreateTimeEntryRequest)request;

        // Validate story exists
        var story = await _storyRepo.GetByIdAsync(req.StoryId, ct)
            ?? throw new StoryNotFoundException(req.StoryId);

        // Get time policy for org (use defaults if none configured)
        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct)
            ?? new TimePolicy();

        // Check daily hours limit
        var dailyTotalMinutes = await _timeEntryRepo.GetDailyTotalMinutesAsync(userId, req.Date, ct);
        var newTotalMinutes = dailyTotalMinutes + req.DurationMinutes;
        var maxDailyMinutes = (int)(policy.MaxDailyHours * 60);

        if (newTotalMinutes > maxDailyMinutes)
            throw new DailyHoursExceededException(policy.MaxDailyHours);

        // Flag overtime
        var overtimeThresholdMinutes = (int)(policy.OvertimeThresholdHoursPerDay * 60);
        var isOvertime = newTotalMinutes > overtimeThresholdMinutes;

        // Set status based on approval policy
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

        // Publish activity log to outbox
        await _outbox.PublishAsync(new
        {
            MessageType = "AuditEvent",
            Action = "TimeEntryCreated",
            EntityType = "TimeEntry",
            EntityId = entry.TimeEntryId.ToString(),
            OrganizationId = orgId,
            UserId = userId
        }, ct);

        return MapToResponse(entry);
    }

    public async Task<object> UpdateAsync(Guid timeEntryId, Guid userId, object request, CancellationToken ct = default)
    {
        var req = (UpdateTimeEntryRequest)request;

        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct)
            ?? throw new TimeEntryNotFoundException(timeEntryId);

        // Check ownership
        if (entry.MemberId != userId)
            throw new InsufficientPermissionsException();

        // Apply partial updates
        if (req.DurationMinutes.HasValue) entry.DurationMinutes = req.DurationMinutes.Value;
        if (req.Date.HasValue) entry.Date = req.Date.Value;
        if (req.IsBillable.HasValue) entry.IsBillable = req.IsBillable.Value;
        if (req.Notes != null) entry.Notes = req.Notes;

        // If entry was Approved, reset to Pending
        if (entry.Status == "Approved")
            entry.Status = "Pending";

        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);

        return MapToResponse(entry);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid timeEntryId, Guid userId, CancellationToken ct = default)
    {
        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct)
            ?? throw new TimeEntryNotFoundException(timeEntryId);

        // Check ownership
        if (entry.MemberId != userId)
            throw new InsufficientPermissionsException();

        entry.FlgStatus = "D";
        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);
    }

    public async Task<object> ListAsync(Guid orgId, Guid? storyId, Guid? projectId, Guid? sprintId,
        Guid? memberId, DateTime? dateFrom, DateTime? dateTo, bool? isBillable,
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _timeEntryRepo.ListAsync(
            orgId, storyId, projectId, sprintId, memberId,
            dateFrom, dateTo, isBillable, status, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();

        return new PaginatedResponse<TimeEntryResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<object> ApproveAsync(Guid timeEntryId, Guid approverId, string approverRole,
        Guid approverDeptId, CancellationToken ct = default)
    {
        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct)
            ?? throw new TimeEntryNotFoundException(timeEntryId);

        var policy = await _timePolicyRepo.GetByOrganizationAsync(entry.OrganizationId, ct)
            ?? new TimePolicy();

        // Check approver authorization based on workflow
        await ValidateApproverAuthorization(entry, policy, approverId, approverRole, approverDeptId, ct);

        entry.Status = "Approved";
        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);

        // Create TimeApproval record
        var approval = new TimeApproval
        {
            OrganizationId = entry.OrganizationId,
            TimeEntryId = timeEntryId,
            ApproverId = approverId,
            Action = "Approved"
        };
        await _timeApprovalRepo.AddAsync(approval, ct);

        // Publish notification to outbox
        await _outbox.PublishAsync(new
        {
            MessageType = "NotificationRequest",
            Action = "TimeEntryApproved",
            EntityType = "TimeEntry",
            EntityId = timeEntryId.ToString(),
            NotificationType = "TimeEntryApproved"
        }, ct);

        return MapToResponse(entry);
    }

    public async Task<object> RejectAsync(Guid timeEntryId, Guid approverId, string approverRole,
        Guid approverDeptId, string reason, CancellationToken ct = default)
    {
        var entry = await _timeEntryRepo.GetByIdAsync(timeEntryId, ct)
            ?? throw new TimeEntryNotFoundException(timeEntryId);

        var policy = await _timePolicyRepo.GetByOrganizationAsync(entry.OrganizationId, ct)
            ?? new TimePolicy();

        // Check approver authorization based on workflow
        await ValidateApproverAuthorization(entry, policy, approverId, approverRole, approverDeptId, ct);

        entry.Status = "Rejected";
        entry.DateUpdated = DateTime.UtcNow;
        await _timeEntryRepo.UpdateAsync(entry, ct);

        // Create TimeApproval record with reason
        var approval = new TimeApproval
        {
            OrganizationId = entry.OrganizationId,
            TimeEntryId = timeEntryId,
            ApproverId = approverId,
            Action = "Rejected",
            Reason = reason
        };
        await _timeApprovalRepo.AddAsync(approval, ct);

        // Publish notification to outbox
        await _outbox.PublishAsync(new
        {
            MessageType = "NotificationRequest",
            Action = "TimeEntryRejected",
            EntityType = "TimeEntry",
            EntityId = timeEntryId.ToString(),
            NotificationType = "TimeEntryRejected"
        }, ct);

        return MapToResponse(entry);
    }

    public async Task<object> GetProjectCostSummaryAsync(Guid projectId, DateTime? dateFrom,
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
            // Resolve applicable rate
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

            // Group by member
            if (!memberCosts.ContainsKey(entry.MemberId))
                memberCosts[entry.MemberId] = new MemberCostDetail { MemberId = entry.MemberId };
            memberCosts[entry.MemberId].Hours += hours;
            memberCosts[entry.MemberId].Cost += cost;

            // Group by department
            if (deptId != Guid.Empty)
            {
                if (!deptCosts.ContainsKey(deptId))
                    deptCosts[deptId] = new DepartmentCostDetail { DepartmentId = deptId };
                deptCosts[deptId].Hours += hours;
                deptCosts[deptId].Cost += cost;
            }
        }

        // Calculate non-billable hours (all approved entries minus billable)
        // GetApprovedBillableByProjectAsync only returns billable, so we need all approved for non-billable
        var allApprovedEntries = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, dateFrom, dateTo, false, "Approved", 1, int.MaxValue, ct);
        var totalNonBillableHours = allApprovedEntries.Items.Sum(e => e.DurationMinutes / 60.0m);

        return new ProjectCostSummaryResponse
        {
            TotalCost = totalCost,
            TotalBillableHours = totalBillableHours,
            TotalNonBillableHours = totalNonBillableHours,
            CostByMember = memberCosts.Values.ToList(),
            CostByDepartment = deptCosts.Values.ToList()
        };
    }

    public async Task<object> GetProjectUtilizationAsync(Guid projectId, DateTime? dateFrom,
        DateTime? dateTo, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;

        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct)
            ?? new TimePolicy();

        // Get all time entries for project in date range
        var (entries, _) = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, dateFrom, dateTo, null, null, 1, int.MaxValue, ct);
        var entryList = entries.ToList();

        // Calculate working days in range
        var start = dateFrom ?? entryList.MinBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var end = dateTo ?? entryList.MaxBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var workingDays = CountWorkingDays(start, end);
        var expectedHours = policy.RequiredHoursPerDay * workingDays;

        // Group by member
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

        return new ResourceUtilizationResponse
        {
            Members = members
        };
    }

    public async Task<object> GetSprintVelocityAsync(Guid sprintId, CancellationToken ct = default)
    {
        // Get stories in sprint via SprintStory
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

        // Get approved time entries for sprint
        var approvedEntries = await _timeEntryRepo.GetApprovedBySprintAsync(sprintId, ct);
        var totalLoggedHours = approvedEntries.Sum(e => e.DurationMinutes) / 60.0m;

        decimal? averageHoursPerPoint = totalStoryPoints > 0
            ? Math.Round(totalLoggedHours / totalStoryPoints, 2)
            : null;

        return new SprintVelocityResponse
        {
            TotalStoryPoints = totalStoryPoints,
            TotalLoggedHours = totalLoggedHours,
            AverageHoursPerPoint = averageHoursPerPoint,
            CompletedStoryCount = completedStoryCount
        };
    }

    private async System.Threading.Tasks.Task ValidateApproverAuthorization(
        TimeEntry entry, TimePolicy policy, Guid approverId, string approverRole,
        Guid approverDeptId, CancellationToken ct)
    {
        if (policy.ApprovalWorkflow == "None")
            return;

        // OrgAdmin can always approve
        if (approverRole == "OrgAdmin")
            return;

        if (policy.ApprovalWorkflow == "DeptLeadApproval")
        {
            // Approver must be DeptLead in the time entry owner's department
            if (approverRole != "DeptLead")
                throw new InsufficientPermissionsException();
        }
        else if (policy.ApprovalWorkflow == "ProjectLeadApproval")
        {
            // Approver must be the project lead of the story's project
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
