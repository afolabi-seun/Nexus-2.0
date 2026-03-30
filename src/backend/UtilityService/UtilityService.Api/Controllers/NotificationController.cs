using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.Notifications;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService) => _notificationService = notificationService;

    [HttpPost("notifications/dispatch")]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> Dispatch(
        [FromBody] DispatchNotificationRequest request, CancellationToken ct)
    {
        await _notificationService.DispatchAsync(request, ct);
        return Ok(Wrap(null!, "Notification dispatched."));
    }

    [HttpGet("notification-logs")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetUserHistory(
        [FromQuery] NotificationLogFilterRequest filter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _notificationService.GetUserHistoryAsync(userId, orgId, filter, page, pageSize, ct);
        return Ok(Wrap(result, "Notification logs retrieved."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message,
        CorrelationId = HttpContext.Items["CorrelationId"]?.ToString()
    };
}
