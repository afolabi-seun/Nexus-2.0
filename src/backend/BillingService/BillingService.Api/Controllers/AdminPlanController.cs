using BillingService.Api.Attributes;
using BillingService.Api.Extensions;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Admin;
using BillingService.Domain.Interfaces.Services.AdminBilling;
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
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return (await _adminPlanService.GetAllPlansAsync(ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Create a new subscription plan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] AdminCreatePlanRequest request, CancellationToken ct)
    {
        return (await _adminPlanService.CreatePlanAsync(request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update an existing plan. PlanCode cannot be changed.
    /// </summary>
    [HttpPut("{planId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid planId,
        [FromBody] AdminUpdatePlanRequest request,
        CancellationToken ct)
    {
        return (await _adminPlanService.UpdatePlanAsync(planId, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Deactivate a plan. Existing subscriptions are not affected.
    /// </summary>
    [HttpPatch("{planId}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid planId, CancellationToken ct)
    {
        var adminId = GetAdminId();
        return (await _adminPlanService.DeactivatePlanAsync(planId, adminId, ct)).ToActionResult(HttpContext);
    }

    private Guid GetAdminId() =>
        Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
}
