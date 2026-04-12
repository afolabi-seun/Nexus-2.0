using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Departments;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Domain.Interfaces.Services.Departments;

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
    [OrgAdmin]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _departmentService.CreateAsync(orgId, request, ct);
        return ApiResponse<object>.Ok(result, "Department created successfully.").ToActionResult(HttpContext, 201);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _departmentService.ListAsync(orgId, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Departments retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _departmentService.GetByIdAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [DeptLead]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _departmentService.UpdateAsync(id, request, ct);
        return ApiResponse<object>.Ok(result, "Department updated.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/status")]
    [OrgAdmin]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] StatusChangeRequest request, CancellationToken ct)
    {
        await _departmentService.UpdateStatusAsync(id, request.Status, ct);
        return ApiResponse<object>.Ok(null!, "Department status updated.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> ListMembers(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _departmentService.ListMembersAsync(id, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Department members retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}/preferences")]
    public async Task<IActionResult> GetPreferences(Guid id, CancellationToken ct)
    {
        var result = await _departmentService.GetPreferencesAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpPut("{id:guid}/preferences")]
    [DeptLead]
    public async Task<IActionResult> UpdatePreferences(
        Guid id, [FromBody] DepartmentPreferencesRequest request, CancellationToken ct)
    {
        var result = await _departmentService.UpdatePreferencesAsync(id, request, ct);
        return ApiResponse<object>.Ok(result, "Department preferences updated.").ToActionResult(HttpContext);
    }
}
