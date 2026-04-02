using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Workflows;
using WorkService.Domain.Interfaces.Services.Workflows;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/workflows")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkflows(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _workflowService.GetWorkflowsAsync(orgId, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpPut("organization")]
    [OrgAdmin]
    public async Task<IActionResult> SaveOrganizationOverride(
        [FromBody] WorkflowOverrideRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        await _workflowService.SaveOrganizationOverrideAsync(orgId, request, ct);
        return ApiResponse<object>.Ok(null!, "Organization workflow override saved.").ToActionResult(HttpContext);
    }

    [HttpPut("department/{departmentId:guid}")]
    [DeptLead]
    public async Task<IActionResult> SaveDepartmentOverride(
        Guid departmentId, [FromBody] WorkflowOverrideRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        await _workflowService.SaveDepartmentOverrideAsync(orgId, departmentId, request, ct);
        return ApiResponse<object>.Ok(null!, "Department workflow override saved.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
