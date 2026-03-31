using BillingService.Api.Attributes;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Admin;
using BillingService.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Controllers;

/// <summary>
/// Platform-wide plan management for PlatformAdmin users.
/// Provides CRUD operations for subscription plans including inactive plans.
/// </summary>
[ApiController]
[Route("api/v1/admin/billing/plans")]
[Authorize]
[PlatformAdmin]
public class AdminPlanController : ControllerBase
{
    private readonly IAdminPlanService _adminPlanService;

    public AdminPlanController(IAdminPlanService adminPlanService)
    {
        _adminPlanService = adminPlanService;
    }

    /// <summary>
    /// List all plans including inactive ones.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(CancellationToken ct)
    {
        var result = await _adminPlanService.GetAllPlansAsync(ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Create a new subscription plan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] AdminCreatePlanRequest request, CancellationToken ct)
    {
        var result = await _adminPlanService.CreatePlanAsync(request, ct);
        return StatusCode(201, Wrap(result, "Plan created."));
    }

    /// <summary>
    /// Update an existing plan. PlanCode cannot be changed.
    /// </summary>
    [HttpPut("{planId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid planId,
        [FromBody] AdminUpdatePlanRequest request,
        CancellationToken ct)
    {
        var result = await _adminPlanService.UpdatePlanAsync(planId, request, ct);
        return Ok(Wrap(result, "Plan updated."));
    }

    /// <summary>
    /// Deactivate a plan. Existing subscriptions are not affected.
    /// </summary>
    [HttpPatch("{planId}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(
        Guid planId, CancellationToken ct)
    {
        var adminId = GetAdminId();
        var result = await _adminPlanService.DeactivatePlanAsync(planId, adminId, ct);
        return Ok(Wrap(result, "Plan deactivated."));
    }

    private Guid GetAdminId() =>
        Guid.Parse(HttpContext.Items["userId"]?.ToString()!);

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
