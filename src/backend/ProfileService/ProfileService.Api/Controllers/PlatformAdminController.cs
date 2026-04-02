using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Interfaces.Services.PlatformAdmins;

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
    public async Task<IActionResult> GetByUsername(
        string username, CancellationToken ct)
    {
        var result = await _platformAdminService.GetByUsernameAsync(username, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/password")]
    [ServiceAuth]
    public async Task<IActionResult> UpdatePassword(
        Guid id, [FromBody] PlatformAdminPasswordRequest request, CancellationToken ct)
    {
        await _platformAdminService.UpdatePasswordAsync(id, request.PasswordHash, ct);
        return ApiResponse<object>.Ok(null!, "Password updated.").ToActionResult(HttpContext);
    }
}

public class PlatformAdminPasswordRequest
{
    public string PasswordHash { get; set; } = string.Empty;
}
