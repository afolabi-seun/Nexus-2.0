using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Invites;
using ProfileService.Domain.Interfaces.Services.Invites;

namespace ProfileService.Api.Controllers;

/// <summary>
/// Manages the invitation system for onboarding new team members.
/// </summary>
[ApiController]
[Route("api/v1/invites")]
public class InviteController : ControllerBase
{
    private readonly IInviteService _inviteService;

    public InviteController(IInviteService inviteService)
    {
        _inviteService = inviteService;
    }

    /// <summary>
    /// Create a new invitation for a team member.
    /// </summary>
    /// <param name="request">Invite details including email, name, department, and role</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created invite with token</returns>
    /// <response code="201">Invite created — cryptographic token generated with 48-hour expiry</response>
    /// <response code="409">Email already registered as a member</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/invites
    ///     {
    ///         "email": "new@example.com",
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "departmentId": "guid",
    ///         "roleId": "Member"
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateInviteRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var departmentId = Guid.Parse(HttpContext.Items["departmentId"]?.ToString()!);
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

        var result = await _inviteService.CreateAsync(orgId, memberId, departmentId, roleName, request, ct);
        return StatusCode(201, Wrap(result, "Invite created successfully."));
    }

    /// <summary>
    /// List pending invitations for the current organization.
    /// </summary>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of pending invites</returns>
    /// <response code="200">Invites retrieved</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

        var result = await _inviteService.ListAsync(orgId, departmentId, roleName, page, pageSize, ct);
        return Ok(Wrap(result, "Invites retrieved."));
    }

    /// <summary>
    /// Validate an invite token.
    /// </summary>
    /// <param name="token">Invite token from the invite link</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Invite details (organization name, department, role)</returns>
    /// <response code="200">Token is valid</response>
    /// <response code="410">Token expired or already used</response>
    [HttpGet("{token}/validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<ApiResponse<object>>> Validate(
        string token, CancellationToken ct)
    {
        var result = await _inviteService.ValidateTokenAsync(token, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Accept an invitation and create the team member account.
    /// </summary>
    /// <param name="token">Invite token</param>
    /// <param name="request">Acceptance details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of acceptance</returns>
    /// <response code="200">Invite accepted — team member created with credentials</response>
    /// <response code="409">Email already registered</response>
    /// <response code="410">Token expired or already used</response>
    [HttpPost("{token}/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<ApiResponse<object>>> Accept(
        string token, [FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        await _inviteService.AcceptAsync(token, request, ct);
        return Ok(Wrap(null!, "Invite accepted successfully."));
    }

    /// <summary>
    /// Cancel a pending invitation.
    /// </summary>
    /// <param name="id">Invite ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of cancellation</returns>
    /// <response code="200">Invite cancelled</response>
    /// <response code="404">Invite not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id, CancellationToken ct)
    {
        await _inviteService.CancelAsync(id, ct);
        return Ok(Wrap(null!, "Invite cancelled."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
