using BillingService.Api.Extensions;
using BillingService.Application.DTOs;
using BillingService.Domain.Interfaces.Services.Plans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Controllers;

/// <summary>
/// Provides read-only access to subscription plan tiers (Free, Starter, Professional, Enterprise).
/// </summary>
[ApiController]
[Route("api/v1/plans")]
[Authorize]
public class PlanController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlanController(IPlanService planService)
    {
        _planService = planService;
    }

    /// <summary>
    /// List all active subscription plans.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active plans with pricing, feature limits, and tier levels</returns>
    /// <response code="200">Plans retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return (await _planService.GetAllActiveAsync(ct)).ToActionResult(HttpContext);
    }
}
