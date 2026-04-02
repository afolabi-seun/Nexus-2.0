using BillingService.Api.Attributes;
using BillingService.Api.Extensions;
using BillingService.Application.DTOs;
using BillingService.Domain.Interfaces.Services.FeatureGates;
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
    public async Task<IActionResult> CheckFeature(
        string feature, [FromQuery] Guid organizationId, CancellationToken ct)
    {
        var result = await _featureGateService.CheckFeatureAsync(organizationId, feature, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }
}
