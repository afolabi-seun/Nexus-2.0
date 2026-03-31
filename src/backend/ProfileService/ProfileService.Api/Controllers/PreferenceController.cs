using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _preferenceService.GetAsync(memberId, ct);
        return Ok(Wrap(result));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        [FromBody] UserPreferencesRequest request, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _preferenceService.UpdateAsync(memberId, request, ct);
        return Ok(Wrap(result, "Preferences updated."));
    }

    [HttpGet("resolved")]
    public async Task<ActionResult<ApiResponse<object>>> GetResolved(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var departmentId = Guid.Parse(HttpContext.Items["departmentId"]?.ToString()!);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

        var result = await _preferenceResolver.ResolveAsync(memberId, departmentId, orgId, ct);
        return Ok(Wrap(result));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
