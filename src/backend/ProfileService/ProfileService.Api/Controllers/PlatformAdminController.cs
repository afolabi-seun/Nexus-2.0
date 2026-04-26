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
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetByUsername(
        string username, CancellationToken ct)
    {
        return (await _platformAdminService.GetByUsernameAsync(username, ct)).ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/password")]
    [ServiceAuth]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UpdatePassword(
        Guid id, [FromBody] PlatformAdminPasswordRequest request, CancellationToken ct)
    {
        return (await _platformAdminService.UpdatePasswordAsync(id, request.PasswordHash, ct)).ToActionResult(HttpContext);
    }
}

public class PlatformAdminPasswordRequest
{
    public string PasswordHash { get; set; } = string.Empty;
}
