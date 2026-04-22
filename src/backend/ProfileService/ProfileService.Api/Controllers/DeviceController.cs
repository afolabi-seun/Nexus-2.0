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
        return (await _deviceService.ListAsync(memberId, ct)).ToActionResult(HttpContext);
    }

    [HttpPatch("{id:guid}/primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        return (await _deviceService.SetPrimaryAsync(memberId, id, ct)).ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        return (await _deviceService.RemoveAsync(memberId, id, ct)).ToActionResult(HttpContext);
    }
}
