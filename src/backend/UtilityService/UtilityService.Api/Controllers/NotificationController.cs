using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Api.Extensions;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.Notifications;
using UtilityService.Domain.Interfaces.Services.Notifications;
using UtilityService.Application.Helpers;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService) => _notificationService = notificationService;

    [HttpPost("notifications/dispatch")]
    [ServiceAuth]
    public async Task<IActionResult> Dispatch(
        [FromBody] DispatchNotificationRequest request, CancellationToken ct)
    {
        return (await _notificationService.DispatchAsync(request, ct)).ToActionResult();
    }

    [HttpGet("notification-logs")]
    [Authorize]
    public async Task<IActionResult> GetUserHistory(
        [FromQuery] NotificationLogFilterRequest filter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        return (await _notificationService.GetUserHistoryAsync(userId, orgId, filter, page, pageSize, ct)).ToActionResult();
    }
}
