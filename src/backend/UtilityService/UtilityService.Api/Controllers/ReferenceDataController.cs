using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Api.Extensions;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ReferenceData;
using UtilityService.Domain.Interfaces.Services.ReferenceData;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1/reference")]
[Authorize]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ReferenceDataController(IReferenceDataService referenceDataService) => _referenceDataService = referenceDataService;

    [HttpGet("department-types")]
    public async Task<IActionResult> GetDepartmentTypes(CancellationToken ct)
    {
        var result = await _referenceDataService.GetDepartmentTypesAsync(ct);
        return ApiResponse<object>.Ok(result, "Department types retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("priority-levels")]
    public async Task<IActionResult> GetPriorityLevels(CancellationToken ct)
    {
        var result = await _referenceDataService.GetPriorityLevelsAsync(ct);
        return ApiResponse<object>.Ok(result, "Priority levels retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("task-types")]
    public async Task<IActionResult> GetTaskTypes(CancellationToken ct)
    {
        var result = await _referenceDataService.GetTaskTypesAsync(ct);
        return ApiResponse<object>.Ok(result, "Task types retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("workflow-states")]
    public async Task<IActionResult> GetWorkflowStates(CancellationToken ct)
    {
        var result = await _referenceDataService.GetWorkflowStatesAsync(ct);
        return ApiResponse<object>.Ok(result, "Workflow states retrieved.").ToActionResult(HttpContext);
    }

    [HttpPost("department-types")]
    [OrgAdmin]
    public async Task<IActionResult> CreateDepartmentType(
        [FromBody] CreateDepartmentTypeRequest request, CancellationToken ct)
    {
        var result = await _referenceDataService.CreateDepartmentTypeAsync(request, ct);
        return ApiResponse<object>.Ok(result, "Department type created.").ToActionResult(HttpContext, 201);
    }

    [HttpPost("priority-levels")]
    [OrgAdmin]
    public async Task<IActionResult> CreatePriorityLevel(
        [FromBody] CreatePriorityLevelRequest request, CancellationToken ct)
    {
        var result = await _referenceDataService.CreatePriorityLevelAsync(request, ct);
        return ApiResponse<object>.Ok(result, "Priority level created.").ToActionResult(HttpContext, 201);
    }
}
