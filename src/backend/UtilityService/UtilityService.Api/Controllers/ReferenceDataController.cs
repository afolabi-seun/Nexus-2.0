using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ReferenceData;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1/reference")]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ReferenceDataController(IReferenceDataService referenceDataService) => _referenceDataService = referenceDataService;

    [HttpGet("department-types")]
    public async Task<ActionResult<ApiResponse<object>>> GetDepartmentTypes(CancellationToken ct)
    {
        var result = await _referenceDataService.GetDepartmentTypesAsync(ct);
        return Ok(Wrap(result, "Department types retrieved."));
    }

    [HttpGet("priority-levels")]
    public async Task<ActionResult<ApiResponse<object>>> GetPriorityLevels(CancellationToken ct)
    {
        var result = await _referenceDataService.GetPriorityLevelsAsync(ct);
        return Ok(Wrap(result, "Priority levels retrieved."));
    }

    [HttpGet("task-types")]
    public async Task<ActionResult<ApiResponse<object>>> GetTaskTypes(CancellationToken ct)
    {
        var result = await _referenceDataService.GetTaskTypesAsync(ct);
        return Ok(Wrap(result, "Task types retrieved."));
    }

    [HttpGet("workflow-states")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowStates(CancellationToken ct)
    {
        var result = await _referenceDataService.GetWorkflowStatesAsync(ct);
        return Ok(Wrap(result, "Workflow states retrieved."));
    }

    [HttpPost("department-types")]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> CreateDepartmentType(
        [FromBody] CreateDepartmentTypeRequest request, CancellationToken ct)
    {
        var result = await _referenceDataService.CreateDepartmentTypeAsync(request, ct);
        return StatusCode(201, Wrap(result, "Department type created."));
    }

    [HttpPost("priority-levels")]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> CreatePriorityLevel(
        [FromBody] CreatePriorityLevelRequest request, CancellationToken ct)
    {
        var result = await _referenceDataService.CreatePriorityLevelAsync(request, ct);
        return StatusCode(201, Wrap(result, "Priority level created."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message,
        CorrelationId = HttpContext.Items["CorrelationId"]?.ToString()
    };
}
