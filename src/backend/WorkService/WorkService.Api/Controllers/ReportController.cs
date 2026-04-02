using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Domain.Interfaces.Services.Reports;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("velocity")]
    public async Task<IActionResult> GetVelocityChart(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetVelocityChartAsync(orgId, count, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpGet("department-workload")]
    public async Task<IActionResult> GetDepartmentWorkload(
        [FromQuery] Guid? sprintId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetDepartmentWorkloadAsync(orgId, sprintId, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpGet("capacity")]
    public async Task<IActionResult> GetCapacityUtilization(
        [FromQuery] Guid? departmentId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetCapacityUtilizationAsync(orgId, departmentId, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpGet("cycle-time")]
    public async Task<IActionResult> GetCycleTime(
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetCycleTimeAsync(orgId, dateFrom, dateTo, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpGet("task-completion")]
    public async Task<IActionResult> GetTaskCompletion(
        [FromQuery] Guid? sprintId = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetTaskCompletionAsync(orgId, sprintId, dateFrom, dateTo, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
