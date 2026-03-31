using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
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
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflows(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _workflowService.GetWorkflowsAsync(orgId, ct);
        return Ok(Wrap(result));
    }

    [HttpPut("organization")]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> SaveOrganizationOverride(
        [FromBody] WorkflowOverrideRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        await _workflowService.SaveOrganizationOverrideAsync(orgId, request, ct);
        return Ok(Wrap<object>(null!, "Organization workflow override saved."));
    }

    [HttpPut("department/{departmentId:guid}")]
    [DeptLead]
    public async Task<ActionResult<ApiResponse<object>>> SaveDepartmentOverride(
        Guid departmentId, [FromBody] WorkflowOverrideRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        await _workflowService.SaveDepartmentOverrideAsync(orgId, departmentId, request, ct);
        return Ok(Wrap<object>(null!, "Department workflow override saved."));
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
