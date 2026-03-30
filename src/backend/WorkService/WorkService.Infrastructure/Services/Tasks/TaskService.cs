using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs.Tasks;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Helpers;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Domain.Interfaces.Services;
using WorkService.Infrastructure.Services.ServiceClients;

namespace WorkService.Infrastructure.Services.Tasks;

public class TaskService : ITaskService
{
    private static readonly HashSet<string> ValidPriorities = ["Critical", "High", "Medium", "Low"];

    private readonly ITaskRepository _taskRepo;
    private readonly IStoryRepository _storyRepo;
    private readonly IActivityLogRepository _activityLogRepo;
    private readonly IOutboxService _outbox;
    private readonly IProfileServiceClient? _profileClient;
    private readonly ILogger<TaskService> _logger;    public TaskService(
        ITaskRepository taskRepo, IStoryRepository storyRepo,
        IActivityLogRepository activityLogRepo, IOutboxService outbox,
        ILogger<TaskService> logger, IProfileServiceClient? profileClient = null)
    {
        _taskRepo = taskRepo; _storyRepo = storyRepo;
        _activityLogRepo = activityLogRepo; _outbox = outbox;
        _logger = logger; _profileClient = profileClient;
    }

    public async Task<object> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default)
    {
        var req = (CreateTaskRequest)request;
        var deptCode = TaskTypeDepartmentMap.GetDepartmentCode(req.TaskType);

        var story = await _storyRepo.GetByIdAsync(req.StoryId, ct)
            ?? throw new StoryNotFoundException(req.StoryId);

        Guid? departmentId = null;
        if (_profileClient != null)
        {
            try
            {
                var dept = await _profileClient.GetDepartmentByCodeAsync(organizationId, deptCode, ct);
                departmentId = dept?.DepartmentId;
            }
            catch { /* department lookup is best-effort */ }
        }

        var task = new Domain.Entities.Task
        {
            OrganizationId = organizationId, StoryId = req.StoryId,
            Title = req.Title, Description = req.Description,
            TaskType = req.TaskType, Status = "ToDo", Priority = req.Priority,
            DepartmentId = departmentId, EstimatedHours = req.EstimatedHours,
            DueDate = req.DueDate
        };

        await _taskRepo.AddAsync(task, ct);

        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = organizationId, EntityType = "Task", EntityId = task.TaskId,
            StoryKey = story.StoryKey, Action = "Created", ActorId = creatorId, ActorName = "System",
            Description = $"Task '{task.Title}' created"
        }, ct);

        return BuildTaskResponse(task, story.StoryKey);
    }

    public async Task<object> GetByIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);
        var story = await _storyRepo.GetByIdAsync(task.StoryId, ct);
        return BuildTaskResponse(task, story?.StoryKey ?? "");
    }

    public async Task<object> ListByStoryAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        var tasks = await _taskRepo.ListByStoryAsync(storyId, ct);
        return tasks.Select(t => BuildTaskResponse(t, story?.StoryKey ?? "")).ToList();
    }

    public async Task<object> UpdateAsync(Guid taskId, Guid actorId, object request, CancellationToken ct = default)
    {
        var req = (UpdateTaskRequest)request;
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        if (req.Title != null) task.Title = req.Title;
        if (req.Description != null) task.Description = req.Description;
        if (req.Priority != null)
        {
            if (!ValidPriorities.Contains(req.Priority)) throw new InvalidPriorityException(req.Priority);
            task.Priority = req.Priority;
        }
        if (req.EstimatedHours.HasValue) task.EstimatedHours = req.EstimatedHours;
        if (req.DueDate.HasValue) task.DueDate = req.DueDate;
        task.DateUpdated = DateTime.UtcNow;

        await _taskRepo.UpdateAsync(task, ct);
        var story = await _storyRepo.GetByIdAsync(task.StoryId, ct);
        return BuildTaskResponse(task, story?.StoryKey ?? "");
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);
        if (task.Status == "InProgress") throw new TaskInProgressException(taskId);

        task.FlgStatus = "D";
        task.DateUpdated = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);
    }

    public async Task<object> TransitionStatusAsync(Guid taskId, Guid actorId, string newStatus, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        if (!WorkflowStateMachine.IsValidTaskTransition(task.Status, newStatus))
            throw new InvalidTaskTransitionException(task.Status, newStatus);

        if (newStatus == "InProgress" && !task.AssigneeId.HasValue)
            throw new StoryRequiresAssigneeException();

        var oldStatus = task.Status;
        task.Status = newStatus;
        if (newStatus == "Done") task.CompletedDate = DateTime.UtcNow;
        task.DateUpdated = DateTime.UtcNow;

        await _taskRepo.UpdateAsync(task, ct);

        var story = await _storyRepo.GetByIdAsync(task.StoryId, ct);
        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = task.OrganizationId, EntityType = "Task", EntityId = task.TaskId,
            StoryKey = story?.StoryKey, Action = "StatusChanged", ActorId = actorId, ActorName = "System",
            OldValue = oldStatus, NewValue = newStatus,
            Description = $"Task status changed from {oldStatus} to {newStatus}"
        }, ct);

        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "TaskStatusChanged", EntityType = "Task", EntityId = taskId.ToString() }, ct);

        return BuildTaskResponse(task, story?.StoryKey ?? "");
    }

    public async Task<object> AssignAsync(Guid taskId, Guid actorId, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        if (_profileClient != null && task.DepartmentId.HasValue)
        {
            var members = await _profileClient.GetDepartmentMembersAsync(task.DepartmentId.Value, ct);
            if (!members.Any(m => m.Id == assigneeId))
                throw new AssigneeNotInDepartmentException(assigneeId, task.DepartmentId.Value.ToString());

            var member = members.FirstOrDefault(m => m.Id == assigneeId);
            if (member != null)
            {
                var activeCount = await _taskRepo.CountActiveByAssigneeAsync(assigneeId, ct);
                if (activeCount >= member.MaxConcurrentTasks)
                    throw new AssigneeAtCapacityException(assigneeId);
            }
        }

        task.AssigneeId = assigneeId;
        task.DateUpdated = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);

        var story = await _storyRepo.GetByIdAsync(task.StoryId, ct);
        return BuildTaskResponse(task, story?.StoryKey ?? "");
    }

    public async Task<object> SelfAssignAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        task.AssigneeId = userId;
        task.DateUpdated = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);

        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "TaskAssigned", EntityType = "Task", EntityId = taskId.ToString() }, ct);

        var story = await _storyRepo.GetByIdAsync(task.StoryId, ct);
        return BuildTaskResponse(task, story?.StoryKey ?? "");
    }

    public async System.Threading.Tasks.Task UnassignAsync(Guid taskId, Guid actorId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);
        task.AssigneeId = null;
        task.DateUpdated = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);

        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = task.OrganizationId, EntityType = "Task", EntityId = task.TaskId,
            Action = "Unassigned", ActorId = actorId, ActorName = "System", Description = "Task unassigned"
        }, ct);
    }

    public async System.Threading.Tasks.Task LogHoursAsync(Guid taskId, Guid actorId, decimal hours, string? description, CancellationToken ct = default)
    {
        if (hours <= 0) throw new HoursMustBePositiveException();

        var task = await _taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        task.ActualHours = (task.ActualHours ?? 0) + hours;
        task.DateUpdated = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);
    }

    public async Task<object> SuggestAssigneeAsync(string taskType, Guid organizationId, CancellationToken ct = default)
    {
        var deptCode = TaskTypeDepartmentMap.GetDepartmentCode(taskType);

        if (_profileClient == null)
            return new SuggestAssigneeResponse();

        var dept = await _profileClient.GetDepartmentByCodeAsync(organizationId, deptCode, ct);
        if (dept == null) return new SuggestAssigneeResponse();

        var members = await _profileClient.GetDepartmentMembersAsync(dept.DepartmentId, ct);
        Application.Contracts.TeamMemberResponse? best = null;
        var lowestCount = int.MaxValue;

        foreach (var m in members.Where(m => m.Availability == "Available"))
        {
            var activeCount = await _taskRepo.CountActiveByAssigneeAsync(m.Id, ct);
            if (activeCount < m.MaxConcurrentTasks && activeCount < lowestCount)
            {
                lowestCount = activeCount;
                best = m;
            }
        }

        return new SuggestAssigneeResponse
        {
            SuggestedAssigneeId = best?.Id,
            SuggestedAssigneeName = best?.DisplayName,
            ActiveTaskCount = best != null ? lowestCount : 0,
            MaxConcurrentTasks = best?.MaxConcurrentTasks ?? 0
        };
    }

    private static TaskDetailResponse BuildTaskResponse(Domain.Entities.Task task, string storyKey) => new()
    {
        TaskId = task.TaskId, StoryId = task.StoryId, StoryKey = storyKey,
        Title = task.Title, Description = task.Description, TaskType = task.TaskType,
        Status = task.Status, Priority = task.Priority, AssigneeId = task.AssigneeId,
        DepartmentId = task.DepartmentId, EstimatedHours = task.EstimatedHours,
        ActualHours = task.ActualHours, DueDate = task.DueDate, CompletedDate = task.CompletedDate,
        FlgStatus = task.FlgStatus, DateCreated = task.DateCreated, DateUpdated = task.DateUpdated
    };
}
