using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Interfaces.Services.Devices;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DeviceController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _deviceService.ListAsync(memberId, ct);
        return ApiResponse<object>.Ok(result, "Devices retrieved.").ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _deviceService.SetPrimaryAsync(memberId, id, ct);
        return ApiResponse<object>.Ok(null!, "Primary device updated.").ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _deviceService.RemoveAsync(memberId, id, ct);
        return ApiResponse<object>.Ok(null!, "Device removed.").ToActionResult(HttpContext);
    }
}
