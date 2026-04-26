using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Labels;
using WorkService.Application.DTOs.Stories;
using WorkService.Application.DTOs.Tasks;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Helpers;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Domain.Interfaces.Repositories.Comments;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.StoryLabels;
using WorkService.Domain.Interfaces.Repositories.StoryLinks;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Domain.Interfaces.Services.Stories;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.Stories;

public class StoryService : IStoryService
{
    private static readonly HashSet<int> FibonacciSet = [1, 2, 3, 5, 8, 13, 21];
    private static readonly HashSet<string> ValidPriorities = ["Critical", "High", "Medium", "Low"];
    private static readonly HashSet<string> ValidStoryTypes = ["Feature", "Bug", "Improvement", "Epic", "Task"];

    private readonly IStoryRepository _storyRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly ICommentRepository _commentRepo;
    private readonly IStoryLabelRepository _storyLabelRepo;
    private readonly ILabelRepository _labelRepo;
    private readonly IStoryLinkRepository _storyLinkRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly IActivityLogRepository _activityLogRepo;
    private readonly IStoryIdGenerator _storyIdGenerator;
    private readonly IOutboxService _outbox;
    private readonly WorkDbContext _dbContext;
    private readonly ILogger<StoryService> _logger;

    public StoryService(
        IStoryRepository storyRepo, IProjectRepository projectRepo, ITaskRepository taskRepo,
        ICommentRepository commentRepo, IStoryLabelRepository storyLabelRepo, ILabelRepository labelRepo,
        IStoryLinkRepository storyLinkRepo, ISprintRepository sprintRepo,
        IActivityLogRepository activityLogRepo, IStoryIdGenerator storyIdGenerator,
        IOutboxService outbox, WorkDbContext dbContext, ILogger<StoryService> logger)
    {
        _storyRepo = storyRepo; _projectRepo = projectRepo; _taskRepo = taskRepo;
        _commentRepo = commentRepo; _storyLabelRepo = storyLabelRepo; _labelRepo = labelRepo;
        _storyLinkRepo = storyLinkRepo; _sprintRepo = sprintRepo;
        _activityLogRepo = activityLogRepo; _storyIdGenerator = storyIdGenerator;
        _outbox = outbox; _dbContext = dbContext; _logger = logger;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid reporterId, object request, CancellationToken ct = default)
    {
        var req = (CreateStoryRequest)request;

        var project = await _projectRepo.GetByIdAsync(req.ProjectId, ct);
        if (project == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound,
                $"Project with ID '{req.ProjectId}' was not found.", 404);
        if (project.OrganizationId != organizationId)
            throw new OrganizationMismatchException();

        if (req.StoryPoints.HasValue && !FibonacciSet.Contains(req.StoryPoints.Value))
            return ServiceResult<object>.Fail(
                ErrorCodes.InvalidStoryPointsValue, ErrorCodes.InvalidStoryPoints,
                $"Story points value '{req.StoryPoints.Value}' is not a valid Fibonacci number.", 400);
        if (!ValidPriorities.Contains(req.Priority))
            return ServiceResult<object>.Fail(
                ErrorCodes.InvalidPriorityValue, ErrorCodes.InvalidPriority,
                $"Priority '{req.Priority}' is not valid.", 400);
        if (!ValidStoryTypes.Contains(req.StoryType))
            return ServiceResult<object>.Fail(
                ErrorCodes.InvalidStoryTypeValue, ErrorCodes.InvalidStoryType,
                $"Story type '{req.StoryType}' is not valid.", 400);

        var (storyKey, seqNum) = await _storyIdGenerator.GenerateNextIdAsync(req.ProjectId, ct);

        var story = new Story
        {
            OrganizationId = organizationId,
            ProjectId = req.ProjectId,
            StoryKey = storyKey,
            SequenceNumber = seqNum,
            Title = req.Title,
            Description = req.Description,
            AcceptanceCriteria = req.AcceptanceCriteria,
            StoryPoints = req.StoryPoints,
            Priority = req.Priority,
            StoryType = req.StoryType,
            Status = "Backlog",
            ReporterId = reporterId,
            DepartmentId = req.DepartmentId,
            DueDate = req.DueDate
        };

        await _storyRepo.AddAsync(story, ct);

        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = organizationId, EntityType = "Story", EntityId = story.StoryId,
            StoryKey = storyKey, Action = "Created", ActorId = reporterId, ActorName = "System",
            Description = $"Story {storyKey} created"
        }, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _outbox.PublishAsync(new { MessageType = "AuditEvent", Action = "StoryCreated", EntityType = "Story", EntityId = story.StoryId.ToString(), OrganizationId = organizationId, UserId = reporterId }, ct);

        var detail = await BuildDetailResponse(story, ct);
        return ServiceResult<object>.Created(detail, "Story created successfully.");
    }

    public async Task<ServiceResult<object>> GetByIdAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);
        var detail = await BuildDetailResponse(story, ct);
        return ServiceResult<object>.Ok(detail);
    }

    public async Task<ServiceResult<object>> GetByKeyAsync(string storyKey, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByKeyAsync(Guid.Empty, storyKey, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryKeyNotFoundValue, ErrorCodes.StoryKeyNotFound,
                $"Story with key '{storyKey}' was not found.", 404);
        var detail = await BuildDetailResponse(story, ct);
        return ServiceResult<object>.Ok(detail);
    }

    public async Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, Guid? projectId,
        string? status, string? priority, string? storyType, Guid? departmentId, Guid? assigneeId, Guid? sprintId,
        List<string>? labels, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var (items, totalCount) = await _storyRepo.ListAsync(organizationId, page, pageSize, projectId,
            status, priority, storyType, departmentId, assigneeId, sprintId, labels, dateFrom, dateTo, ct);

        var responses = items.Select(s => new StoryListResponse
        {
            StoryId = s.StoryId, StoryKey = s.StoryKey, Title = s.Title,
            Priority = s.Priority, StoryType = s.StoryType, Status = s.Status, StoryPoints = s.StoryPoints,
            DueDate = s.DueDate, DateCreated = s.DateCreated
        }).ToList();

        var paginated = new PaginatedResponse<StoryListResponse>
        {
            Data = responses, TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return ServiceResult<object>.Ok(paginated, "Stories retrieved.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid storyId, Guid actorId, object request, CancellationToken ct = default)
    {
        var req = (UpdateStoryRequest)request;
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);

        if (req.StoryPoints.HasValue && !FibonacciSet.Contains(req.StoryPoints.Value))
            return ServiceResult<object>.Fail(
                ErrorCodes.InvalidStoryPointsValue, ErrorCodes.InvalidStoryPoints,
                $"Story points value '{req.StoryPoints.Value}' is not a valid Fibonacci number.", 400);
        if (req.Priority != null && !ValidPriorities.Contains(req.Priority))
            return ServiceResult<object>.Fail(
                ErrorCodes.InvalidPriorityValue, ErrorCodes.InvalidPriority,
                $"Priority '{req.Priority}' is not valid.", 400);

        if (req.Title != null) story.Title = req.Title;
        if (req.Description != null)
        {
            var old = story.Description;
            story.Description = req.Description;
            await LogActivity(story, "DescriptionUpdated", actorId, old, req.Description, "Description updated", ct);
        }
        if (req.AcceptanceCriteria != null) story.AcceptanceCriteria = req.AcceptanceCriteria;
        if (req.StoryPoints.HasValue)
        {
            var old = story.StoryPoints?.ToString();
            story.StoryPoints = req.StoryPoints;
            await LogActivity(story, "PointsChanged", actorId, old, req.StoryPoints.ToString(), "Story points updated", ct);
        }
        if (req.Priority != null && req.Priority != story.Priority)
        {
            var old = story.Priority;
            story.Priority = req.Priority;
            await LogActivity(story, "PriorityChanged", actorId, old, req.Priority, "Priority changed", ct);
        }
        if (req.StoryType != null && req.StoryType != story.StoryType)
        {
            if (!ValidStoryTypes.Contains(req.StoryType))
                return ServiceResult<object>.Fail(
                    ErrorCodes.InvalidStoryTypeValue, ErrorCodes.InvalidStoryType,
                    $"Story type '{req.StoryType}' is not valid.", 400);
            var old = story.StoryType;
            story.StoryType = req.StoryType;
            await LogActivity(story, "TypeChanged", actorId, old, req.StoryType, "Story type changed", ct);
        }
        if (req.DepartmentId.HasValue) story.DepartmentId = req.DepartmentId;
        if (req.DueDate.HasValue)
        {
            var old = story.DueDate?.ToString("o");
            story.DueDate = req.DueDate;
            await LogActivity(story, "DueDateChanged", actorId, old, req.DueDate.Value.ToString("o"), "Due date changed", ct);
        }

        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);
        await _dbContext.SaveChangesAsync(ct);
        var detail = await BuildDetailResponse(story, ct);
        return ServiceResult<object>.Ok(detail, "Story updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);

        if (story.SprintId.HasValue)
        {
            var sprint = await _sprintRepo.GetByIdAsync(story.SprintId.Value, ct);
            if (sprint?.Status == "Active")
                return ServiceResult<object>.Fail(
                    ErrorCodes.StoryInActiveSprintValue, ErrorCodes.StoryInActiveSprint,
                    $"Story '{storyId}' cannot be deleted because it is in an active sprint.", 400);
        }

        story.FlgStatus = "D";
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Story deleted.");
    }

    public async Task<ServiceResult<object>> TransitionStatusAsync(Guid storyId, Guid actorId, string newStatus, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);

        if (!WorkflowStateMachine.IsValidStoryTransition(story.Status, newStatus))
            return ServiceResult<object>.Fail(
                ErrorCodes.InvalidStoryTransitionValue, ErrorCodes.InvalidStoryTransition,
                $"Cannot transition story from '{story.Status}' to '{newStatus}'.", 400);

        // Preconditions
        if (newStatus == "Ready")
        {
            if (string.IsNullOrWhiteSpace(story.Description))
                return ServiceResult<object>.Fail(
                    ErrorCodes.StoryDescriptionRequiredValue, ErrorCodes.StoryDescriptionRequired,
                    "Story must have a description before moving to Ready.", 400);
            if (!story.StoryPoints.HasValue || story.StoryPoints.Value <= 0)
                return ServiceResult<object>.Fail(
                    ErrorCodes.StoryRequiresPointsValue, ErrorCodes.StoryRequiresPoints,
                    "Story must have story points before moving to Ready.", 400);
        }
        if (newStatus == "InProgress" && !story.AssigneeId.HasValue)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryRequiresAssigneeValue, ErrorCodes.StoryRequiresAssignee,
                "Story must have an assignee before moving to InProgress.", 400);
        if (newStatus == "InReview")
        {
            var taskCount = await _storyRepo.CountTasksAsync(storyId, ct);
            if (taskCount == 0)
                return ServiceResult<object>.Fail(
                    ErrorCodes.StoryRequiresTasksValue, ErrorCodes.StoryRequiresTasks,
                    "Story must have tasks before moving to InReview.", 400);
            var allDevDone = await _storyRepo.AllDevTasksDoneAsync(storyId, ct);
            if (!allDevDone)
                return ServiceResult<object>.Fail(
                    ErrorCodes.StoryRequiresTasksValue, ErrorCodes.StoryRequiresTasks,
                    "All dev tasks must be done before moving to InReview.", 400);
        }
        if (newStatus == "Done")
        {
            var allDone = await _storyRepo.AllTasksDoneAsync(storyId, ct);
            if (!allDone)
                return ServiceResult<object>.Fail(
                    ErrorCodes.StoryRequiresTasksValue, ErrorCodes.StoryRequiresTasks,
                    "All tasks must be done before moving to Done.", 400);
        }

        var oldStatus = story.Status;
        story.Status = newStatus;
        if (newStatus == "Done") story.CompletedDate = DateTime.UtcNow;
        story.DateUpdated = DateTime.UtcNow;

        await _storyRepo.UpdateAsync(story, ct);
        await LogActivity(story, "StatusChanged", actorId, oldStatus, newStatus, $"Status changed from {oldStatus} to {newStatus}", ct);
        await _dbContext.SaveChangesAsync(ct);
        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "StoryStatusChanged", EntityType = "Story", EntityId = storyId.ToString(), NotificationType = "StoryStatusChanged" }, ct);

        var detail = await BuildDetailResponse(story, ct);
        return ServiceResult<object>.Ok(detail, "Story status updated.");
    }

    public async Task<ServiceResult<object>> AssignAsync(Guid storyId, Guid actorId, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);

        story.AssigneeId = assigneeId;
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);

        await LogActivity(story, "Assigned", actorId, null, assigneeId.ToString(), $"Story assigned", ct);
        await _dbContext.SaveChangesAsync(ct);
        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "StoryAssigned", EntityType = "Story", EntityId = storyId.ToString(), NotificationType = "StoryAssigned" }, ct);

        var detail = await BuildDetailResponse(story, ct);
        return ServiceResult<object>.Ok(detail, "Story assigned.");
    }

    public async Task<ServiceResult<object>> UnassignAsync(Guid storyId, Guid actorId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);

        var oldAssignee = story.AssigneeId?.ToString();
        story.AssigneeId = null;
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);
        await LogActivity(story, "Unassigned", actorId, oldAssignee, null, "Story unassigned", ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Story unassigned.");
    }

    public async Task<ServiceResult<object>> CreateLinkAsync(Guid storyId, Guid targetStoryId, string linkType, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct);
        if (story == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{storyId}' was not found.", 404);
        var target = await _storyRepo.GetByIdAsync(targetStoryId, ct);
        if (target == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.StoryNotFoundValue, ErrorCodes.StoryNotFound,
                $"Story with ID '{targetStoryId}' was not found.", 404);

        var inverseLinkType = linkType switch
        {
            "blocks" => "is_blocked_by",
            "is_blocked_by" => "blocks",
            "relates_to" => "relates_to",
            "duplicates" => "duplicates",
            _ => linkType
        };

        await _storyLinkRepo.AddAsync(new StoryLink { OrganizationId = story.OrganizationId, SourceStoryId = storyId, TargetStoryId = targetStoryId, LinkType = linkType }, ct);
        await _storyLinkRepo.AddAsync(new StoryLink { OrganizationId = story.OrganizationId, SourceStoryId = targetStoryId, TargetStoryId = storyId, LinkType = inverseLinkType }, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Story link created.");
    }

    public async Task<ServiceResult<object>> DeleteLinkAsync(Guid storyId, Guid linkId, CancellationToken ct = default)
    {
        var link = await _storyLinkRepo.GetByIdAsync(linkId, ct);
        if (link == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.NotFoundValue, ErrorCodes.NotFound,
                $"Link with ID '{linkId}' was not found.", 404);
        var inverse = await _storyLinkRepo.FindInverseAsync(link.TargetStoryId, link.SourceStoryId, GetInverseLinkType(link.LinkType), ct);

        await _storyLinkRepo.DeleteAsync(link, ct);
        if (inverse != null) await _storyLinkRepo.DeleteAsync(inverse, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Story link deleted.");
    }

    public async Task<ServiceResult<object>> ApplyLabelAsync(Guid storyId, Guid labelId, CancellationToken ct = default)
    {
        var count = await _storyLabelRepo.CountByStoryAsync(storyId, ct);
        if (count >= 10)
            return ServiceResult<object>.Fail(
                ErrorCodes.MaxLabelsPerStoryValue, ErrorCodes.MaxLabelsPerStory,
                $"Story '{storyId}' already has the maximum of 10 labels.", 400);

        var existing = await _storyLabelRepo.GetAsync(storyId, labelId, ct);
        if (existing != null)
            return ServiceResult<object>.NoContent("Label already applied.");

        await _storyLabelRepo.AddAsync(new StoryLabel { StoryId = storyId, LabelId = labelId }, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Label applied.");
    }

    public async Task<ServiceResult<object>> RemoveLabelAsync(Guid storyId, Guid labelId, CancellationToken ct = default)
    {
        var existing = await _storyLabelRepo.GetAsync(storyId, labelId, ct);
        if (existing != null)
        {
            await _storyLabelRepo.DeleteAsync(existing, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        return ServiceResult<object>.NoContent("Label removed.");
    }

    private static string GetInverseLinkType(string linkType) => linkType switch
    {
        "blocks" => "is_blocked_by",
        "is_blocked_by" => "blocks",
        _ => linkType
    };

    private async System.Threading.Tasks.Task LogActivity(Story story, string action, Guid actorId, string? oldValue, string? newValue, string description, CancellationToken ct)
    {
        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = story.OrganizationId, EntityType = "Story", EntityId = story.StoryId,
            StoryKey = story.StoryKey, Action = action, ActorId = actorId, ActorName = "System",
            OldValue = oldValue, NewValue = newValue, Description = description
        }, ct);
    }

    private async Task<StoryDetailResponse> BuildDetailResponse(Story story, CancellationToken ct)
    {
        var totalTasks = await _storyRepo.CountTasksAsync(story.StoryId, ct);
        var completedTasks = await _storyRepo.CountCompletedTasksAsync(story.StoryId, ct);
        var tasks = await _taskRepo.ListByStoryAsync(story.StoryId, ct);
        var storyLabels = await _storyLabelRepo.ListByStoryAsync(story.StoryId, ct);
        var links = await _storyLinkRepo.ListByStoryAsync(story.StoryId, ct);
        var comments = await _commentRepo.ListByEntityAsync("Story", story.StoryId, ct);
        var project = await _projectRepo.GetByIdAsync(story.ProjectId, ct);

        var labelResponses = new List<LabelResponse>();
        foreach (var sl in storyLabels)
        {
            var label = await _labelRepo.GetByIdAsync(sl.LabelId, ct);
            if (label != null)
                labelResponses.Add(new LabelResponse { LabelId = label.LabelId, Name = label.Name, Color = label.Color });
        }

        var linkResponses = links.Where(l => l.SourceStoryId == story.StoryId).Select(l => new StoryLinkResponse
        {
            LinkId = l.StoryLinkId, TargetStoryId = l.TargetStoryId, LinkType = l.LinkType
        }).ToList();

        var deptContributions = tasks.GroupBy(t => t.DepartmentId).Select(g => new DepartmentContribution
        {
            DepartmentName = g.Key?.ToString() ?? "Unassigned",
            TaskCount = g.Count(),
            CompletedTaskCount = g.Count(t => t.Status == "Done")
        }).ToList();

        return new StoryDetailResponse
        {
            StoryId = story.StoryId, ProjectId = story.ProjectId,
            ProjectName = project?.ProjectName ?? "", ProjectKey = project?.ProjectKey ?? "",
            StoryKey = story.StoryKey, SequenceNumber = story.SequenceNumber,
            Title = story.Title, Description = story.Description,
            AcceptanceCriteria = story.AcceptanceCriteria, StoryPoints = story.StoryPoints,
            Priority = story.Priority, StoryType = story.StoryType, Status = story.Status,
            AssigneeId = story.AssigneeId, ReporterId = story.ReporterId,
            SprintId = story.SprintId, DepartmentId = story.DepartmentId,
            DueDate = story.DueDate, CompletedDate = story.CompletedDate,
            TotalTaskCount = totalTasks, CompletedTaskCount = completedTasks,
            CompletionPercentage = totalTasks > 0 ? Math.Round((decimal)completedTasks / totalTasks * 100, 2) : 0,
            DepartmentContributions = deptContributions,
            Tasks = tasks.Select(t => new TaskDetailResponse
            {
                TaskId = t.TaskId, StoryId = t.StoryId, StoryKey = story.StoryKey,
                Title = t.Title, Description = t.Description, TaskType = t.TaskType,
                Status = t.Status, Priority = t.Priority, AssigneeId = t.AssigneeId,
                DepartmentId = t.DepartmentId, EstimatedHours = t.EstimatedHours,
                ActualHours = t.ActualHours, DueDate = t.DueDate, CompletedDate = t.CompletedDate,
                FlgStatus = t.FlgStatus, DateCreated = t.DateCreated, DateUpdated = t.DateUpdated
            }).ToList(),
            Labels = labelResponses, Links = linkResponses,
            CommentCount = comments.Count(),
            FlgStatus = story.FlgStatus, DateCreated = story.DateCreated, DateUpdated = story.DateUpdated
        };
    }

    public async Task<ServiceResult<object>> BulkUpdateStatusAsync(Guid organizationId, Guid actorId, List<Guid> storyIds, string newStatus, CancellationToken ct = default)
    {
        var results = new List<object>();
        foreach (var storyId in storyIds)
        {
            var result = await TransitionStatusAsync(storyId, actorId, newStatus, ct);
            if (result.IsSuccess)
                results.Add(new { StoryId = storyId, Success = true });
            else
                results.Add(new { StoryId = storyId, Success = false, Error = result.Message });
        }
        var response = new { Updated = results.Count(r => ((dynamic)r).Success), Total = storyIds.Count, Results = results };
        return ServiceResult<object>.Ok(response, "Bulk status update completed.");
    }

    public async Task<ServiceResult<object>> BulkAssignAsync(Guid organizationId, Guid actorId, List<Guid> storyIds, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default)
    {
        var results = new List<object>();
        foreach (var storyId in storyIds)
        {
            var result = await AssignAsync(storyId, actorId, assigneeId, actorRole, actorDepartmentId, ct);
            if (result.IsSuccess)
                results.Add(new { StoryId = storyId, Success = true });
            else
                results.Add(new { StoryId = storyId, Success = false, Error = result.Message });
        }
        var response = new { Assigned = results.Count(r => ((dynamic)r).Success), Total = storyIds.Count, Results = results };
        return ServiceResult<object>.Ok(response, "Bulk assign completed.");
    }
}
