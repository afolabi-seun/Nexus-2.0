using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Tasks;
using WorkService.Domain.Interfaces.Services.ActivityLog;
using WorkService.Domain.Interfaces.Services.Comments;
using WorkService.Domain.Interfaces.Services.Tasks;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICommentService _commentService;
    private readonly IActivityLogService _activityLogService;

    public TaskController(ITaskService taskService, ICommentService commentService, IActivityLogService activityLogService)
    {
        _taskService = taskService;
        _commentService = commentService;
        _activityLogService = activityLogService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _taskService.CreateAsync(orgId, userId, request, ct);
        return StatusCode(201, Wrap(result, "Task created successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _taskService.GetByIdAsync(id, ct);
        return Ok(Wrap(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _taskService.UpdateAsync(id, userId, request, ct);
        return Ok(Wrap(result, "Task updated."));
    }

    [HttpDelete("{id:guid}")]
    [DeptLead]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteAsync(id, ct);
        return Ok(Wrap<object>(null!, "Task deleted."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> TransitionStatus(
        Guid id, [FromBody] TaskStatusRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _taskService.TransitionStatusAsync(id, userId, request.Status, ct);
        return Ok(Wrap(result, "Task status updated."));
    }

    [HttpPatch("{id:guid}/assign")]
    [DeptLead]
    public async Task<ActionResult<ApiResponse<object>>> Assign(
        Guid id, [FromBody] TaskAssignRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetRole();
        var deptId = GetDepartmentId();
        var result = await _taskService.AssignAsync(id, userId, request.AssigneeId, role, deptId, ct);
        return Ok(Wrap(result, "Task assigned."));
    }

    [HttpPatch("{id:guid}/self-assign")]
    public async Task<ActionResult<ApiResponse<object>>> SelfAssign(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _taskService.SelfAssignAsync(id, userId, ct);
        return Ok(Wrap(result, "Task self-assigned."));
    }

    [HttpPatch("{id:guid}/unassign")]
    [DeptLead]
    public async Task<ActionResult<ApiResponse<object>>> Unassign(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _taskService.UnassignAsync(id, userId, ct);
        return Ok(Wrap<object>(null!, "Task unassigned."));
    }

    [HttpPatch("{id:guid}/log-hours")]
    public async Task<ActionResult<ApiResponse<object>>> LogHours(
        Guid id, [FromBody] LogHoursRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        await _taskService.LogHoursAsync(id, userId, request.Hours, request.Description, ct);
        return Ok(Wrap<object>(null!, "Hours logged."));
    }

    [HttpGet("{id:guid}/activity")]
    public async Task<ActionResult<ApiResponse<object>>> ListActivity(Guid id, CancellationToken ct)
    {
        var result = await _activityLogService.GetByEntityAsync("Task", id, ct);
        return Ok(Wrap(result, "Activity log retrieved."));
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<object>>> ListComments(Guid id, CancellationToken ct)
    {
        var result = await _commentService.ListByEntityAsync("Task", id, ct);
        return Ok(Wrap(result, "Comments retrieved."));
    }

    [HttpGet("suggest-assignee")]
    public async Task<ActionResult<ApiResponse<object>>> SuggestAssignee(
        [FromQuery] string taskType, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _taskService.SuggestAssigneeAsync(taskType, orgId, ct);
        return Ok(Wrap(result));
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
