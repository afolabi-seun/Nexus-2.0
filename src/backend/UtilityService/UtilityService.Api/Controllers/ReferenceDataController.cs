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
        return (await _referenceDataService.GetDepartmentTypesAsync(ct)).ToActionResult();
    }

    [HttpGet("priority-levels")]
    public async Task<IActionResult> GetPriorityLevels(CancellationToken ct)
    {
        return (await _referenceDataService.GetPriorityLevelsAsync(ct)).ToActionResult();
    }

    [HttpGet("task-types")]
    public async Task<IActionResult> GetTaskTypes(CancellationToken ct)
    {
        return (await _referenceDataService.GetTaskTypesAsync(ct)).ToActionResult();
    }

    [HttpGet("workflow-states")]
    public async Task<IActionResult> GetWorkflowStates(CancellationToken ct)
    {
        return (await _referenceDataService.GetWorkflowStatesAsync(ct)).ToActionResult();
    }

    [HttpPost("department-types")]
    [OrgAdmin]
    public async Task<IActionResult> CreateDepartmentType(
        [FromBody] CreateDepartmentTypeRequest request, CancellationToken ct)
    {
        return (await _referenceDataService.CreateDepartmentTypeAsync(request, ct)).ToActionResult();
    }

    [HttpPost("priority-levels")]
    [OrgAdmin]
    public async Task<IActionResult> CreatePriorityLevel(
        [FromBody] CreatePriorityLevelRequest request, CancellationToken ct)
    {
        return (await _referenceDataService.CreatePriorityLevelAsync(request, ct)).ToActionResult();
    }
}
