using WorkService.Application.DTOs.Reports;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Reports;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Services.ServiceClients;

namespace WorkService.Infrastructure.Services.Reports;

public class ReportService : IReportService
{
    private readonly ISprintRepository _sprintRepo;
    private readonly IStoryRepository _storyRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly ISprintStoryRepository _sprintStoryRepo;
    private readonly IProfileServiceClient? _profileClient;

    public ReportService(
        ISprintRepository sprintRepo, IStoryRepository storyRepo,
        ITaskRepository taskRepo, ISprintStoryRepository sprintStoryRepo,
        IProfileServiceClient? profileClient = null)
    {
        _sprintRepo = sprintRepo; _storyRepo = storyRepo;
        _taskRepo = taskRepo; _sprintStoryRepo = sprintStoryRepo;
        _profileClient = profileClient;
    }

    public async Task<ServiceResult<object>> GetVelocityChartAsync(Guid organizationId, int count, CancellationToken ct = default)
    {
        var sprints = await _sprintRepo.GetCompletedAsync(organizationId, count, ct);
        var results = new List<VelocityChartResponse>();

        foreach (var sprint in sprints)
        {
            var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprint.SprintId, ct);
            var totalPoints = 0;
            var completedStories = 0;
            var totalStories = 0;

            foreach (var ss in sprintStories)
            {
                var story = await _storyRepo.GetByIdAsync(ss.StoryId, ct);
                if (story == null) continue;
                totalStories++;
                totalPoints += story.StoryPoints ?? 0;
                if (story.Status is "Done" or "Closed") completedStories++;
            }

            results.Add(new VelocityChartResponse
            {
                SprintName = sprint.SprintName, Velocity = sprint.Velocity ?? 0,
                TotalStoryPoints = totalPoints,
                CompletionRate = totalStories > 0 ? Math.Round((decimal)completedStories / totalStories * 100, 2) : 0,
                StartDate = sprint.StartDate, EndDate = sprint.EndDate
            });
        }

        return ServiceResult<object>.Ok(results);
    }

    public async Task<ServiceResult<object>> GetDepartmentWorkloadAsync(Guid organizationId, Guid? sprintId, CancellationToken ct = default)
    {
        var tasks = await _taskRepo.ListByDepartmentAsync(organizationId, sprintId, ct);
        var taskList = tasks.ToList();

        var data = taskList.GroupBy(t => t.DepartmentId?.ToString() ?? "Unassigned")
            .Select(g => new DepartmentWorkloadResponse
            {
                DepartmentName = g.Key, TotalTasks = g.Count(),
                CompletedTasks = g.Count(t => t.Status == "Done"),
                InProgressTasks = g.Count(t => t.Status == "InProgress"),
                MemberCount = g.Select(t => t.AssigneeId).Where(a => a.HasValue).Distinct().Count(),
                AvgTasksPerMember = g.Select(t => t.AssigneeId).Where(a => a.HasValue).Distinct().Count() > 0
                    ? Math.Round((decimal)g.Count() / g.Select(t => t.AssigneeId).Where(a => a.HasValue).Distinct().Count(), 2)
                    : 0
            }).ToList();

        return ServiceResult<object>.Ok(data);
    }

    public async Task<ServiceResult<object>> GetCapacityUtilizationAsync(Guid organizationId, Guid? departmentId, CancellationToken ct = default)
    {
        var results = new List<CapacityUtilizationResponse>();
        // Capacity utilization requires ProfileService for member data
        // Return empty list if ProfileService is not available
        await System.Threading.Tasks.Task.CompletedTask;
        return ServiceResult<object>.Ok(results);
    }

    public async Task<ServiceResult<object>> GetCycleTimeAsync(Guid organizationId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var (stories, _) = await _storyRepo.ListAsync(organizationId, 1, 10000, null,
            "Done", null, null, null, null, null, null, dateFrom, dateTo, ct);

        var data = stories.Where(s => s.CompletedDate.HasValue).Select(s =>
        {
            var cycleTimeDays = s.CompletedDate.HasValue
                ? (int)(s.CompletedDate.Value - s.DateCreated).TotalDays
                : 0;
            var leadTimeDays = cycleTimeDays; // Simplified: same as cycle time for now

            return new CycleTimeResponse
            {
                StoryKey = s.StoryKey, Title = s.Title,
                CycleTimeDays = cycleTimeDays, LeadTimeDays = leadTimeDays,
                CompletedDate = s.CompletedDate!.Value
            };
        }).ToList();

        return ServiceResult<object>.Ok(data);
    }

    public async Task<ServiceResult<object>> GetTaskCompletionAsync(Guid organizationId, Guid? sprintId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var tasks = await _taskRepo.ListByDepartmentAsync(organizationId, sprintId, ct);
        var taskList = tasks.ToList();

        if (dateFrom.HasValue) taskList = taskList.Where(t => t.DateCreated >= dateFrom.Value).ToList();
        if (dateTo.HasValue) taskList = taskList.Where(t => t.DateCreated <= dateTo.Value).ToList();

        var data = taskList.GroupBy(t => t.DepartmentId?.ToString() ?? "Unassigned")
            .Select(g =>
            {
                var completed = g.Where(t => t.Status == "Done").ToList();
                var avgHours = completed.Count > 0
                    ? Math.Round(completed.Where(t => t.CompletedDate.HasValue)
                        .Average(t => (t.CompletedDate!.Value - t.DateCreated).TotalHours), 2)
                    : 0;

                return new TaskCompletionResponse
                {
                    DepartmentName = g.Key, TotalTasks = g.Count(),
                    CompletedTasks = completed.Count,
                    CompletionRate = g.Count() > 0 ? Math.Round((decimal)completed.Count / g.Count() * 100, 2) : 0,
                    AvgCompletionTimeHours = (decimal)avgHours
                };
            }).ToList();

        return ServiceResult<object>.Ok(data);
    }
}
