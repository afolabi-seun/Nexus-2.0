using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
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
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInviteRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var memberId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var departmentId = Guid.Parse(HttpContext.Items["departmentId"]?.ToString()!);
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

        var result = await _inviteService.CreateAsync(orgId, memberId, departmentId, roleName, request, ct);
        return ApiResponse<object>.Ok(result, "Invite created successfully.").ToActionResult(HttpContext, 201);
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
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

        var result = await _inviteService.ListAsync(orgId, departmentId, roleName, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Invites retrieved.").ToActionResult(HttpContext);
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
    public async Task<IActionResult> Validate(
        string token, CancellationToken ct)
    {
        var result = await _inviteService.ValidateTokenAsync(token, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
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
    public async Task<IActionResult> Accept(
        string token, [FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        await _inviteService.AcceptAsync(token, request, ct);
        return ApiResponse<object>.Ok(null!, "Invite accepted successfully.").ToActionResult(HttpContext);
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
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _inviteService.CancelAsync(id, ct);
        return ApiResponse<object>.Ok(null!, "Invite cancelled.").ToActionResult(HttpContext);
    }
}
