using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Preferences;
using ProfileService.Domain.Interfaces.Services.Preferences;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/preferences")]
[Authorize]
public class PreferenceController : ControllerBase
{
    private readonly IPreferenceService _preferenceService;
    private readonly IPreferenceResolver _preferenceResolver;

    public PreferenceController(IPreferenceService preferenceService, IPreferenceResolver preferenceResolver)
    {
        _preferenceService = preferenceService;
        _preferenceResolver = preferenceResolver;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _preferenceService.GetAsync(memberId, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UserPreferencesRequest request, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _preferenceService.UpdateAsync(memberId, request, ct);
        return ApiResponse<object>.Ok(result, "Preferences updated.").ToActionResult(HttpContext);
    }

    [HttpGet("resolved")]
    public async Task<IActionResult> GetResolved(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var departmentId = Guid.Parse(HttpContext.Items["departmentId"]?.ToString()!);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

        var result = await _preferenceResolver.ResolveAsync(memberId, departmentId, orgId, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }
}
