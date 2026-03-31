using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.TimeEntries;
using WorkService.Domain.Interfaces.Services.TimeEntries;
using WorkService.Domain.Interfaces.Services.TimerSessions;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages time entries — manual logging, timer start/stop, approval workflows.
/// </summary>
[ApiController]
[Route("api/v1/time-entries")]
[Authorize]
public class TimeEntryController : ControllerBase
{
    private readonly ITimeEntryService _timeEntryService;
    private readonly ITimerSessionService _timerSessionService;

    public TimeEntryController(ITimeEntryService timeEntryService, ITimerSessionService timerSessionService)
    {
        _timeEntryService = timeEntryService;
        _timerSessionService = timerSessionService;
    }

    /// <summary>
    /// Create a manual time entry.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateTimeEntryRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _timeEntryService.CreateAsync(orgId, userId, request, ct);
        return StatusCode(201, Wrap(result, "Time entry created successfully."));
    }

    /// <summary>
    /// List and filter time entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] Guid? storyId = null, [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? sprintId = null, [FromQuery] Guid? memberId = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool? isBillable = null, [FromQuery] string? status = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _timeEntryService.ListAsync(orgId, storyId, projectId, sprintId,
            memberId, dateFrom, dateTo, isBillable, status, page, pageSize, ct);
        return Ok(Wrap(result, "Time entries retrieved."));
    }

    /// <summary>
    /// Update a time entry.
    /// </summary>
    [HttpPut("{timeEntryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid timeEntryId, [FromBody] UpdateTimeEntryRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _timeEntryService.UpdateAsync(timeEntryId, userId, request, ct);
        return Ok(Wrap(result, "Time entry updated."));
    }

    /// <summary>
    /// Delete a time entry (soft-delete).
    /// </summary>
    [HttpDelete("{timeEntryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid timeEntryId, CancellationToken ct)
    {
        var userId = GetUserId();
        await _timeEntryService.DeleteAsync(timeEntryId, userId, ct);
        return Ok(Wrap<object>(null!, "Time entry deleted."));
    }

    /// <summary>
    /// Approve a time entry.
    /// </summary>
    [HttpPost("{timeEntryId:guid}/approve")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> Approve(Guid timeEntryId, CancellationToken ct)
    {
        var approverId = GetUserId();
        var approverRole = GetRole();
        var approverDeptId = GetDepartmentId();
        var result = await _timeEntryService.ApproveAsync(timeEntryId, approverId, approverRole, approverDeptId, ct);
        return Ok(Wrap(result, "Time entry approved."));
    }

    /// <summary>
    /// Reject a time entry.
    /// </summary>
    [HttpPost("{timeEntryId:guid}/reject")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> Reject(
        Guid timeEntryId, [FromBody] RejectTimeEntryRequest request, CancellationToken ct)
    {
        var approverId = GetUserId();
        var approverRole = GetRole();
        var approverDeptId = GetDepartmentId();
        var result = await _timeEntryService.RejectAsync(timeEntryId, approverId, approverRole, approverDeptId, request.Reason, ct);
        return Ok(Wrap(result, "Time entry rejected."));
    }

    /// <summary>
    /// Start a timer for a story.
    /// </summary>
    [HttpPost("timer/start")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> StartTimer(
        [FromBody] TimerStartRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var orgId = GetOrganizationId();
        var result = await _timerSessionService.StartAsync(userId, request.StoryId, orgId, ct);
        return Ok(Wrap(result, "Timer started."));
    }

    /// <summary>
    /// Stop the active timer.
    /// </summary>
    [HttpPost("timer/stop")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> StopTimer(CancellationToken ct)
    {
        var userId = GetUserId();
        var orgId = GetOrganizationId();
        var result = await _timerSessionService.StopAsync(userId, orgId, ct);
        return Ok(Wrap(result, "Timer stopped."));
    }

    /// <summary>
    /// Get active timer status.
    /// </summary>
    [HttpGet("timer/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ApiResponse<object>>> GetTimerStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _timerSessionService.GetStatusAsync(userId, ct);
        if (result == null)
            return NoContent();
        return Ok(Wrap(result, "Timer status retrieved."));
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
