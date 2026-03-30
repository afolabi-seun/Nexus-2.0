using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Interfaces.Services;

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
    public async Task<ActionResult<ApiResponse<object>>> List(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _deviceService.ListAsync(memberId, ct);
        return Ok(Wrap(result, "Devices retrieved."));
    }

    [HttpPatch("{id:guid}/primary")]
    public async Task<ActionResult<ApiResponse<object>>> SetPrimary(Guid id, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _deviceService.SetPrimaryAsync(memberId, id, ct);
        return Ok(Wrap(null!, "Primary device updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Remove(Guid id, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _deviceService.RemoveAsync(memberId, id, ct);
        return Ok(Wrap(null!, "Device removed."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
