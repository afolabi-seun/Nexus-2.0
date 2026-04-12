using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Projects;
using WorkService.Domain.Interfaces.Services.CostSnapshots;
using WorkService.Domain.Interfaces.Services.Projects;
using WorkService.Domain.Interfaces.Services.TimeEntries;

using WorkService.Domain.Interfaces.Services.Export;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages projects — organizational containers with unique project keys for story ID generation.
/// </summary>
[ApiController]
[Route("api/v1/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ITimeEntryService _timeEntryService;
    private readonly ICostSnapshotService _costSnapshotService;
    private readonly IExportService _exportService;

    public ProjectController(IProjectService projectService, ITimeEntryService timeEntryService, ICostSnapshotService costSnapshotService, IExportService exportService)
    {
        _projectService = projectService;
        _timeEntryService = timeEntryService;
        _costSnapshotService = costSnapshotService;
        _exportService = exportService;
    }

    /// <summary>
    /// Create a new project.
    /// </summary>
    /// <param name="request">Project name and unique project key (2-10 uppercase alphanumeric)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created project</returns>
    /// <response code="201">Project created</response>
    /// <response code="400">Invalid project key format</response>
    /// <response code="409">Project name or key already exists</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/projects
    ///     {
    ///         "name": "Mobile App",
    ///         "projectKey": "MOB"
    ///     }
    ///
    /// The project key is used as a prefix for story IDs (e.g., MOB-1, MOB-2).
    /// </remarks>
    [HttpPost]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _projectService.CreateAsync(orgId, userId, request, ct);
        return ApiResponse<object>.Ok(result, "Project created successfully.").ToActionResult(HttpContext, 201);
    }

    /// <summary>
    /// List projects in the current organization.
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of projects</returns>
    /// <response code="200">Projects retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _projectService.ListAsync(orgId, page, pageSize, status, ct);
        return ApiResponse<object>.Ok(result, "Projects retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get project details by ID.
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Project details including story count and sprint count</returns>
    /// <response code="200">Project found</response>
    /// <response code="404">Project not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _projectService.GetByIdAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a project.
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="request">Updated project data (project key is immutable)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated project</returns>
    /// <response code="200">Project updated</response>
    /// <response code="404">Project not found</response>
    [HttpPut("{id:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        var result = await _projectService.UpdateAsync(id, request, ct);
        return ApiResponse<object>.Ok(result, "Project updated.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a project's status.
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <param name="request">New status value</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of status change</returns>
    /// <response code="200">Project status updated</response>
    /// <response code="404">Project not found</response>
    [HttpPatch("{id:guid}/status")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] ProjectStatusRequest request, CancellationToken ct)
    {
        await _projectService.UpdateStatusAsync(id, request.Status, ct);
        return ApiResponse<object>.Ok(null!, "Project status updated.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get project cost summary.
    /// </summary>
    [HttpGet("{projectId:guid}/cost-summary")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCostSummary(
        Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var result = await _timeEntryService.GetProjectCostSummaryAsync(projectId, dateFrom, dateTo, ct);
        return ApiResponse<object>.Ok(result, "Project cost summary retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get project resource utilization.
    /// </summary>
    [HttpGet("{projectId:guid}/utilization")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUtilization(
        Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var result = await _timeEntryService.GetProjectUtilizationAsync(projectId, dateFrom, dateTo, ct);
        return ApiResponse<object>.Ok(result, "Resource utilization retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get historical cost snapshots for a project.
    /// </summary>
    [HttpGet("{projectId:guid}/cost-snapshots")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCostSnapshots(
        Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _costSnapshotService.ListByProjectAsync(projectId, dateFrom, dateTo, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Cost snapshots retrieved.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);

    /// <summary>
    /// Export stories as CSV.
    /// </summary>
    [HttpGet("export/stories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportStories(
        [FromQuery] Guid? projectId = null, [FromQuery] Guid? sprintId = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var csv = await _exportService.ExportStoriesCsvAsync(orgId, projectId, sprintId, ct);
        return File(csv, "text/csv", "stories-export.csv");
    }

    /// <summary>
    /// Export time entries as CSV.
    /// </summary>
    [HttpGet("export/time-entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTimeEntries(
        [FromQuery] Guid? projectId = null, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var csv = await _exportService.ExportTimeEntriesCsvAsync(orgId, projectId, dateFrom, dateTo, ct);
        return File(csv, "text/csv", "time-entries-export.csv");
    }
}
