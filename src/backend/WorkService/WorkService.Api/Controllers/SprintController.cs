using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Sprints;
using WorkService.Domain.Interfaces.Services.Sprints;
using WorkService.Domain.Interfaces.Services.TimeEntries;
using WorkService.Application.Helpers;

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
    [HttpPost("projects/{projectId:guid}/sprints")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid projectId, [FromBody] CreateSprintRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        return (await _sprintService.CreateAsync(orgId, projectId, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List sprints in the current organization.
    /// </summary>
    [HttpGet("sprints")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] Guid? projectId = null,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = GetOrganizationId();
        return (await _sprintService.ListAsync(orgId, page, pageSize, status, projectId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get sprint details by ID.
    /// </summary>
    [HttpGet("sprints/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return (await _sprintService.GetByIdAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a sprint (name, goal, dates).
    /// </summary>
    [HttpPut("sprints/{id:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSprintRequest request, CancellationToken ct)
    {
        return (await _sprintService.UpdateAsync(id, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Start a sprint (transition from Planning to Active).
    /// </summary>
    [HttpPatch("sprints/{id:guid}/start")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        return (await _sprintService.StartAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Complete a sprint — calculates velocity and moves incomplete stories to Backlog.
    /// </summary>
    [HttpPatch("sprints/{id:guid}/complete")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        return (await _sprintService.CompleteAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cancel a sprint.
    /// </summary>
    [HttpPatch("sprints/{id:guid}/cancel")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        return (await _sprintService.CancelAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Add a story to a sprint.
    /// </summary>
    [HttpPost("sprints/{sprintId:guid}/stories")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddStory(
        Guid sprintId, [FromBody] AddStoryToSprintRequest request, CancellationToken ct)
    {
        return (await _sprintService.AddStoryAsync(sprintId, request.StoryId, ct)).ToActionResult(HttpContext);
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
        return (await _sprintService.RemoveStoryAsync(sprintId, storyId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get sprint metrics (burndown, completion rate, story points).
    /// </summary>
    [HttpGet("sprints/{id:guid}/metrics")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(Guid id, CancellationToken ct)
    {
        return (await _sprintService.GetMetricsAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get velocity history for the last N completed sprints.
    /// </summary>
    [HttpGet("sprints/velocity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocityHistory(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        return (await _sprintService.GetVelocityHistoryAsync(orgId, count, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get the currently active sprint.
    /// </summary>
    [HttpGet("sprints/active")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSprint(
        [FromQuery] Guid? projectId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        return (await _sprintService.GetActiveSprintAsync(orgId, projectId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get enriched sprint velocity with time tracking data.
    /// </summary>
    [HttpGet("sprints/{sprintId:guid}/velocity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVelocity(Guid sprintId, CancellationToken ct)
    {
        return (await _timeEntryService.GetSprintVelocityAsync(sprintId, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
