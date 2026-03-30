using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Interfaces.Services;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/platform-admins")]
[Authorize]
public class PlatformAdminController : ControllerBase
{
    private readonly IPlatformAdminService _platformAdminService;

    public PlatformAdminController(IPlatformAdminService platformAdminService)
    {
        _platformAdminService = platformAdminService;
    }

    [HttpGet("by-username/{username}")]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> GetByUsername(
        string username, CancellationToken ct)
    {
        var result = await _platformAdminService.GetByUsernameAsync(username, ct);
        return Ok(Wrap(result));
    }

    [HttpPatch("{id:guid}/password")]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePassword(
        Guid id, [FromBody] PlatformAdminPasswordRequest request, CancellationToken ct)
    {
        await _platformAdminService.UpdatePasswordAsync(id, request.PasswordHash, ct);
        return Ok(Wrap(null!, "Password updated."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}

public class PlatformAdminPasswordRequest
{
    public string PasswordHash { get; set; } = string.Empty;
}
