using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.Session;
using SecurityService.Domain.Interfaces.Services;

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
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SessionResponse>>>> GetSessions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        page = Math.Max(page, 1);

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

        var apiResponse = ApiResponse<PaginatedResponse<SessionResponse>>.Ok(paginated);
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    [HttpDelete("{sessionId}")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeSession(
        string sessionId, CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        await _sessionService.RevokeSessionAsync(userId, sessionId, ct);

        var apiResponse = ApiResponse<object>.Ok(null!, "Session revoked.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }

    [HttpDelete("all")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeAllSessions(CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var currentDeviceId = HttpContext.Items["deviceId"]?.ToString() ?? string.Empty;

        await _sessionService.RevokeAllSessionsExceptCurrentAsync(userId, currentDeviceId, ct);

        var apiResponse = ApiResponse<object>.Ok(null!, "All other sessions revoked.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }
}
