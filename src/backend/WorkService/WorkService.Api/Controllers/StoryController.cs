using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Labels;
using WorkService.Application.DTOs.Stories;
using WorkService.Domain.Interfaces.Services.ActivityLog;
using WorkService.Domain.Interfaces.Services.Comments;
using WorkService.Domain.Interfaces.Services.Stories;
using WorkService.Application.Helpers;

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
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStoryRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        return (await _storyService.CreateAsync(orgId, userId, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List stories in the current organization with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? projectId = null, [FromQuery] string? status = null,
        [FromQuery] string? priority = null, [FromQuery] string? storyType = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? assigneeId = null, [FromQuery] Guid? sprintId = null,
        [FromQuery] List<string>? labels = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = GetOrganizationId();
        return (await _storyService.ListAsync(orgId, page, pageSize, projectId, status, priority, storyType, departmentId, assigneeId, sprintId, labels, dateFrom, dateTo, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get story details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return (await _storyService.GetByIdAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get story details by story key (e.g., MOB-42).
    /// </summary>
    [HttpGet("by-key/{storyKey}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string storyKey, CancellationToken ct)
    {
        return (await _storyService.GetByKeyAsync(storyKey, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a story.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateStoryRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        return (await _storyService.UpdateAsync(id, userId, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Delete a story.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return (await _storyService.DeleteAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Transition a story's workflow status.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransitionStatus(
        Guid id, [FromBody] StoryStatusRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        return (await _storyService.TransitionStatusAsync(id, userId, request.Status, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Assign a story to a team member.
    /// </summary>
    [HttpPatch("{id:guid}/assign")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid id, [FromBody] StoryAssignRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetRole();
        var deptId = GetDepartmentId();
        return (await _storyService.AssignAsync(id, userId, request.AssigneeId, role, deptId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Unassign a story.
    /// </summary>
    [HttpPatch("{id:guid}/unassign")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unassign(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        return (await _storyService.UnassignAsync(id, userId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Create a link between two stories.
    /// </summary>
    [HttpPost("{id:guid}/links")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLink(
        Guid id, [FromBody] CreateStoryLinkRequest request, CancellationToken ct)
    {
        return (await _storyService.CreateLinkAsync(id, request.TargetStoryId, request.LinkType, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Delete a story link.
    /// </summary>
    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteLink(Guid id, Guid linkId, CancellationToken ct)
    {
        return (await _storyService.DeleteLinkAsync(id, linkId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Apply a label to a story (max 10 labels per story).
    /// </summary>
    [HttpPost("{id:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyLabel(
        Guid id, [FromBody] ApplyLabelRequest request, CancellationToken ct)
    {
        return (await _storyService.ApplyLabelAsync(id, request.LabelId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Remove a label from a story.
    /// </summary>
    [HttpDelete("{id:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveLabel(Guid id, Guid labelId, CancellationToken ct)
    {
        return (await _storyService.RemoveLabelAsync(id, labelId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List comments on a story.
    /// </summary>
    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListComments(Guid id, CancellationToken ct)
    {
        return (await _commentService.ListByEntityAsync("Story", id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List activity log for a story.
    /// </summary>
    [HttpGet("{id:guid}/activity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActivity(Guid id, CancellationToken ct)
    {
        return (await _activityLogService.GetByEntityAsync("Story", id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Organization-wide activity feed.
    /// </summary>
    [HttpGet("/api/v1/activity-feed")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivityFeed(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = GetOrganizationId();
        return (await _activityLogService.GetOrganizationFeedAsync(orgId, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Bulk update story statuses.
    /// </summary>
    [HttpPost("bulk/status")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpdateStatus(
        [FromBody] BulkStatusRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        return (await _storyService.BulkUpdateStatusAsync(orgId, userId, request.StoryIds, request.Status, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Bulk assign stories.
    /// </summary>
    [HttpPost("bulk/assign")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkAssign(
        [FromBody] BulkAssignRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var role = GetRole();
        var deptId = GetDepartmentId();
        return (await _storyService.BulkAssignAsync(orgId, userId, request.StoryIds, request.AssigneeId, role, deptId, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;
    private Guid GetDepartmentId() => Guid.TryParse(HttpContext.Items["departmentId"]?.ToString(), out var id) ? id : Guid.Empty;
}
