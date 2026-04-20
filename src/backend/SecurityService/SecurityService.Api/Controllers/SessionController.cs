using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityService.Api.Extensions;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.Session;
using SecurityService.Domain.Interfaces.Services.Session;
using SecurityService.Application.Helpers;

namespace SecurityService.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var sessions = await _sessionService.GetSessionsAsync(userId, page, pageSize, ct);

        var sessionList = sessions.Select(s => new SessionResponse
        {
            SessionId = s.SessionId,
            DeviceId = s.DeviceId,
            IpAddress = s.IpAddress,
            CreatedAt = s.CreatedAt
        }).ToList();

        var paginated = new PaginatedResponse<SessionResponse>
        {
            Data = sessionList,
            Page = page,
            PageSize = pageSize,
            TotalCount = sessionList.Count,
            TotalPages = 1
        };

        return ApiResponse<PaginatedResponse<SessionResponse>>.Ok(paginated, "Sessions retrieved.").ToActionResult(HttpContext);
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> RevokeSession(
        string sessionId, CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _sessionService.RevokeSessionAsync(userId, sessionId, ct);

        return ApiResponse<object>.Ok(null!, "Session revoked.").ToActionResult(HttpContext);
    }

    [HttpDelete("all")]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var currentDeviceId = HttpContext.Items["deviceId"]?.ToString() ?? string.Empty;

        await _sessionService.RevokeAllSessionsExceptCurrentAsync(userId, currentDeviceId, ct);

        return ApiResponse<object>.Ok(null!, "All other sessions revoked.").ToActionResult(HttpContext);
    }
}
