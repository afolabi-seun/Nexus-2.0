using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Labels;
using WorkService.Application.DTOs.Stories;
using WorkService.Domain.Interfaces.Services;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages stories — the core work items with professional IDs, workflow state machine, and assignment.
/// </summary>
[ApiController]
[Route("api/v1/stories")]
[Authorize]
public class StoryController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly ICommentService _commentService;
    private readonly IActivityLogService _activityLogService;

    public StoryController(IStoryService storyService, ICommentService commentService, IActivityLogService activityLogService)
    {
        _storyService = storyService;
        _commentService = commentService;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Create a new story in a project.
    /// </summary>
    /// <param name="request">Story details including project, title, priority, and story points</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created story with generated professional ID</returns>
    /// <response code="201">Story created with auto-generated key (e.g., MOB-42)</response>
    /// <response code="400">Validation error</response>
    /// <response code="404">Project not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/stories
    ///     {
    ///         "projectId": "guid",
    ///         "title": "User login flow",
    ///         "description": "Implement the user login flow",
    ///         "priority": "High",
    ///         "storyPoints": 5
    ///     }
    ///
    /// Story starts in Backlog status. Professional ID is generated atomically per project.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateStoryRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _storyService.CreateAsync(orgId, userId, request, ct);
        return StatusCode(201, Wrap(result, "Story created successfully."));
    }

    /// <summary>
    /// List stories in the current organization with optional filters.
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="projectId">Optional project filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="priority">Optional priority filter</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="assigneeId">Optional assignee filter</param>
    /// <param name="sprintId">Optional sprint filter</param>
    /// <param name="labels">Optional label filter</param>
    /// <param name="dateFrom">Optional date range start</param>
    /// <param name="dateTo">Optional date range end</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of stories</returns>
    /// <response code="200">Stories retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? projectId = null, [FromQuery] string? status = null,
        [FromQuery] string? priority = null, [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? assigneeId = null, [FromQuery] Guid? sprintId = null,
        [FromQuery] List<string>? labels = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _storyService.ListAsync(orgId, page, pageSize, projectId, status, priority, departmentId, assigneeId, sprintId, labels, dateFrom, dateTo, ct);
        return Ok(Wrap(result, "Stories retrieved."));
    }

    /// <summary>
    /// Get story details by ID.
    /// </summary>
    /// <param name="id">Story ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Story details including tasks, labels, links, and assignee</returns>
    /// <response code="200">Story found</response>
    /// <response code="404">Story not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _storyService.GetByIdAsync(id, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Get story details by story key (e.g., MOB-42).
    /// </summary>
    /// <param name="storyKey">Story key (e.g., MOB-42)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Story details</returns>
    /// <response code="200">Story found</response>
    /// <response code="404">Story not found</response>
    [HttpGet("by-key/{storyKey}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetByKey(string storyKey, CancellationToken ct)
    {
        var result = await _storyService.GetByKeyAsync(storyKey, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Update a story.
    /// </summary>
    /// <param name="id">Story ID</param>
    /// <param name="request">Updated story data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated story</returns>
    /// <response code="200">Story updated</response>
    /// <response code="404">Story not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateStoryRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _storyService.UpdateAsync(id, userId, request, ct);
        return Ok(Wrap(result, "Story updated."));
    }

    /// <summary>
    /// Delete a story.
    /// </summary>
    /// <param name="id">Story ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="200">Story deleted</response>
    /// <response code="400">Story is in an active sprint</response>
    /// <response code="404">Story not found</response>
    [HttpDelete("{id:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _storyService.DeleteAsync(id, ct);
        return Ok(Wrap<object>(null!, "Story deleted."));
    }

    /// <summary>
    /// Transition a story's workflow status.
    /// </summary>
    /// <param name="id">Story ID</param>
    /// <param name="request">Target status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated story with new status</returns>
    /// <response code="200">Status transitioned</response>
    /// <response code="400">Invalid status transition</response>
    /// <remarks>
    /// Valid transitions: Backlog → Ready → InProgress → InReview → QA → Done → Closed.
    ///
    ///     PATCH /api/v1/stories/{id}/status
    ///     {
    ///         "status": "InProgress"
    ///     }
    ///
    /// </remarks>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> TransitionStatus(
        Guid id, [FromBody] StoryStatusRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _storyService.TransitionStatusAsync(id, userId, request.Status, ct);
        return Ok(Wrap(result, "Story status updated."));
    }

    /// <summary>
    /// Assign a story to a team member.
    /// </summary>
    /// <param name="id">Story ID</param>
    /// <param name="request">Assignee ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated story with assignee</returns>
    /// <response code="200">Story assigned</response>
    /// <response code="404">Story or assignee not found</response>
    [HttpPatch("{id:guid}/assign")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Assign(
        Guid id, [FromBody] StoryAssignRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetRole();
        var deptId = GetDepartmentId();
        var result = await _storyService.AssignAsync(id, userId, request.AssigneeId, role, deptId, ct);
        return Ok(Wrap(result, "Story assigned."));
    }

    /// <summary>
    /// Unassign a story.
    /// </summary>
    [HttpPatch("{id:guid}/unassign")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Unassign(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _storyService.UnassignAsync(id, userId, ct);
        return Ok(Wrap<object>(null!, "Story unassigned."));
    }

    /// <summary>
    /// Create a link between two stories.
    /// </summary>
    [HttpPost("{id:guid}/links")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<object>>> CreateLink(
        Guid id, [FromBody] CreateStoryLinkRequest request, CancellationToken ct)
    {
        await _storyService.CreateLinkAsync(id, request.TargetStoryId, request.LinkType, ct);
        return StatusCode(201, Wrap<object>(null!, "Story link created."));
    }

    /// <summary>
    /// Delete a story link.
    /// </summary>
    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLink(Guid id, Guid linkId, CancellationToken ct)
    {
        await _storyService.DeleteLinkAsync(id, linkId, ct);
        return Ok(Wrap<object>(null!, "Story link deleted."));
    }

    /// <summary>
    /// Apply a label to a story (max 10 labels per story).
    /// </summary>
    [HttpPost("{id:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ApplyLabel(
        Guid id, [FromBody] ApplyLabelRequest request, CancellationToken ct)
    {
        await _storyService.ApplyLabelAsync(id, request.LabelId, ct);
        return Ok(Wrap<object>(null!, "Label applied."));
    }

    /// <summary>
    /// Remove a label from a story.
    /// </summary>
    [HttpDelete("{id:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveLabel(Guid id, Guid labelId, CancellationToken ct)
    {
        await _storyService.RemoveLabelAsync(id, labelId, ct);
        return Ok(Wrap<object>(null!, "Label removed."));
    }

    /// <summary>
    /// List comments on a story.
    /// </summary>
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListComments(Guid id, CancellationToken ct)
    {
        var result = await _commentService.ListByEntityAsync("Story", id, ct);
        return Ok(Wrap(result, "Comments retrieved."));
    }

    /// <summary>
    /// List activity log for a story.
    /// </summary>
    [HttpGet("{id:guid}/activity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListActivity(Guid id, CancellationToken ct)
    {
        var result = await _activityLogService.GetByEntityAsync("Story", id, ct);
        return Ok(Wrap(result, "Activity log retrieved."));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;
    private Guid GetDepartmentId() => Guid.TryParse(HttpContext.Items["departmentId"]?.ToString(), out var id) ? id : Guid.Empty;

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => Wrap<object>(data, message);
}
