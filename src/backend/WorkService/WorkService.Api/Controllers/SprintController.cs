using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Sprints;
using WorkService.Domain.Interfaces.Services.Sprints;
using WorkService.Domain.Interfaces.Services.TimeEntries;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages sprint lifecycle — creation, planning, start, completion, and velocity tracking.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class SprintController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly ITimeEntryService _timeEntryService;

    public SprintController(ISprintService sprintService, ITimeEntryService timeEntryService)
    {
        _sprintService = sprintService;
        _timeEntryService = timeEntryService;
    }

    /// <summary>
    /// Create a new sprint for a project.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Sprint name, goal, start date, and end date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created sprint in Planning status</returns>
    /// <response code="201">Sprint created</response>
    /// <response code="400">Invalid date range or sprint overlap</response>
    /// <response code="404">Project not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/projects/{projectId}/sprints
    ///     {
    ///         "name": "Sprint 1",
    ///         "goal": "Complete user authentication",
    ///         "startDate": "2026-04-01",
    ///         "endDate": "2026-04-14"
    ///     }
    ///
    /// Only one active sprint per project is allowed.
    /// </remarks>
    [HttpPost("projects/{projectId:guid}/sprints")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid projectId, [FromBody] CreateSprintRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _sprintService.CreateAsync(orgId, projectId, request, ct);
        return ApiResponse<object>.Ok(result, "Sprint created successfully.").ToActionResult(HttpContext, 201);
    }

    /// <summary>
    /// List sprints in the current organization.
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="status">Optional status filter (Planning, Active, Completed, Cancelled)</param>
    /// <param name="projectId">Optional project filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of sprints</returns>
    /// <response code="200">Sprints retrieved</response>
    [HttpGet("sprints")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] Guid? projectId = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _sprintService.ListAsync(orgId, page, pageSize, status, projectId, ct);
        return ApiResponse<object>.Ok(result, "Sprints retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get sprint details by ID.
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Sprint details including stories and metrics</returns>
    /// <response code="200">Sprint found</response>
    /// <response code="404">Sprint not found</response>
    [HttpGet("sprints/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sprintService.GetByIdAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a sprint (name, goal, dates).
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="request">Updated sprint data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated sprint</returns>
    /// <response code="200">Sprint updated</response>
    /// <response code="404">Sprint not found</response>
    [HttpPut("sprints/{id:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSprintRequest request, CancellationToken ct)
    {
        var result = await _sprintService.UpdateAsync(id, request, ct);
        return ApiResponse<object>.Ok(result, "Sprint updated.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Start a sprint (transition from Planning to Active).
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Started sprint</returns>
    /// <response code="200">Sprint started</response>
    /// <response code="400">Sprint not in Planning status or another sprint is already active</response>
    [HttpPatch("sprints/{id:guid}/start")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await _sprintService.StartAsync(id, ct);
        return ApiResponse<object>.Ok(result, "Sprint started.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Complete a sprint — calculates velocity and moves incomplete stories to Backlog.
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completed sprint with velocity and completion rate</returns>
    /// <response code="200">Sprint completed</response>
    /// <response code="400">Sprint is not active</response>
    [HttpPatch("sprints/{id:guid}/complete")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _sprintService.CompleteAsync(id, ct);
        return ApiResponse<object>.Ok(result, "Sprint completed.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cancel a sprint.
    /// </summary>
    /// <param name="id">Sprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cancelled sprint</returns>
    /// <response code="200">Sprint cancelled</response>
    [HttpPatch("sprints/{id:guid}/cancel")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _sprintService.CancelAsync(id, ct);
        return ApiResponse<object>.Ok(result, "Sprint cancelled.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Add a story to a sprint.
    /// </summary>
    /// <param name="sprintId">Sprint ID</param>
    /// <param name="request">Story ID to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation</returns>
    /// <response code="200">Story added to sprint</response>
    /// <response code="400">Sprint not in Planning status or story already in sprint</response>
    [HttpPost("sprints/{sprintId:guid}/stories")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddStory(
        Guid sprintId, [FromBody] AddStoryToSprintRequest request, CancellationToken ct)
    {
        await _sprintService.AddStoryAsync(sprintId, request.StoryId, ct);
        return ApiResponse<object>.Ok(null!, "Story added to sprint.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Remove a story from a sprint.
    /// </summary>
    [HttpDelete("sprints/{sprintId:guid}/stories/{storyId:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveStory(
        Guid sprintId, Guid storyId, CancellationToken ct)
    {
        await _sprintService.RemoveStoryAsync(sprintId, storyId, ct);
        return ApiResponse<object>.Ok(null!, "Story removed from sprint.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get sprint metrics (burndown, completion rate, story points).
    /// </summary>
    [HttpGet("sprints/{id:guid}/metrics")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(Guid id, CancellationToken ct)
    {
        var result = await _sprintService.GetMetricsAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get velocity history for the last N completed sprints.
    /// </summary>
    /// <param name="count">Number of sprints to include (default 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Velocity data points</returns>
    /// <response code="200">Velocity history retrieved</response>
    [HttpGet("sprints/velocity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocityHistory(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _sprintService.GetVelocityHistoryAsync(orgId, count, ct);
        return ApiResponse<object>.Ok(result, "Velocity history retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get the currently active sprint.
    /// </summary>
    /// <param name="projectId">Optional project filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Active sprint or null</returns>
    /// <response code="200">Active sprint found (or null if none)</response>
    [HttpGet("sprints/active")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSprint(
        [FromQuery] Guid? projectId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _sprintService.GetActiveSprintAsync(orgId, projectId, ct);
        return ApiResponse<object>.Ok(result!).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get enriched sprint velocity with time tracking data.
    /// </summary>
    [HttpGet("sprints/{sprintId:guid}/velocity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocity(Guid sprintId, CancellationToken ct)
    {
        var result = await _timeEntryService.GetSprintVelocityAsync(sprintId, ct);
        return ApiResponse<object>.Ok(result, "Sprint velocity retrieved.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
