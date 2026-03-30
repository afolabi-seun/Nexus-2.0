using BillingService.Application.DTOs;
using BillingService.Domain.Interfaces.Services;
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
    public async Task<ActionResult<ApiResponse<object>>> GetAll(CancellationToken ct)
    {
        var result = await _planService.GetAllActiveAsync(ct);
        var response = ApiResponse<object>.Ok(result);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(response);
    }
}
