using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Departments;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Domain.Interfaces.Services;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _departmentService.CreateAsync(orgId, request, ct);
        return StatusCode(201, Wrap(result, "Department created successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _departmentService.ListAsync(orgId, page, pageSize, ct);
        return Ok(Wrap(result, "Departments retrieved."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _departmentService.GetByIdAsync(id, ct);
        return Ok(Wrap(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _departmentService.UpdateAsync(id, request, ct);
        return Ok(Wrap(result, "Department updated."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        Guid id, [FromBody] StatusChangeRequest request, CancellationToken ct)
    {
        await _departmentService.UpdateStatusAsync(id, request.Status, ct);
        return Ok(Wrap(null!, "Department status updated."));
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<object>>> ListMembers(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _departmentService.ListMembersAsync(id, page, pageSize, ct);
        return Ok(Wrap(result, "Department members retrieved."));
    }

    [HttpGet("{id:guid}/preferences")]
    public async Task<ActionResult<ApiResponse<object>>> GetPreferences(Guid id, CancellationToken ct)
    {
        var result = await _departmentService.GetPreferencesAsync(id, ct);
        return Ok(Wrap(result));
    }

    [HttpPut("{id:guid}/preferences")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePreferences(
        Guid id, [FromBody] DepartmentPreferencesRequest request, CancellationToken ct)
    {
        var result = await _departmentService.UpdatePreferencesAsync(id, request, ct);
        return Ok(Wrap(result, "Department preferences updated."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
