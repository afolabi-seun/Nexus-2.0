using BillingService.Api.Attributes;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Subscriptions;
using BillingService.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Controllers;

/// <summary>
/// Manages subscription lifecycle — create, upgrade, downgrade, cancel, and view current subscription.
/// </summary>
[ApiController]
[Route("api/v1/subscriptions")]
[Authorize]
[OrgAdmin]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get the current organization's subscription details.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription details including plan info and usage metrics</returns>
    /// <response code="200">Subscription found</response>
    /// <response code="404">No subscription found for this organization</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetCurrent(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _subscriptionService.GetCurrentAsync(orgId, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Create a new subscription for the organization.
    /// </summary>
    /// <param name="request">Plan ID and optional payment method token</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created subscription</returns>
    /// <response code="201">Subscription created — paid plans start with 14-day trial</response>
    /// <response code="404">Plan not found</response>
    /// <response code="409">Organization already has a subscription</response>
    /// <response code="502">Payment provider error</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/subscriptions
    ///     {
    ///         "planId": "guid",
    ///         "paymentMethodToken": null
    ///     }
    ///
    /// Free plan activates immediately. Paid plans start a 14-day trial.
    /// One subscription per organization.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateSubscriptionRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _subscriptionService.CreateAsync(orgId, request, ct);
        return StatusCode(201, Wrap(result, "Subscription created."));
    }

    /// <summary>
    /// Upgrade to a higher-tier plan.
    /// </summary>
    /// <param name="request">Target plan ID (must be higher tier)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated subscription</returns>
    /// <response code="200">Subscription upgraded — prorated charges applied</response>
    /// <response code="400">Invalid upgrade path or no active subscription</response>
    /// <response code="502">Payment provider error</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/v1/subscriptions/upgrade
    ///     {
    ///         "newPlanId": "guid"
    ///     }
    ///
    /// Upgrading during trial ends the trial and starts paid billing immediately.
    /// </remarks>
    [HttpPatch("upgrade")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ApiResponse<object>>> Upgrade(
        [FromBody] UpgradeSubscriptionRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _subscriptionService.UpgradeAsync(orgId, request, ct);
        return Ok(Wrap(result, "Subscription upgraded."));
    }

    /// <summary>
    /// Schedule a downgrade to a lower-tier plan at the end of the billing period.
    /// </summary>
    /// <param name="request">Target plan ID (must be lower tier)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated subscription with scheduled downgrade</returns>
    /// <response code="200">Downgrade scheduled at period end</response>
    /// <response code="400">Invalid downgrade path, no active subscription, or usage exceeds target plan limits</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/v1/subscriptions/downgrade
    ///     {
    ///         "newPlanId": "guid"
    ///     }
    ///
    /// Blocked if current usage exceeds the target plan's limits.
    /// </remarks>
    [HttpPatch("downgrade")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Downgrade(
        [FromBody] DowngradeSubscriptionRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _subscriptionService.DowngradeAsync(orgId, request, ct);
        return Ok(Wrap(result, "Downgrade scheduled."));
    }

    /// <summary>
    /// Cancel the current subscription.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cancelled subscription</returns>
    /// <response code="200">Subscription cancelled — effective at period end</response>
    /// <response code="400">No active subscription or already cancelled</response>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _subscriptionService.CancelAsync(orgId, ct);
        return Ok(Wrap(result, "Subscription cancelled."));
    }

    private Guid GetOrganizationId() =>
        Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
