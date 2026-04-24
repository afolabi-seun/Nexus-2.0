using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
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
    public async Task<IActionResult> GetVelocityTrends(
        [FromQuery] Guid projectId, [FromQuery] int sprintCount = 10, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetVelocityTrendsAsync(projectId, sprintCount, ct);
        return ApiResponse<object>.Ok(result, "Velocity trends retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("resource-management")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourceManagement(
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        [FromQuery] Guid? departmentId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _analyticsService.GetResourceManagementAsync(orgId, dateFrom, dateTo, departmentId, ct);
        return ApiResponse<object>.Ok(result, "Resource management data retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("resource-utilization")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourceUtilization(
        [FromQuery] Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetResourceUtilizationAsync(projectId, dateFrom, dateTo, ct);
        return ApiResponse<object>.Ok(result, "Resource utilization data retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("project-cost")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectCost(
        [FromQuery] Guid projectId, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetProjectCostAnalyticsAsync(projectId, dateFrom, dateTo, ct);
        return ApiResponse<object>.Ok(result, "Project cost analytics retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("project-health")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectHealth(
        [FromQuery] Guid projectId, [FromQuery] bool history = false, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetProjectHealthAsync(projectId, history, ct);
        return ApiResponse<object>.Ok(result, "Project health data retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("dependencies")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDependencies(
        [FromQuery] Guid projectId, [FromQuery] Guid? sprintId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();

        // Fetch all stories for the project
        var (stories, _) = await _storyRepo.ListAsync(orgId, 1, int.MaxValue, projectId,
            null, null, null, null, null, null, null, null, null, ct);
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
        return ApiResponse<object>.Ok(result, "Dependency analysis retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("bugs")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBugMetrics(
        [FromQuery] Guid projectId, [FromQuery] Guid? sprintId = null, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetBugMetricsAsync(projectId, sprintId, ct);
        return ApiResponse<object>.Ok(result, "Bug metrics retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] Guid projectId, CancellationToken ct = default)
    {
        var result = await _analyticsService.GetDashboardAsync(projectId, ct);
        return ApiResponse<object>.Ok(result, "Dashboard summary retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("snapshot-status")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshotStatus(CancellationToken ct = default)
    {
        var result = await _analyticsService.GetSnapshotStatusAsync(ct);
        return ApiResponse<object>.Ok(result, "Snapshot status retrieved.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
