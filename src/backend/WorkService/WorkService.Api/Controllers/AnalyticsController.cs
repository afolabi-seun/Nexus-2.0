using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Application.DTOs;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.StoryLinks;
using WorkService.Domain.Interfaces.Services.Analytics;

namespace WorkService.Api.Controllers;

/// <summary>
/// Analytics and reporting endpoints for velocity, resources, cost, health, dependencies, bugs, and dashboard.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IDependencyAnalyzer _dependencyAnalyzer;
    private readonly IStoryLinkRepository _storyLinkRepo;
    private readonly IStoryRepository _storyRepo;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        IDependencyAnalyzer dependencyAnalyzer,
        IStoryLinkRepository storyLinkRepo,
        IStoryRepository storyRepo)
    {
        _analyticsService = analyticsService;
        _dependencyAnalyzer = dependencyAnalyzer;
        _storyLinkRepo = storyLinkRepo;
        _storyRepo = storyRepo;
    }

    [HttpGet("velocity")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetVelocityTrends(
        [FromQuery] Guid projectId, [FromQuery] int sprintCount = 10, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetVelocityTrendsAsync(projectId, sprintCount, ct);
        return Ok(Wrap(result, "Velocity trends retrieved."));
    }

    [HttpGet("resource-management")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetResourceManagement(
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        [FromQuery] Guid? departmentId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _analyticsService.GetResourceManagementAsync(orgId, dateFrom, dateTo, departmentId, ct);
        return Ok(Wrap(result, "Resource management data retrieved."));
    }

    [HttpGet("resource-utilization")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetResourceUtilization(
        [FromQuery] Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetResourceUtilizationAsync(projectId, dateFrom, dateTo, ct);
        return Ok(Wrap(result, "Resource utilization data retrieved."));
    }

    [HttpGet("project-cost")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetProjectCost(
        [FromQuery] Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetProjectCostAnalyticsAsync(projectId, dateFrom, dateTo, ct);
        return Ok(Wrap(result, "Project cost analytics retrieved."));
    }

    [HttpGet("project-health")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetProjectHealth(
        [FromQuery] Guid projectId, [FromQuery] bool history = false, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetProjectHealthAsync(projectId, history, ct);
        return Ok(Wrap(result, "Project health data retrieved."));
    }

    [HttpGet("dependencies")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetDependencies(
        [FromQuery] Guid projectId, [FromQuery] Guid? sprintId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();

        // Fetch all stories for the project
        var (stories, _) = await _storyRepo.ListAsync(orgId, 1, int.MaxValue, projectId,
            null, null, null, null, null, null, null, null, ct);
        var storyList = stories.ToList();

        // Fetch all links for those stories
        var allLinks = new List<Domain.Entities.StoryLink>();
        foreach (var story in storyList)
        {
            var links = await _storyLinkRepo.ListByStoryAsync(story.StoryId, ct);
            allLinks.AddRange(links);
        }

        // Deduplicate links by StoryLinkId
        var uniqueLinks = allLinks.DistinctBy(l => l.StoryLinkId).ToList();

        var result = _dependencyAnalyzer.Analyze(uniqueLinks, storyList, sprintId);
        return Ok(Wrap(result, "Dependency analysis retrieved."));
    }

    [HttpGet("bugs")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetBugMetrics(
        [FromQuery] Guid projectId, [FromQuery] Guid? sprintId = null, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetBugMetricsAsync(projectId, sprintId, ct);
        return Ok(Wrap(result, "Bug metrics retrieved."));
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetDashboard(
        [FromQuery] Guid projectId, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetDashboardAsync(projectId, ct);
        return Ok(Wrap(result, "Dashboard summary retrieved."));
    }

    [HttpGet("snapshot-status")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetSnapshotStatus(CancellationToken ct = default)
    {
        var result = await _analyticsService.GetSnapshotStatusAsync(ct);
        return Ok(Wrap(result, "Snapshot status retrieved."));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => Wrap<object>(data, message);
}
