using BillingService.Api.Attributes;
using BillingService.Application.DTOs;
using BillingService.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("api/v1/feature-gates")]
[Authorize]
[ServiceAuth]
public class FeatureGateController : ControllerBase
{
    private readonly IFeatureGateService _featureGateService;

    public FeatureGateController(IFeatureGateService featureGateService)
    {
        _featureGateService = featureGateService;
    }

    [HttpGet("{feature}")]
    public async Task<ActionResult<ApiResponse<object>>> CheckFeature(
        string feature, [FromQuery] Guid organizationId, CancellationToken ct)
    {
        var result = await _featureGateService.CheckFeatureAsync(organizationId, feature, ct);
        var response = ApiResponse<object>.Ok(result);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(response);
    }
}
