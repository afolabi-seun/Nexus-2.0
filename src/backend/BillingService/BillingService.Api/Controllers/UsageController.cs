using BillingService.Api.Attributes;
using BillingService.Api.Extensions;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Interfaces.Services.Usage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("api/v1/usage")]
[Authorize]
public class UsageController : ControllerBase
{
    private readonly IUsageService _usageService;

    public UsageController(IUsageService usageService)
    {
        _usageService = usageService;
    }

    [HttpGet]
    [OrgAdmin]
    public async Task<IActionResult> GetUsage(CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        return (await _usageService.GetUsageAsync(orgId, ct)).ToActionResult(HttpContext);
    }

    [HttpPost("increment")]
    [ServiceAuth]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Increment(
        [FromBody] IncrementUsageRequest request, CancellationToken ct)
    {
        var orgIdStr = HttpContext.Request.Query["organizationId"].FirstOrDefault()
            ?? HttpContext.Items["organizationId"]?.ToString();

        if (string.IsNullOrEmpty(orgIdStr) || !Guid.TryParse(orgIdStr, out var orgId))
        {
            return ApiResponseExtensions.ToBadRequest("organizationId is required.", HttpContext);
        }

        return (await _usageService.IncrementAsync(orgId, request.MetricName, request.Value, ct)).ToActionResult(HttpContext);
    }
}
