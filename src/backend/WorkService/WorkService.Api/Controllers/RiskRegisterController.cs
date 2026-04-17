using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.RiskRegisters;
using WorkService.Domain.Interfaces.Services.RiskRegisters;
using WorkService.Application.Helpers;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages risk register entries for projects.
/// </summary>
[ApiController]
[Route("api/v1/analytics/risks")]
[Authorize]
public class RiskRegisterController : ControllerBase
{
    private readonly IRiskRegisterService _riskService;

    public RiskRegisterController(IRiskRegisterService riskService)
    {
        _riskService = riskService;
    }

    /// <summary>
    /// Create a new risk register entry.
    /// </summary>
    [HttpPost]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRiskRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _riskService.CreateAsync(orgId, userId, request, ct);
        return ApiResponse<object>.Ok(result, "Risk register entry created.").ToActionResult(HttpContext, 201);
    }

    /// <summary>
    /// Update a risk register entry.
    /// </summary>
    [HttpPut("{riskId:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid riskId, [FromBody] UpdateRiskRequest request, CancellationToken ct)
    {
        var result = await _riskService.UpdateAsync(riskId, request, ct);
        return ApiResponse<object>.Ok(result, "Risk register entry updated.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Soft-delete a risk register entry.
    /// </summary>
    [HttpDelete("{riskId:guid}")]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid riskId, CancellationToken ct)
    {
        await _riskService.DeleteAsync(riskId, ct);
        return ApiResponse<object>.Ok(null!, "Risk register entry deleted.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// List risk register entries with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid projectId,
        [FromQuery] Guid? sprintId = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? mitigationStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = GetOrganizationId();
        var result = await _riskService.ListAsync(orgId, projectId, sprintId, severity, mitigationStatus, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Risk register entries retrieved.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
}
