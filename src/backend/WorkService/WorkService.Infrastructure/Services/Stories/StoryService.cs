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
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.Stories;

public class StoryService : IStoryService
{
    private static readonly HashSet<int> FibonacciSet = [1, 2, 3, 5, 8, 13, 21];
    private static readonly HashSet<string> ValidPriorities = ["Critical", "High", "Medium", "Low"];

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

    public async Task<object> CreateAsync(Guid organizationId, Guid reporterId, object request, CancellationToken ct = default)
    {
        var req = (CreateStoryRequest)request;

        var project = await _projectRepo.GetByIdAsync(req.ProjectId, ct)
            ?? throw new ProjectNotFoundException(req.ProjectId);
        if (project.OrganizationId != organizationId)
            throw new OrganizationMismatchException();

        if (req.StoryPoints.HasValue && !FibonacciSet.Contains(req.StoryPoints.Value))
            throw new InvalidStoryPointsException(req.StoryPoints.Value);
        if (!ValidPriorities.Contains(req.Priority))
            throw new InvalidPriorityException(req.Priority);

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

        return await BuildDetailResponse(story, ct);
    }

    public async Task<object> GetByIdAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);
        return await BuildDetailResponse(story, ct);
    }

    public async Task<object> GetByKeyAsync(string storyKey, CancellationToken ct = default)
    {
        // StoryKey lookup needs org context — use Guid.Empty as placeholder since query filter handles org scoping
        var story = await _storyRepo.GetByKeyAsync(Guid.Empty, storyKey, ct);
        if (story == null) throw new StoryKeyNotFoundException(storyKey);
        return await BuildDetailResponse(story, ct);
    }

    public async Task<object> ListAsync(Guid organizationId, int page, int pageSize, Guid? projectId,
        string? status, string? priority, Guid? departmentId, Guid? assigneeId, Guid? sprintId,
        List<string>? labels, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var (items, totalCount) = await _storyRepo.ListAsync(organizationId, page, pageSize, projectId,
            status, priority, departmentId, assigneeId, sprintId, labels, dateFrom, dateTo, ct);

        var responses = items.Select(s => new StoryListResponse
        {
            StoryId = s.StoryId, StoryKey = s.StoryKey, Title = s.Title,
            Priority = s.Priority, Status = s.Status, StoryPoints = s.StoryPoints,
            DateCreated = s.DateCreated
        }).ToList();

        return new PaginatedResponse<StoryListResponse>
        {
            Data = responses, TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<object> UpdateAsync(Guid storyId, Guid actorId, object request, CancellationToken ct = default)
    {
        var req = (UpdateStoryRequest)request;
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);

        if (req.StoryPoints.HasValue && !FibonacciSet.Contains(req.StoryPoints.Value))
            throw new InvalidStoryPointsException(req.StoryPoints.Value);
        if (req.Priority != null && !ValidPriorities.Contains(req.Priority))
            throw new InvalidPriorityException(req.Priority);

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
        return await BuildDetailResponse(story, ct);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);

        if (story.SprintId.HasValue)
        {
            var sprint = await _sprintRepo.GetByIdAsync(story.SprintId.Value, ct);
            if (sprint?.Status == "Active") throw new StoryInActiveSprintException(storyId);
        }

        story.FlgStatus = "D";
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<object> TransitionStatusAsync(Guid storyId, Guid actorId, string newStatus, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);

        if (!WorkflowStateMachine.IsValidStoryTransition(story.Status, newStatus))
            throw new InvalidStoryTransitionException(story.Status, newStatus);

        // Preconditions
        if (newStatus == "Ready")
        {
            if (string.IsNullOrWhiteSpace(story.Description)) throw new StoryDescriptionRequiredException();
            if (!story.StoryPoints.HasValue || story.StoryPoints.Value <= 0) throw new StoryRequiresPointsException();
        }
        if (newStatus == "InProgress" && !story.AssigneeId.HasValue)
            throw new StoryRequiresAssigneeException();
        if (newStatus == "InReview")
        {
            var taskCount = await _storyRepo.CountTasksAsync(storyId, ct);
            if (taskCount == 0) throw new StoryRequiresTasksException();
            var allDevDone = await _storyRepo.AllDevTasksDoneAsync(storyId, ct);
            if (!allDevDone) throw new StoryRequiresTasksException();
        }
        if (newStatus == "Done")
        {
            var allDone = await _storyRepo.AllTasksDoneAsync(storyId, ct);
            if (!allDone) throw new StoryRequiresTasksException();
        }

        var oldStatus = story.Status;
        story.Status = newStatus;
        if (newStatus == "Done") story.CompletedDate = DateTime.UtcNow;
        story.DateUpdated = DateTime.UtcNow;

        await _storyRepo.UpdateAsync(story, ct);
        await LogActivity(story, "StatusChanged", actorId, oldStatus, newStatus, $"Status changed from {oldStatus} to {newStatus}", ct);
        await _dbContext.SaveChangesAsync(ct);
        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "StoryStatusChanged", EntityType = "Story", EntityId = storyId.ToString(), NotificationType = "StoryStatusChanged" }, ct);

        return await BuildDetailResponse(story, ct);
    }

    public async Task<object> AssignAsync(Guid storyId, Guid actorId, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);

        story.AssigneeId = assigneeId;
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);

        await LogActivity(story, "Assigned", actorId, null, assigneeId.ToString(), $"Story assigned", ct);
        await _dbContext.SaveChangesAsync(ct);
        await _outbox.PublishAsync(new { MessageType = "NotificationRequest", Action = "StoryAssigned", EntityType = "Story", EntityId = storyId.ToString(), NotificationType = "StoryAssigned" }, ct);

        return await BuildDetailResponse(story, ct);
    }

    public async System.Threading.Tasks.Task UnassignAsync(Guid storyId, Guid actorId, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct)
            ?? throw new StoryNotFoundException(storyId);

        var oldAssignee = story.AssigneeId?.ToString();
        story.AssigneeId = null;
        story.DateUpdated = DateTime.UtcNow;
        await _storyRepo.UpdateAsync(story, ct);
        await LogActivity(story, "Unassigned", actorId, oldAssignee, null, "Story unassigned", ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async System.Threading.Tasks.Task CreateLinkAsync(Guid storyId, Guid targetStoryId, string linkType, CancellationToken ct = default)
    {
        var story = await _storyRepo.GetByIdAsync(storyId, ct) ?? throw new StoryNotFoundException(storyId);
        var target = await _storyRepo.GetByIdAsync(targetStoryId, ct) ?? throw new StoryNotFoundException(targetStoryId);

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
    }

    public async System.Threading.Tasks.Task DeleteLinkAsync(Guid storyId, Guid linkId, CancellationToken ct = default)
    {
        var link = await _storyLinkRepo.GetByIdAsync(linkId, ct) ?? throw new NotFoundException("Link", linkId);
        var inverse = await _storyLinkRepo.FindInverseAsync(link.TargetStoryId, link.SourceStoryId, GetInverseLinkType(link.LinkType), ct);

        await _storyLinkRepo.DeleteAsync(link, ct);
        if (inverse != null) await _storyLinkRepo.DeleteAsync(inverse, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async System.Threading.Tasks.Task ApplyLabelAsync(Guid storyId, Guid labelId, CancellationToken ct = default)
    {
        var count = await _storyLabelRepo.CountByStoryAsync(storyId, ct);
        if (count >= 10) throw new MaxLabelsPerStoryException(storyId);

        var existing = await _storyLabelRepo.GetAsync(storyId, labelId, ct);
        if (existing != null) return;

        await _storyLabelRepo.AddAsync(new StoryLabel { StoryId = storyId, LabelId = labelId }, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async System.Threading.Tasks.Task RemoveLabelAsync(Guid storyId, Guid labelId, CancellationToken ct = default)
    {
        var existing = await _storyLabelRepo.GetAsync(storyId, labelId, ct);
        if (existing != null)
        {
            await _storyLabelRepo.DeleteAsync(existing, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
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
            Priority = story.Priority, Status = story.Status,
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

    public async Task<object> BulkUpdateStatusAsync(Guid organizationId, Guid actorId, List<Guid> storyIds, string newStatus, CancellationToken ct = default)
    {
        var results = new List<object>();
        foreach (var storyId in storyIds)
        {
            try
            {
                var result = await TransitionStatusAsync(storyId, actorId, newStatus, ct);
                results.Add(new { StoryId = storyId, Success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { StoryId = storyId, Success = false, Error = ex.Message });
            }
        }
        return new { Updated = results.Count(r => ((dynamic)r).Success), Total = storyIds.Count, Results = results };
    }

    public async Task<object> BulkAssignAsync(Guid organizationId, Guid actorId, List<Guid> storyIds, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default)
    {
        var results = new List<object>();
        foreach (var storyId in storyIds)
        {
            try
            {
                await AssignAsync(storyId, actorId, assigneeId, actorRole, actorDepartmentId, ct);
                results.Add(new { StoryId = storyId, Success = true });
            }
            catch (Exception ex)
            {
                results.Add(new { StoryId = storyId, Success = false, Error = ex.Message });
            }
        }
        return new { Assigned = results.Count(r => ((dynamic)r).Success), Total = storyIds.Count, Results = results };
    }
}
