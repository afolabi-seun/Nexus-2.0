using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Application.DTOs;
using WorkService.Domain.Interfaces.Services;

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
    public async Task<ActionResult<ApiResponse<object>>> GetVelocityChart(
        [FromQuery] int count = 10, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetVelocityChartAsync(orgId, count, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("department-workload")]
    public async Task<ActionResult<ApiResponse<object>>> GetDepartmentWorkload(
        [FromQuery] Guid? sprintId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetDepartmentWorkloadAsync(orgId, sprintId, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("capacity")]
    public async Task<ActionResult<ApiResponse<object>>> GetCapacityUtilization(
        [FromQuery] Guid? departmentId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetCapacityUtilizationAsync(orgId, departmentId, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("cycle-time")]
    public async Task<ActionResult<ApiResponse<object>>> GetCycleTime(
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetCycleTimeAsync(orgId, dateFrom, dateTo, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("task-completion")]
    public async Task<ActionResult<ApiResponse<object>>> GetTaskCompletion(
        [FromQuery] Guid? sprintId = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _reportService.GetTaskCompletionAsync(orgId, sprintId, dateFrom, dateTo, ct);
        return Ok(Wrap(result));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
