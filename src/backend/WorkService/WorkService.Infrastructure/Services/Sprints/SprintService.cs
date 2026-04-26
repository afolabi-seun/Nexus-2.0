using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Sprints;
using WorkService.Application.DTOs.Stories;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Analytics;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Domain.Interfaces.Services.Sprints;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Sprints;

public class SprintService : ISprintService
{
    private readonly ISprintRepository _sprintRepo;
    private readonly ISprintStoryRepository _sprintStoryRepo;
    private readonly IStoryRepository _storyRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IOutboxService _outbox;
    private readonly IConnectionMultiplexer _redis;
    private readonly WorkDbContext _dbContext;
    private readonly IProfileServiceClient? _profileClient;
    private readonly IAnalyticsSnapshotService? _analyticsSnapshotService;
    private readonly ILogger<SprintService> _logger;

    public SprintService(
        ISprintRepository sprintRepo, ISprintStoryRepository sprintStoryRepo,
        IStoryRepository storyRepo, ITaskRepository taskRepo, IProjectRepository projectRepo,
        IOutboxService outbox, IConnectionMultiplexer redis, WorkDbContext dbContext,
        ILogger<SprintService> logger, IProfileServiceClient? profileClient = null,
        IAnalyticsSnapshotService? analyticsSnapshotService = null)
    {
        _sprintRepo = sprintRepo; _sprintStoryRepo = sprintStoryRepo;
        _storyRepo = storyRepo; _taskRepo = taskRepo; _projectRepo = projectRepo;
        _outbox = outbox; _redis = redis; _dbContext = dbContext; _logger = logger; _profileClient = profileClient;
        _analyticsSnapshotService = analyticsSnapshotService;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid projectId, object request, CancellationToken ct = default)
    {
        var req = (CreateSprintRequest)request;
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound,
                $"Project with ID '{projectId}' was not found.", 404);

        if (req.EndDate <= req.StartDate)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintEndBeforeStartValue, ErrorCodes.SprintEndBeforeStart,
                "Sprint end date must be after start date.", 400);

        var sprint = new Sprint
        {
            OrganizationId = organizationId, ProjectId = projectId,
            SprintName = req.SprintName, Goal = req.Goal,
            StartDate = req.StartDate, EndDate = req.EndDate, Status = "Planning"
        };

        await _sprintRepo.AddAsync(sprint, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Created(await BuildDetailResponse(sprint, ct), "Sprint created successfully.");
    }

    public async Task<ServiceResult<object>> GetByIdAsync(Guid sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);
        return ServiceResult<object>.Ok(await BuildDetailResponse(sprint, ct));
    }

    public async Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, string? status, Guid? projectId, CancellationToken ct = default)
    {
        var (items, totalCount) = await _sprintRepo.ListAsync(organizationId, page, pageSize, status, projectId, ct);
        var responses = items.Select(s => new SprintListResponse
        {
            SprintId = s.SprintId, SprintName = s.SprintName,
            Status = s.Status, StartDate = s.StartDate, EndDate = s.EndDate,
            Velocity = s.Velocity
        }).ToList();

        return ServiceResult<object>.Ok(new PaginatedResponse<SprintListResponse>
        {
            Data = responses, TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }, "Sprints retrieved.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid sprintId, object request, CancellationToken ct = default)
    {
        var req = (UpdateSprintRequest)request;
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);
        if (sprint.Status != "Planning")
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotInPlanningValue, ErrorCodes.SprintNotInPlanning,
                $"Sprint '{sprintId}' is not in Planning status.", 400);

        if (req.SprintName != null) sprint.SprintName = req.SprintName;
        if (req.Goal != null) sprint.Goal = req.Goal;
        if (req.StartDate.HasValue) sprint.StartDate = req.StartDate.Value;
        if (req.EndDate.HasValue) sprint.EndDate = req.EndDate.Value;
        sprint.DateUpdated = DateTime.UtcNow;

        await _sprintRepo.UpdateAsync(sprint, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Ok(await BuildDetailResponse(sprint, ct), "Sprint updated.");
    }

    public async Task<ServiceResult<object>> StartAsync(Guid sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);
        if (sprint.Status != "Planning")
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotInPlanningValue, ErrorCodes.SprintNotInPlanning,
                $"Sprint '{sprintId}' is not in Planning status.", 400);

        var activeSprint = await _sprintRepo.GetActiveByProjectAsync(sprint.ProjectId, ct);
        if (activeSprint != null)
            return ServiceResult<object>.Fail(
                ErrorCodes.OnlyOneActiveSprintValue, ErrorCodes.OnlyOneActiveSprint,
                $"Project '{sprint.ProjectId}' already has an active sprint.", 400);

        sprint.Status = "Active";
        sprint.DateUpdated = DateTime.UtcNow;
        await _sprintRepo.UpdateAsync(sprint, ct);
        await _dbContext.SaveChangesAsync(ct);

        var db = _redis.GetDatabase();
        await db.StringSetAsync(RedisKeys.SprintActive(sprint.ProjectId), sprint.SprintId.ToString(), TimeSpan.FromMinutes(2));

        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "SprintStarted", EntityType = "Sprint", EntityId = sprintId.ToString(), NotificationType = "SprintStarted" }, ct);

        return ServiceResult<object>.Ok(await BuildDetailResponse(sprint, ct), "Sprint started.");
    }

    public async Task<ServiceResult<object>> CompleteAsync(Guid sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);
        if (sprint.Status != "Active")
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintAlreadyCompletedValue, ErrorCodes.SprintAlreadyCompleted,
                $"Sprint '{sprintId}' is not active and cannot be completed.", 400);

        var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprintId, ct);
        var velocity = 0;
        var completedStories = 0;
        var totalStories = 0;

        foreach (var ss in sprintStories)
        {
            var story = await _storyRepo.GetByIdAsync(ss.StoryId, ct);
            if (story == null) continue;
            totalStories++;

            if (story.Status is "Done" or "Closed")
            {
                velocity += story.StoryPoints ?? 0;
                completedStories++;
            }
            else
            {
                story.SprintId = null;
                story.Status = "Backlog";
                story.DateUpdated = DateTime.UtcNow;
                await _storyRepo.UpdateAsync(story, ct);
            }
        }

        sprint.Status = "Completed";
        sprint.Velocity = velocity;
        sprint.DateUpdated = DateTime.UtcNow;
        await _sprintRepo.UpdateAsync(sprint, ct);
        await _dbContext.SaveChangesAsync(ct);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.SprintActive(sprint.ProjectId));
        await db.KeyDeleteAsync(RedisKeys.SprintMetrics(sprintId));

        var completionRate = totalStories > 0 ? Math.Round((decimal)completedStories / totalStories * 100, 2) : 0;
        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "SprintEnded", EntityType = "Sprint", EntityId = sprintId.ToString(), NotificationType = "SprintEnded", TemplateVariables = new Dictionary<string, string> { ["Velocity"] = velocity.ToString(), ["CompletionRate"] = completionRate.ToString() } }, ct);

        // Fire-and-forget: trigger analytics snapshot generation
        if (_analyticsSnapshotService != null)
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await _analyticsSnapshotService.TriggerSprintCloseSnapshotsAsync(sprintId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate analytics snapshots for sprint {SprintId}", sprintId);
                }
            });
        }

        return ServiceResult<object>.Ok(await BuildDetailResponse(sprint, ct), "Sprint completed.");
    }

    public async Task<ServiceResult<object>> CancelAsync(Guid sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);

        var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprintId, ct);
        foreach (var ss in sprintStories)
        {
            var story = await _storyRepo.GetByIdAsync(ss.StoryId, ct);
            if (story == null) continue;
            story.SprintId = null;
            story.Status = "Backlog";
            story.DateUpdated = DateTime.UtcNow;
            await _storyRepo.UpdateAsync(story, ct);
        }

        sprint.Status = "Cancelled";
        sprint.DateUpdated = DateTime.UtcNow;
        await _sprintRepo.UpdateAsync(sprint, ct);
        await _dbContext.SaveChangesAsync(ct);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.SprintActive(sprint.ProjectId));

        return ServiceResult<object>.Ok(await BuildDetailResponse(sprint, ct), "Sprint cancelled.");
    }

    public async Task<ServiceResult<object>> AddStoryAsync(Guid sprintId, Guid storyId, CancellationToken ct = default)
    {
        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);
        if (sprint.Status != "Planning")
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotInPlanningValue, ErrorCodes.SprintNotInPlanning,
                $"Sprint '{sprintId}' is not in Planning status.", 400);

        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);
        if (story.ProjectId != sprint.ProjectId)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryProjectMismatchValue, ErrorCodes.StoryProjectMismatch,
                $"Story '{storyId}' does not belong to the same project as sprint '{sprintId}'.", 400);

        var existing = await _sprintStoryRepo.GetAsync(sprintId, storyId, ct);
        if (existing != null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryAlreadyInSprintValue, ErrorCodes.StoryAlreadyInSprint,
                $"Story '{storyId}' is already in sprint '{sprintId}'.", 409);

        await _sprintStoryRepo.AddAsync(new SprintStory { SprintId = sprintId, StoryId = storyId }, ct);
        story.SprintId = sprintId;
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent("Story added to sprint.");
    }

    public async Task<ServiceResult<object>> RemoveStoryAsync(Guid sprintId, Guid storyId, CancellationToken ct = default)
    {
        var sprintStory = await _sprintStoryRepo.GetAsync(sprintId, storyId, ct);
        if (sprintStory == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotInSprintValue, ErrorCodes.StoryNotInSprint,
                $"Story '{storyId}' is not in sprint '{sprintId}'.", 400);

        sprintStory.RemovedDate = DateTime.UtcNow;
        await _sprintStoryRepo.UpdateAsync(sprintStory, ct);

        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story != null)
        {
            story.SprintId = null;
            story.DateUpdated = DateTime.UtcNow;
            await _storyRepo.UpdateAsync(story, ct);
        }
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent("Story removed from sprint.");
    }

    public async Task<ServiceResult<object>> GetMetricsAsync(Guid sprintId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.SprintMetrics(sprintId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedMetrics = JsonSerializer.Deserialize<SprintMetricsResponse>(cached!);
            if (cachedMetrics != null) return ServiceResult<object>.Ok(cachedMetrics);
        }

        var sprint = await _sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.SprintNotFoundValue, ErrorCodes.SprintNotFound,
                $"Sprint with ID '{sprintId}' was not found.", 404);

        var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprintId, ct);
        var stories = new List<Story>();
        foreach (var ss in sprintStories)
        {
            var story = await _storyRepo.GetByIdAsync(ss.StoryId, ct);
            if (story != null) stories.Add(story);
        }

        var tasks = await _taskRepo.ListBySprintAsync(sprintId, ct);
        var taskList = tasks.ToList();

        var completedStories = stories.Count(s => s.Status is "Done" or "Closed");
        var totalPoints = stories.Sum(s => s.StoryPoints ?? 0);
        var completedPoints = stories.Where(s => s.Status is "Done" or "Closed").Sum(s => s.StoryPoints ?? 0);

        var storiesByStatus = stories.GroupBy(s => s.Status).ToDictionary(g => g.Key, g => g.Count());
        var tasksByDept = taskList.GroupBy(t => t.DepartmentId?.ToString() ?? "Unassigned").ToDictionary(g => g.Key, g => g.Count());

        var burndown = CalculateBurndown(sprint, totalPoints, stories);

        var metrics = new SprintMetricsResponse
        {
            TotalStories = stories.Count, CompletedStories = completedStories,
            TotalStoryPoints = totalPoints, CompletedStoryPoints = completedPoints,
            CompletionRate = stories.Count > 0 ? Math.Round((decimal)completedStories / stories.Count * 100, 2) : 0,
            Velocity = completedPoints, StoriesByStatus = storiesByStatus,
            TasksByDepartment = tasksByDept, BurndownData = burndown
        };

        var json = JsonSerializer.Serialize(metrics);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(3));

        return ServiceResult<object>.Ok(metrics);
    }

    public async Task<ServiceResult<object>> GetVelocityHistoryAsync(Guid organizationId, int count, CancellationToken ct = default)
    {
        var sprints = await _sprintRepo.GetCompletedAsync(organizationId, count, ct);
        return ServiceResult<object>.Ok(sprints.Select(s => new VelocityResponse
        {
            SprintName = s.SprintName,
            Velocity = s.Velocity ?? 0, StartDate = s.StartDate, EndDate = s.EndDate
        }).ToList(), "Velocity history retrieved.");
    }

    public async Task<ServiceResult<object?>> GetActiveSprintAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default)
    {
        if (projectId.HasValue)
        {
            var sprint = await _sprintRepo.GetActiveByProjectAsync(projectId.Value, ct);
            if (sprint != null)
                return ServiceResult<object?>.Ok(await BuildDetailResponse(sprint, ct));
        }
        return ServiceResult<object?>.Ok(null);
    }

    private static List<BurndownDataPoint> CalculateBurndown(Sprint sprint, int totalPoints, List<Story> stories)
    {
        var points = new List<BurndownDataPoint>();
        var totalDays = (sprint.EndDate - sprint.StartDate).Days;
        if (totalDays <= 0) return points;

        for (var day = 0; day <= totalDays; day++)
        {
            var date = sprint.StartDate.AddDays(day);
            var idealRemaining = totalPoints - (totalPoints * day / totalDays);
            var actualCompleted = stories
                .Where(s => s.CompletedDate.HasValue && s.CompletedDate.Value.Date <= date.Date)
                .Sum(s => s.StoryPoints ?? 0);

            points.Add(new BurndownDataPoint
            {
                Date = date, IdealRemainingPoints = idealRemaining,
                RemainingPoints = totalPoints - actualCompleted
            });
        }
        return points;
    }

    private async Task<SprintDetailResponse> BuildDetailResponse(Sprint sprint, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(sprint.ProjectId, ct);
        var sprintStories = await _sprintStoryRepo.ListBySprintAsync(sprint.SprintId, ct);
        var storyResponses = new List<StoryListResponse>();

        foreach (var ss in sprintStories)
        {
            var story = await _storyRepo.GetByIdAsync(ss.StoryId, ct);
            if (story != null)
            {
                storyResponses.Add(new StoryListResponse
                {
                    StoryId = story.StoryId, StoryKey = story.StoryKey, Title = story.Title,
                    Priority = story.Priority, Status = story.Status, StoryPoints = story.StoryPoints,
                    ProjectName = project?.ProjectName ?? "", DueDate = story.DueDate, DateCreated = story.DateCreated
                });
            }
        }

        return new SprintDetailResponse
        {
            SprintId = sprint.SprintId, ProjectId = sprint.ProjectId,
            ProjectName = project?.ProjectName ?? "", SprintName = sprint.SprintName,
            Goal = sprint.Goal, StartDate = sprint.StartDate, EndDate = sprint.EndDate,
            Status = sprint.Status, Velocity = sprint.Velocity,
            Stories = storyResponses, DateCreated = sprint.DateCreated, DateUpdated = sprint.DateUpdated
        };
    }
}
