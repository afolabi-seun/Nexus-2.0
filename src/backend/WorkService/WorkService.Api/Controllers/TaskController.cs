using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
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
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _taskService.CreateAsync(orgId, userId, request, ct);
        return ApiResponse<object>.Ok(result, "Task created successfully.").ToActionResult(HttpContext, 201);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _taskService.GetByIdAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _taskService.UpdateAsync(id, userId, request, ct);
        return ApiResponse<object>.Ok(result, "Task updated.").ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [DeptLead]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteAsync(id, ct);
        return ApiResponse<object>.Ok(null!, "Task deleted.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> TransitionStatus(
        Guid id, [FromBody] TaskStatusRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _taskService.TransitionStatusAsync(id, userId, request.Status, ct);
        return ApiResponse<object>.Ok(result, "Task status updated.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/assign")]
    [DeptLead]
    public async Task<IActionResult> Assign(
        Guid id, [FromBody] TaskAssignRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetRole();
        var deptId = GetDepartmentId();
        var result = await _taskService.AssignAsync(id, userId, request.AssigneeId, role, deptId, ct);
        return ApiResponse<object>.Ok(result, "Task assigned.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/self-assign")]
    public async Task<IActionResult> SelfAssign(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _taskService.SelfAssignAsync(id, userId, ct);
        return ApiResponse<object>.Ok(result, "Task self-assigned.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/unassign")]
    [DeptLead]
    public async Task<IActionResult> Unassign(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _taskService.UnassignAsync(id, userId, ct);
        return ApiResponse<object>.Ok(null!, "Task unassigned.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/log-hours")]
    public async Task<IActionResult> LogHours(
        Guid id, [FromBody] LogHoursRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        await _taskService.LogHoursAsync(id, userId, request.Hours, request.Description, ct);
        return ApiResponse<object>.Ok(null!, "Hours logged.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}/activity")]
    public async Task<IActionResult> ListActivity(Guid id, CancellationToken ct)
    {
        var result = await _activityLogService.GetByEntityAsync("Task", id, ct);
        return ApiResponse<object>.Ok(result, "Activity log retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> ListComments(Guid id, CancellationToken ct)
    {
        var result = await _commentService.ListByEntityAsync("Task", id, ct);
        return ApiResponse<object>.Ok(result, "Comments retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("suggest-assignee")]
    public async Task<IActionResult> SuggestAssignee(
        [FromQuery] string taskType, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _taskService.SuggestAssigneeAsync(taskType, orgId, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;
    private Guid GetDepartmentId() => Guid.TryParse(HttpContext.Items["departmentId"]?.ToString(), out var id) ? id : Guid.Empty;
}
