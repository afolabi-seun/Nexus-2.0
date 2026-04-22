using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Extensions;
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
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        return (await _notificationSettingService.GetSettingsAsync(memberId, ct)).ToActionResult(HttpContext);
    }

    [HttpPut("notification-settings/{typeId:guid}")]
    public async Task<IActionResult> UpdateSetting(
        Guid typeId, [FromBody] UpdateNotificationSettingRequest request, CancellationToken ct)
    {
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        return (await _notificationSettingService.UpdateSettingAsync(memberId, typeId, request, ct)).ToActionResult(HttpContext);
    }

    [HttpGet("notification-types")]
    public async Task<IActionResult> ListTypes(CancellationToken ct)
    {
        return (await _notificationSettingService.ListTypesAsync(ct)).ToActionResult(HttpContext);
    }
}
