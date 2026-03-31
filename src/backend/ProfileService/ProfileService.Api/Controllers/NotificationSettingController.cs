using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.NotificationSettings;
using ProfileService.Domain.Interfaces.Services.NotificationSettings;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class NotificationSettingController : ControllerBase
{
    private readonly INotificationSettingService _notificationSettingService;

    public NotificationSettingController(INotificationSettingService notificationSettingService)
    {
        _notificationSettingService = notificationSettingService;
    }

    [HttpGet("notification-settings")]
    public async Task<ActionResult<ApiResponse<object>>> GetSettings(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var result = await _notificationSettingService.GetSettingsAsync(memberId, ct);
        return Ok(Wrap(result, "Notification settings retrieved."));
    }

    [HttpPut("notification-settings/{typeId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSetting(
        Guid typeId, [FromBody] UpdateNotificationSettingRequest request, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _notificationSettingService.UpdateSettingAsync(memberId, typeId, request, ct);
        return Ok(Wrap(null!, "Notification setting updated."));
    }

    [HttpGet("notification-types")]
    public async Task<ActionResult<ApiResponse<object>>> ListTypes(CancellationToken ct)
    {
        var result = await _notificationSettingService.ListTypesAsync(ct);
        return Ok(Wrap(result, "Notification types retrieved."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
