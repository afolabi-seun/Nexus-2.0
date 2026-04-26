using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.CostRates;
using WorkService.Domain.Interfaces.Services.CostRates;
using WorkService.Application.Helpers;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages cost rates — hourly rate definitions at member, role/department, and org-default levels.
/// </summary>
[ApiController]
[Route("api/v1/cost-rates")]
[Authorize]
public class CostRateController : ControllerBase
{
    private readonly ICostRateService _costRateService;

    public CostRateController(ICostRateService costRateService)
    {
        _costRateService = costRateService;
    }

    /// <summary>
    /// Create a cost rate (OrgAdmin only).
    /// </summary>
    [HttpPost]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCostRateRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var userRole = GetRole();
        return (await _costRateService.CreateAsync(orgId, userId, userRole, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List cost rates with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? rateType = null, [FromQuery] Guid? memberId = null,
        [FromQuery] Guid? departmentId = null, [FromQuery] string? roleName = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = GetOrganizationId();
        return (await _costRateService.ListAsync(orgId, rateType, memberId,
            departmentId, roleName, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a cost rate (OrgAdmin only).
    /// </summary>
    [HttpPut("{costRateId:guid}")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid costRateId, [FromBody] UpdateCostRateRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var userRole = GetRole();
        return (await _costRateService.UpdateAsync(costRateId, userId, userRole, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Delete a cost rate (OrgAdmin only).
    /// </summary>
    [HttpDelete("{costRateId:guid}")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid costRateId, CancellationToken ct)
    {
        var userId = GetUserId();
        var userRole = GetRole();
        return (await _costRateService.DeleteAsync(costRateId, userId, userRole, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;
}
