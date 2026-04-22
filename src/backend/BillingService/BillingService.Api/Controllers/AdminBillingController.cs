using BillingService.Api.Attributes;
using BillingService.Api.Extensions;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Admin;
using BillingService.Domain.Interfaces.Services.AdminBilling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BillingService.Application.Helpers;

namespace BillingService.Api.Controllers;

/// <summary>
/// Platform-wide billing management for PlatformAdmin users.
/// Provides cross-organization subscription visibility, overrides, cancellations, and usage summaries.
/// </summary>
[ApiController]
[Route("api/v1/admin/billing")]
[Authorize]
[PlatformAdmin]
public class AdminBillingController : ControllerBase
{
    private readonly IAdminBillingService _adminBillingService;

    public AdminBillingController(IAdminBillingService adminBillingService)
    {
        _adminBillingService = adminBillingService;
    }

    /// <summary>
    /// List all subscriptions across organizations with optional filtering and pagination.
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        return (await _adminBillingService.GetAllSubscriptionsAsync(status, search, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get detailed billing information for a specific organization.
    /// </summary>
    [HttpGet("organizations/{organizationId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationBilling(
        Guid organizationId, CancellationToken ct)
    {
        return (await _adminBillingService.GetOrganizationBillingAsync(organizationId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Override an organization's subscription to a different plan.
    /// </summary>
    [HttpPost("organizations/{organizationId}/override")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverrideSubscription(
        Guid organizationId,
        [FromBody] AdminOverrideRequest request,
        CancellationToken ct)
    {
        var adminId = GetAdminId();
        return (await _adminBillingService.OverrideSubscriptionAsync(
            organizationId, request.PlanId, request.Reason, adminId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Immediately cancel an organization's subscription.
    /// </summary>
    [HttpPost("organizations/{organizationId}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSubscription(
        Guid organizationId,
        [FromBody] AdminCancelRequest request,
        CancellationToken ct)
    {
        var adminId = GetAdminId();
        return (await _adminBillingService.AdminCancelSubscriptionAsync(
            organizationId, request.Reason, adminId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get platform-wide usage summary across all organizations.
    /// </summary>
    [HttpGet("usage/summary")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageSummary(CancellationToken ct)
    {
        return (await _adminBillingService.GetUsageSummaryAsync(ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get per-organization usage with optional threshold filter and pagination.
    /// </summary>
    [HttpGet("usage/organizations")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganizationUsage(
        [FromQuery] int? threshold,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        return (await _adminBillingService.GetOrganizationUsageListAsync(threshold, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    private Guid GetAdminId() =>
        Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
}
