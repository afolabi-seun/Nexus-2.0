using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Analytics;
using WorkService.Application.DTOs.TimeEntries;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.CostRates;
using WorkService.Domain.Interfaces.Repositories.CostSnapshots;
using WorkService.Domain.Interfaces.Repositories.ProjectHealthSnapshots;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.ResourceAllocationSnapshots;
using WorkService.Domain.Interfaces.Repositories.RiskRegisters;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.StoryLinks;
using WorkService.Domain.Interfaces.Repositories.TimeEntries;
using WorkService.Domain.Interfaces.Repositories.TimePolicies;
using WorkService.Domain.Interfaces.Repositories.VelocitySnapshots;
using WorkService.Domain.Interfaces.Services.Analytics;
using WorkService.Domain.Interfaces.Services.CostRates;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Services.ServiceClients;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly IVelocitySnapshotRepository _velocitySnapshotRepo;
    private readonly IProjectHealthSnapshotRepository _healthSnapshotRepo;
    private readonly IResourceAllocationSnapshotRepository _resourceSnapshotRepo;
    private readonly IRiskRegisterRepository _riskRepo;
    private readonly ITimeEntryRepository _timeEntryRepo;
    private readonly ITimePolicyRepository _timePolicyRepo;
    private readonly ICostRateRepository _costRateRepo;
    private readonly ICostRateResolver _costRateResolver;
    private readonly IHealthScoreCalculator _healthScoreCalculator;
    private readonly IStoryRepository _storyRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly ISprintStoryRepository _sprintStoryRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IStoryLinkRepository _storyLinkRepo;
    private readonly ICostSnapshotRepository _costSnapshotRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly WorkDbContext _dbContext;
    private readonly IProfileServiceClient? _profileClient;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IVelocitySnapshotRepository velocitySnapshotRepo,
        IProjectHealthSnapshotRepository healthSnapshotRepo,
        IResourceAllocationSnapshotRepository resourceSnapshotRepo,
        IRiskRegisterRepository riskRepo,
        ITimeEntryRepository timeEntryRepo,
        ITimePolicyRepository timePolicyRepo,
        ICostRateRepository costRateRepo,
        ICostRateResolver costRateResolver,
        IHealthScoreCalculator healthScoreCalculator,
        IStoryRepository storyRepo,
        ISprintRepository sprintRepo,
        ISprintStoryRepository sprintStoryRepo,
        IProjectRepository projectRepo,
        IStoryLinkRepository storyLinkRepo,
        ICostSnapshotRepository costSnapshotRepo,
        IConnectionMultiplexer redis,
        WorkDbContext dbContext,
        ILogger<AnalyticsService> logger,
        IProfileServiceClient? profileClient = null)
    {
        _velocitySnapshotRepo = velocitySnapshotRepo;
        _healthSnapshotRepo = healthSnapshotRepo;
        _resourceSnapshotRepo = resourceSnapshotRepo;
        _riskRepo = riskRepo;
        _timeEntryRepo = timeEntryRepo;
        _timePolicyRepo = timePolicyRepo;
        _costRateRepo = costRateRepo;
        _costRateResolver = costRateResolver;
        _healthScoreCalculator = healthScoreCalculator;
        _storyRepo = storyRepo;
        _sprintRepo = sprintRepo;
        _sprintStoryRepo = sprintStoryRepo;
        _projectRepo = projectRepo;
        _storyLinkRepo = storyLinkRepo;
        _costSnapshotRepo = costSnapshotRepo;
        _redis = redis;
        _dbContext = dbContext;
        _logger = logger;
        _profileClient = profileClient;
    }

    // ── Velocity ──────────────────────────────────────────────────────────

    public async Task<object> GetVelocityTrendsAsync(Guid projectId, int sprintCount, CancellationToken ct = default)
    {
        if (sprintCount < 1 || sprintCount > 50)
            throw new InvalidAnalyticsParameterException("sprintCount must be between 1 and 50.");

        var snapshots = await _velocitySnapshotRepo.GetByProjectAsync(projectId, sprintCount, ct);

        return snapshots.Select(s => new VelocitySnapshotResponse
        {
            SprintId = s.SprintId,
            SprintName = s.SprintName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            CommittedPoints = s.CommittedPoints,
            CompletedPoints = s.CompletedPoints,
            TotalLoggedHours = s.TotalLoggedHours,
            AverageHoursPerPoint = s.AverageHoursPerPoint,
            CompletedStoryCount = s.CompletedStoryCount
        }).ToList();
    }

    public async System.Threading.Tasks.Task GenerateVelocitySnapshotAsync(Guid sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null) return;

        var project = await _projectRepo.GetByIdAsync(sprint.ProjectId, ct);
        if (project == null) return;

        var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprintId, ct);
        var storyIds = sprintStories.Where(ss => ss.RemovedDate == null).Select(ss => ss.StoryId).ToList();

        var committedPoints = 0;
        var completedPoints = 0;
        var completedStoryCount = 0;

        foreach (var storyId in storyIds)
        {
            var story = await _storyRepo.GetByIdAsync(storyId, ct);
            if (story == null) continue;

            committedPoints += story.StoryPoints ?? 0;
            if (story.Status is "Done" or "Closed")
            {
                completedPoints += story.StoryPoints ?? 0;
                completedStoryCount++;
            }
        }

        var approvedEntries = await _timeEntryRepo.GetApprovedBySprintAsync(sprintId, ct);
        var totalLoggedHours = approvedEntries.Sum(e => e.DurationMinutes) / 60.0m;

        decimal? averageHoursPerPoint = completedPoints > 0
            ? Math.Round(totalLoggedHours / completedPoints, 2)
            : null;

        var snapshot = new VelocitySnapshot
        {
            OrganizationId = project.OrganizationId,
            ProjectId = sprint.ProjectId,
            SprintId = sprintId,
            SprintName = sprint.SprintName,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            CommittedPoints = committedPoints,
            CompletedPoints = completedPoints,
            TotalLoggedHours = totalLoggedHours,
            AverageHoursPerPoint = averageHoursPerPoint,
            CompletedStoryCount = completedStoryCount,
            SnapshotDate = DateTime.UtcNow
        };

        await _velocitySnapshotRepo.AddOrUpdateAsync(snapshot, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    // ── Resource Management ──────────────────────────────────────────────

    public async Task<object> GetResourceManagementAsync(Guid orgId, DateTime? dateFrom, DateTime? dateTo,
        Guid? departmentId, CancellationToken ct = default)
    {
        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct) ?? new TimePolicy();

        var (entries, _) = await _timeEntryRepo.ListAsync(
            orgId, null, null, null, null, dateFrom, dateTo, null, null, 1, int.MaxValue, ct);
        var entryList = entries.ToList();

        var start = dateFrom ?? entryList.MinBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var end = dateTo ?? entryList.MaxBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var workingDays = CountWorkingDays(start, end);
        var expectedHours = policy.RequiredHoursPerDay * workingDays;

        var memberGroups = entryList.GroupBy(e => e.MemberId);
        var results = new List<ResourceManagementResponse>();

        foreach (var group in memberGroups)
        {
            var memberId = group.Key;
            var totalMinutes = group.Sum(e => e.DurationMinutes);
            var totalLoggedHours = totalMinutes / 60.0m;

            // Get member name from profile service
            var memberName = "Unknown";
            Guid memberDeptId = Guid.Empty;
            if (_profileClient != null)
            {
                try
                {
                    var member = await _profileClient.GetTeamMemberAsync(memberId, ct);
                    if (member != null)
                    {
                        memberName = member.DisplayName;
                        memberDeptId = member.DepartmentId ?? Guid.Empty;
                    }
                }
                catch { /* fallback to Unknown */ }
            }

            // Filter by department if specified
            if (departmentId.HasValue && memberDeptId != departmentId.Value)
                continue;

            // Build project breakdown
            var projectGroups = group.GroupBy(e => e.StoryId);
            var projectHours = new Dictionary<Guid, decimal>();

            foreach (var pg in projectGroups)
            {
                var story = await _storyRepo.GetByIdAsync(pg.Key, ct);
                if (story == null) continue;
                var projId = story.ProjectId;
                var hours = pg.Sum(e => e.DurationMinutes) / 60.0m;
                if (!projectHours.ContainsKey(projId))
                    projectHours[projId] = 0;
                projectHours[projId] += hours;
            }

            var breakdown = new List<ProjectBreakdownItem>();
            foreach (var kvp in projectHours.OrderByDescending(x => x.Value))
            {
                var project = await _projectRepo.GetByIdAsync(kvp.Key, ct);
                breakdown.Add(new ProjectBreakdownItem
                {
                    ProjectId = kvp.Key,
                    ProjectName = project?.ProjectName ?? "Unknown",
                    HoursLogged = kvp.Value,
                    Percentage = totalLoggedHours > 0 ? Math.Round(kvp.Value / totalLoggedHours * 100, 2) : 0
                });
            }

            var utilization = expectedHours > 0
                ? Math.Round(totalLoggedHours / expectedHours * 100, 2)
                : 0;

            results.Add(new ResourceManagementResponse
            {
                MemberId = memberId,
                MemberName = memberName,
                DepartmentId = memberDeptId,
                TotalLoggedHours = totalLoggedHours,
                ProjectBreakdown = breakdown,
                CapacityUtilizationPercentage = utilization
            });
        }

        return results;
    }

    // ── Resource Utilization ─────────────────────────────────────────────

    public async Task<object> GetResourceUtilizationAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct = default)
    {
        // Try to serve from snapshot first
        if (dateFrom.HasValue && dateTo.HasValue)
        {
            var snapshots = await _resourceSnapshotRepo.GetByProjectAsync(projectId, dateFrom.Value, dateTo.Value, ct);
            var snapshotList = snapshots.ToList();
            if (snapshotList.Count > 0)
            {
                return snapshotList.Select(s => new ResourceUtilizationDetailResponse
                {
                    MemberId = s.MemberId,
                    TotalLoggedHours = s.TotalLoggedHours,
                    ExpectedHours = s.ExpectedHours,
                    UtilizationPercentage = s.UtilizationPercentage,
                    BillableHours = s.BillableHours,
                    NonBillableHours = s.NonBillableHours,
                    OvertimeHours = s.OvertimeHours
                }).ToList();
            }
        }

        // Fall back to real-time computation
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;

        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct) ?? new TimePolicy();

        var (entries, _) = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, dateFrom, dateTo, null, null, 1, int.MaxValue, ct);
        var entryList = entries.ToList();

        var start = dateFrom ?? entryList.MinBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var end = dateTo ?? entryList.MaxBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var workingDays = CountWorkingDays(start, end);
        var expectedHours = policy.RequiredHoursPerDay * workingDays;

        var memberGroups = entryList.GroupBy(e => e.MemberId);
        var results = new List<ResourceUtilizationDetailResponse>();

        foreach (var group in memberGroups)
        {
            var totalMinutes = group.Sum(e => e.DurationMinutes);
            var totalLoggedHours = totalMinutes / 60.0m;
            var billableHours = group.Where(e => e.IsBillable).Sum(e => e.DurationMinutes) / 60.0m;
            var nonBillableHours = group.Where(e => !e.IsBillable).Sum(e => e.DurationMinutes) / 60.0m;
            var overtimeHours = group.Where(e => e.IsOvertime).Sum(e => e.DurationMinutes) / 60.0m;
            var utilization = expectedHours > 0
                ? Math.Round(totalLoggedHours / expectedHours * 100, 2)
                : 0;

            results.Add(new ResourceUtilizationDetailResponse
            {
                MemberId = group.Key,
                TotalLoggedHours = totalLoggedHours,
                ExpectedHours = expectedHours,
                UtilizationPercentage = utilization,
                BillableHours = billableHours,
                NonBillableHours = nonBillableHours,
                OvertimeHours = overtimeHours
            });
        }

        return results;
    }

    public async System.Threading.Tasks.Task GenerateResourceAllocationSnapshotAsync(
        Guid projectId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null) return;

        var orgId = project.OrganizationId;
        var policy = await _timePolicyRepo.GetByOrganizationAsync(orgId, ct) ?? new TimePolicy();

        var (entries, _) = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, periodStart, periodEnd, null, null, 1, int.MaxValue, ct);
        var entryList = entries.ToList();

        var workingDays = CountWorkingDays(periodStart, periodEnd);
        var expectedHours = policy.RequiredHoursPerDay * workingDays;

        var memberGroups = entryList.GroupBy(e => e.MemberId);

        foreach (var group in memberGroups)
        {
            var totalMinutes = group.Sum(e => e.DurationMinutes);
            var totalLoggedHours = totalMinutes / 60.0m;
            var billableHours = group.Where(e => e.IsBillable).Sum(e => e.DurationMinutes) / 60.0m;
            var nonBillableHours = group.Where(e => !e.IsBillable).Sum(e => e.DurationMinutes) / 60.0m;
            var overtimeHours = group.Where(e => e.IsOvertime).Sum(e => e.DurationMinutes) / 60.0m;
            var utilization = expectedHours > 0
                ? Math.Round(totalLoggedHours / expectedHours * 100, 2)
                : 0;

            var snapshot = new ResourceAllocationSnapshot
            {
                OrganizationId = orgId,
                ProjectId = projectId,
                MemberId = group.Key,
                TotalLoggedHours = totalLoggedHours,
                ExpectedHours = expectedHours,
                UtilizationPercentage = utilization,
                BillableHours = billableHours,
                NonBillableHours = nonBillableHours,
                OvertimeHours = overtimeHours,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                SnapshotDate = DateTime.UtcNow
            };

            await _resourceSnapshotRepo.AddOrUpdateAsync(snapshot, ct);
        }
        await _dbContext.SaveChangesAsync(ct);
    }

    // ── Project Cost ─────────────────────────────────────────────────────

    public async Task<object> GetProjectCostAnalyticsAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;

        var billableEntries = await _timeEntryRepo.GetApprovedBillableByProjectAsync(projectId, dateFrom, dateTo, ct);
        var billableList = billableEntries.ToList();

        decimal totalCost = 0;
        decimal totalBillableHours = 0;
        var memberCosts = new Dictionary<Guid, MemberCostDetail>();
        var deptCosts = new Dictionary<Guid, DepartmentCostDetail>();

        foreach (var entry in billableList)
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

        // Non-billable hours
        var (allApproved, _) = await _timeEntryRepo.ListAsync(
            orgId, null, projectId, null, null, dateFrom, dateTo, false, "Approved", 1, int.MaxValue, ct);
        var totalNonBillableHours = allApproved.Sum(e => e.DurationMinutes / 60.0m);

        // Burn rate
        var start = dateFrom ?? billableList.MinBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var end = dateTo ?? billableList.MaxBy(e => e.Date)?.Date ?? DateTime.UtcNow;
        var workingDays = CountWorkingDays(start, end);
        var burnRatePerDay = workingDays > 0 ? Math.Round(totalCost / workingDays, 2) : 0;

        // Cost trend from CostSnapshot history
        var (costSnapshots, _) = await _costSnapshotRepo.ListByProjectAsync(projectId, dateFrom, dateTo, 1, 50, ct);
        var costTrend = costSnapshots.Select(cs => new CostTrendItem
        {
            SnapshotDate = cs.SnapshotDate,
            TotalCost = cs.TotalCost
        }).ToList();

        return new ProjectCostAnalyticsResponse
        {
            TotalCost = totalCost,
            TotalBillableHours = totalBillableHours,
            TotalNonBillableHours = totalNonBillableHours,
            BurnRatePerDay = burnRatePerDay,
            CostByMember = memberCosts.Values.ToList(),
            CostByDepartment = deptCosts.Values.ToList(),
            CostTrend = costTrend
        };
    }

    // ── Project Health ───────────────────────────────────────────────────

    public async Task<object> GetProjectHealthAsync(Guid projectId, bool includeHistory, CancellationToken ct = default)
    {
        var latest = await _healthSnapshotRepo.GetLatestByProjectAsync(projectId, ct);

        if (latest == null)
        {
            return new ProjectHealthResponse
            {
                OverallScore = 50,
                VelocityScore = 50,
                BugRateScore = 50,
                OverdueScore = 50,
                RiskScore = 100,
                Trend = "stable",
                SnapshotDate = DateTime.UtcNow
            };
        }

        var response = MapHealthSnapshotToResponse(latest);

        if (includeHistory)
        {
            var history = await _healthSnapshotRepo.GetHistoryByProjectAsync(projectId, 10, ct);
            response.History = history.Select(MapHealthSnapshotToResponse).ToList();
        }

        return response;
    }

    public async System.Threading.Tasks.Task GenerateHealthSnapshotAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null) return;

        var orgId = project.OrganizationId;

        // Fetch data for health calculation
        var recentVelocity = await _velocitySnapshotRepo.GetByProjectAsync(projectId, 3, ct);

        // Count open bugs (stories with Priority = "Bug" or type = "Bug" that are not Done/Closed)
        var (allStories, _) = await _storyRepo.ListAsync(orgId, 1, int.MaxValue, projectId, null, null, null, null, null, null, null, null, null, ct);
        var storyList = allStories.ToList();
        var activeStories = storyList.Where(s => s.Status is not ("Done" or "Closed")).ToList();
        var openBugCount = activeStories.Count(s => s.StoryType == "Bug");
        var totalActiveStories = activeStories.Count;

        // Overdue stories
        var now = DateTime.UtcNow;
        var storiesWithDueDate = storyList.Where(s => s.DueDate.HasValue && s.Status is not ("Done" or "Closed")).ToList();
        var overdueStoryCount = storiesWithDueDate.Count(s => s.DueDate!.Value < now);
        var totalStoriesWithDueDate = storiesWithDueDate.Count;

        // Active risks
        var activeRisks = await _riskRepo.GetActiveByProjectAsync(projectId, ct);

        var result = (HealthScoreResult)_healthScoreCalculator.Calculate(
            recentVelocity, openBugCount, totalActiveStories,
            overdueStoryCount, totalStoriesWithDueDate, activeRisks);

        // Determine trend
        var previousSnapshot = await _healthSnapshotRepo.GetLatestByProjectAsync(projectId, ct);
        var trend = _healthScoreCalculator.DetermineTrend(result.OverallScore, previousSnapshot?.OverallScore);

        var snapshot = new ProjectHealthSnapshot
        {
            OrganizationId = orgId,
            ProjectId = projectId,
            OverallScore = result.OverallScore,
            VelocityScore = result.VelocityScore,
            BugRateScore = result.BugRateScore,
            OverdueScore = result.OverdueScore,
            RiskScore = result.RiskScore,
            Trend = trend,
            SnapshotDate = DateTime.UtcNow
        };

        await _healthSnapshotRepo.AddAsync(snapshot, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    // ── Bug Metrics ──────────────────────────────────────────────────────

    public async Task<object> GetBugMetricsAsync(Guid projectId, Guid? sprintId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;

        IEnumerable<Story> stories;

        if (sprintId.HasValue)
        {
            // Scope to sprint via SprintStory records
            var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprintId.Value, ct);
            var storyIds = sprintStories.Where(ss => ss.RemovedDate == null).Select(ss => ss.StoryId).ToList();
            var storyList = new List<Story>();
            foreach (var storyId in storyIds)
            {
                var story = await _storyRepo.GetByIdAsync(storyId, ct);
                if (story != null) storyList.Add(story);
            }
            stories = storyList;
        }
        else
        {
            var (allStories, _) = await _storyRepo.ListAsync(orgId, 1, int.MaxValue, projectId, null, null, null, null, null, null, null, null, null, ct);
            stories = allStories;
        }

        var storyArray = stories.ToList();
        var bugs = storyArray.Where(s => s.StoryType == "Bug").ToList();

        var totalBugs = bugs.Count;
        var openBugs = bugs.Count(b => b.Status is not ("Done" or "Closed"));
        var closedBugs = bugs.Count(b => b.Status is "Done" or "Closed");
        var reopenedBugs = 0; // Would need activity log tracking for accurate count

        var totalActiveStories = storyArray.Count(s => s.Status is not ("Done" or "Closed"));
        var bugRate = totalActiveStories > 0
            ? Math.Round((decimal)openBugs / totalActiveStories * 100, 2)
            : 0;

        var bugsBySeverity = bugs
            .GroupBy(b => b.Priority)
            .ToDictionary(g => g.Key, g => g.Count());

        // Bug trend: last 10 completed sprints
        var completedSprints = await _sprintRepo.GetCompletedAsync(orgId, 10, ct);
        var bugTrend = new List<BugTrendItem>();

        foreach (var sprint in completedSprints)
        {
            var ss = await _sprintStoryRepo.ListBySprintAsync(sprint.SprintId, ct);
            var sprintStoryIds = ss.Where(s => s.RemovedDate == null).Select(s => s.StoryId).ToList();
            var sprintBugCount = 0;

            foreach (var sid in sprintStoryIds)
            {
                var story = await _storyRepo.GetByIdAsync(sid, ct);
                if (story?.StoryType == "Bug") sprintBugCount++;
            }

            bugTrend.Add(new BugTrendItem
            {
                SprintId = sprint.SprintId,
                SprintName = sprint.SprintName,
                BugCount = sprintBugCount
            });
        }

        return new BugMetricsResponse
        {
            TotalBugs = totalBugs,
            OpenBugs = openBugs,
            ClosedBugs = closedBugs,
            ReopenedBugs = reopenedBugs,
            BugRate = bugRate,
            BugsBySeverity = bugsBySeverity,
            BugTrend = bugTrend
        };
    }

    // ── Dashboard ────────────────────────────────────────────────────────

    public async Task<object> GetDashboardAsync(Guid projectId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.AnalyticsDashboard(projectId);

        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedDashboard = JsonSerializer.Deserialize<DashboardSummaryResponse>(cached!);
            if (cachedDashboard != null) return cachedDashboard;
        }

        // Health
        var healthSnapshot = await _healthSnapshotRepo.GetLatestByProjectAsync(projectId, ct);
        ProjectHealthResponse? healthResponse = healthSnapshot != null
            ? MapHealthSnapshotToResponse(healthSnapshot)
            : null;

        // Velocity
        var velocitySnapshots = await _velocitySnapshotRepo.GetByProjectAsync(projectId, 1, ct);
        var latestVelocity = velocitySnapshots.FirstOrDefault();
        VelocitySnapshotResponse? velocityResponse = latestVelocity != null
            ? new VelocitySnapshotResponse
            {
                SprintId = latestVelocity.SprintId,
                SprintName = latestVelocity.SprintName,
                StartDate = latestVelocity.StartDate,
                EndDate = latestVelocity.EndDate,
                CommittedPoints = latestVelocity.CommittedPoints,
                CompletedPoints = latestVelocity.CompletedPoints,
                TotalLoggedHours = latestVelocity.TotalLoggedHours,
                AverageHoursPerPoint = latestVelocity.AverageHoursPerPoint,
                CompletedStoryCount = latestVelocity.CompletedStoryCount
            }
            : null;

        // Active bugs
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        var orgId = project?.OrganizationId ?? Guid.Empty;
        var (allStories, _) = await _storyRepo.ListAsync(orgId, 1, int.MaxValue, projectId, null, null, null, null, null, null, null, null, null, ct);
        var storyList = allStories.ToList();
        var activeBugCount = storyList.Count(s => s.StoryType == "Bug" && s.Status is not ("Done" or "Closed"));

        // Active risks
        var activeRiskCount = await _riskRepo.CountActiveByProjectAsync(projectId, ct);

        // Blocked stories
        var allLinks = new List<StoryLink>();
        foreach (var story in storyList)
        {
            var links = await _storyLinkRepo.ListByStoryAsync(story.StoryId, ct);
            allLinks.AddRange(links);
        }
        var blockedCount = 0;
        var incomingBlocks = new Dictionary<Guid, List<Guid>>();
        foreach (var link in allLinks)
        {
            if (link.LinkType == "is_blocked_by")
            {
                if (!incomingBlocks.ContainsKey(link.SourceStoryId))
                    incomingBlocks[link.SourceStoryId] = new List<Guid>();
                incomingBlocks[link.SourceStoryId].Add(link.TargetStoryId);
            }
            else if (link.LinkType == "blocks")
            {
                if (!incomingBlocks.ContainsKey(link.TargetStoryId))
                    incomingBlocks[link.TargetStoryId] = new List<Guid>();
                incomingBlocks[link.TargetStoryId].Add(link.SourceStoryId);
            }
        }
        foreach (var kvp in incomingBlocks)
        {
            var story = storyList.FirstOrDefault(s => s.StoryId == kvp.Key);
            if (story == null || story.Status is "Done" or "Closed") continue;
            var hasActiveBlocker = kvp.Value.Any(blockerId =>
            {
                var blocker = storyList.FirstOrDefault(s => s.StoryId == blockerId);
                return blocker != null && blocker.Status is not ("Done" or "Closed");
            });
            if (hasActiveBlocker) blockedCount++;
        }

        // Cost
        decimal totalProjectCost = 0;
        decimal burnRatePerDay = 0;
        try
        {
            var costResult = (ProjectCostAnalyticsResponse)await GetProjectCostAnalyticsAsync(projectId, null, null, ct);
            totalProjectCost = costResult.TotalCost;
            burnRatePerDay = costResult.BurnRatePerDay;
        }
        catch { /* cost data may not be available */ }

        var dashboard = new DashboardSummaryResponse
        {
            ProjectHealth = healthResponse,
            VelocitySnapshot = velocityResponse,
            ActiveBugCount = activeBugCount,
            ActiveRiskCount = activeRiskCount,
            BlockedStoryCount = blockedCount,
            TotalProjectCost = totalProjectCost,
            BurnRatePerDay = burnRatePerDay
        };

        // Cache for 5 minutes
        var json = JsonSerializer.Serialize(dashboard);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(3));

        return dashboard;
    }

    // ── Snapshot Status ──────────────────────────────────────────────────

    public async Task<object> GetSnapshotStatusAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(RedisKeys.AnalyticsSnapshotStatus);

        if (cached.HasValue)
        {
            var status = JsonSerializer.Deserialize<SnapshotStatusResponse>(cached!);
            if (status != null) return status;
        }

        return new SnapshotStatusResponse
        {
            LastRunTime = null,
            ProjectsProcessed = 0,
            ErrorsEncountered = 0,
            NextScheduledRun = null
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ProjectHealthResponse MapHealthSnapshotToResponse(ProjectHealthSnapshot snapshot)
    {
        return new ProjectHealthResponse
        {
            OverallScore = snapshot.OverallScore,
            VelocityScore = snapshot.VelocityScore,
            BugRateScore = snapshot.BugRateScore,
            OverdueScore = snapshot.OverdueScore,
            RiskScore = snapshot.RiskScore,
            Trend = snapshot.Trend,
            SnapshotDate = snapshot.SnapshotDate
        };
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
}
