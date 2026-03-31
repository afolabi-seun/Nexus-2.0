using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
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
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _timePolicyService.GetPolicyAsync(orgId, ct);
        return Ok(Wrap(result, "Time policy retrieved."));
    }

    /// <summary>
    /// Create or update the organization's time policy (OrgAdmin only).
    /// </summary>
    [HttpPut]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Upsert(
        [FromBody] UpdateTimePolicyRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var userRole = GetRole();
        var result = await _timePolicyService.UpsertAsync(orgId, userId, userRole, request, ct);
        return Ok(Wrap(result, "Time policy updated."));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => Wrap<object>(data, message);
}
