using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.TimePolicies;
using WorkService.Domain.Interfaces.Services.TimePolicies;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages organization-level time tracking policies.
/// </summary>
[ApiController]
[Route("api/v1/time-policies")]
[Authorize]
public class TimePolicyController : ControllerBase
{
    private readonly ITimePolicyService _timePolicyService;

    public TimePolicyController(ITimePolicyService timePolicyService)
    {
        _timePolicyService = timePolicyService;
    }

    /// <summary>
    /// Get the organization's time policy.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        return (await _timePolicyService.GetPolicyAsync(orgId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Create or update the organization's time policy (OrgAdmin only).
    /// </summary>
    [HttpPut]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        [FromBody] UpdateTimePolicyRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var userRole = GetRole();
        return (await _timePolicyService.UpsertAsync(orgId, userId, userRole, request, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;
}
