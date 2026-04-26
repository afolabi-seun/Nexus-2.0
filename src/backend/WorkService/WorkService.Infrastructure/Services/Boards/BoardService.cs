using System.Text.Json;
using WorkService.Application.DTOs.Boards;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Boards;
using WorkService.Domain.Results;
using StackExchange.Redis;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Boards;

public class BoardService : IBoardService
{
    private static readonly string[] StoryStatuses = ["Backlog", "Ready", "InProgress", "InReview", "QA", "Done", "Closed"];
    private static readonly string[] TaskStatuses = ["ToDo", "InProgress", "InReview", "Done"];

    private readonly IStoryRepository _storyRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly ISprintStoryRepository _sprintStoryRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IConnectionMultiplexer _redis;

    public BoardService(
        IStoryRepository storyRepo, ITaskRepository taskRepo,
        ISprintRepository sprintRepo, ISprintStoryRepository sprintStoryRepo,
        IProjectRepository projectRepo, IConnectionMultiplexer redis)
    {
        _storyRepo = storyRepo; _taskRepo = taskRepo;
        _sprintRepo = sprintRepo; _sprintStoryRepo = sprintStoryRepo;
        _projectRepo = projectRepo; _redis = redis;
    }

    public async Task<ServiceResult<object>> GetKanbanBoardAsync(Guid organizationId, Guid? projectId, Guid? sprintId,
        Guid? departmentId, Guid? assigneeId, string? priority, List<string>? labels, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.BoardKanban(organizationId, projectId, sprintId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedBoard = JsonSerializer.Deserialize<KanbanBoardResponse>(cached!);
            if (cachedBoard != null) return ServiceResult<object>.Ok(cachedBoard);
        }

        var (stories, _) = await _storyRepo.ListAsync(organizationId, 1, 1000, projectId,
            null, priority, null, departmentId, assigneeId, sprintId, labels, null, null, ct);

        var columns = StoryStatuses.Select(status =>
        {
            var statusStories = stories.Where(s => s.Status == status).ToList();
            return new KanbanColumn
            {
                Status = status, CardCount = statusStories.Count,
                TotalPoints = statusStories.Sum(s => s.StoryPoints ?? 0),
                Cards = statusStories.Select(s => new KanbanCard
                {
                    StoryId = s.StoryId, StoryKey = s.StoryKey, Title = s.Title,
                    Priority = s.Priority, StoryPoints = s.StoryPoints
                }).ToList()
            };
        }).ToList();

        var response = new KanbanBoardResponse { Columns = columns };
        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(2));

        return ServiceResult<object>.Ok(response);
    }

    public async Task<ServiceResult<object>> GetSprintBoardAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default)
    {
        Domain.Entities.Sprint? activeSprint = null;
        if (projectId.HasValue)
            activeSprint = await _sprintRepo.GetActiveByProjectAsync(projectId.Value, ct);

        if (activeSprint == null)
        {
            return ServiceResult<object>.Ok(new SprintBoardResponse
            {
                HasActiveSprint = false,
                Message = "No active sprint found"
            });
        }

        var tasks = await _taskRepo.ListBySprintAsync(activeSprint.SprintId, ct);
        var project = await _projectRepo.GetByIdAsync(activeSprint.ProjectId, ct);

        var columns = TaskStatuses.Select(status => new SprintBoardColumn
        {
            Status = status,
            Cards = tasks.Where(t => t.Status == status).Select(t => new SprintBoardCard
            {
                TaskId = t.TaskId, TaskTitle = t.Title, TaskType = t.TaskType,
                Priority = t.Priority, ProjectName = project?.ProjectName ?? ""
            }).ToList()
        }).ToList();

        return ServiceResult<object>.Ok(new SprintBoardResponse
        {
            SprintName = activeSprint.SprintName, HasActiveSprint = true,
            ProjectName = project?.ProjectName, Columns = columns
        });
    }

    public async Task<ServiceResult<object>> GetBacklogAsync(Guid organizationId, Guid? projectId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.BoardBacklog(organizationId, projectId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedBacklog = JsonSerializer.Deserialize<BacklogResponse>(cached!);
            if (cachedBacklog != null) return ServiceResult<object>.Ok(cachedBacklog);
        }

        var (stories, _) = await _storyRepo.ListAsync(organizationId, 1, 1000, projectId,
            null, null, null, null, null, null, null, null, null, ct);

        var backlogStories = stories.Where(s => s.SprintId == null).ToList();

        var priorityOrder = new Dictionary<string, int>
        {
            ["Critical"] = 0, ["High"] = 1, ["Medium"] = 2, ["Low"] = 3
        };

        var sorted = backlogStories
            .OrderBy(s => priorityOrder.GetValueOrDefault(s.Priority, 99))
            .ThenBy(s => s.DateCreated)
            .ToList();

        var response = new BacklogResponse
        {
            TotalStories = sorted.Count,
            TotalPoints = sorted.Sum(s => s.StoryPoints ?? 0),
            Items = sorted.Select(s => new BacklogItem
            {
                StoryId = s.StoryId, StoryKey = s.StoryKey, Title = s.Title,
                Priority = s.Priority, StoryPoints = s.StoryPoints, Status = s.Status,
                DateCreated = s.DateCreated
            }).ToList()
        };

        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(2));

        return ServiceResult<object>.Ok(response);
    }

    public async Task<ServiceResult<object>> GetDepartmentBoardAsync(Guid organizationId, Guid? projectId, Guid? sprintId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.BoardDept(organizationId, projectId, sprintId);
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedBoard = JsonSerializer.Deserialize<DepartmentBoardResponse>(cached!);
            if (cachedBoard != null) return ServiceResult<object>.Ok(cachedBoard);
        }

        var tasks = await _taskRepo.ListByDepartmentAsync(organizationId, sprintId, ct);
        var taskList = tasks.ToList();

        if (projectId.HasValue)
        {
            var storyIds = (await _storyRepo.ListAsync(organizationId, 1, 10000, projectId,
                null, null, null, null, null, null, null, null, null, ct)).Items.Select(s => s.StoryId).ToHashSet();
            taskList = taskList.Where(t => storyIds.Contains(t.StoryId)).ToList();
        }

        var groups = taskList.GroupBy(t => t.DepartmentId?.ToString() ?? "Unassigned")
            .Select(g => new DepartmentBoardGroup
            {
                DepartmentName = g.Key, TaskCount = g.Count(),
                TasksByStatus = g.GroupBy(t => t.Status).ToDictionary(sg => sg.Key, sg => sg.Count())
            }).ToList();

        var response = new DepartmentBoardResponse { Departments = groups };
        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(2));

        return ServiceResult<object>.Ok(response);
    }
}
