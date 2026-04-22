using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Invites;
using ProfileService.Domain.Interfaces.Services.Invites;
using ProfileService.Application.Helpers;

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

        return (await _inviteService.CreateAsync(orgId, memberId, departmentId, roleName, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List pending invitations for the current organization.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

        return (await _inviteService.ListAsync(orgId, departmentId, roleName, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Validate an invite token.
    /// </summary>
    [HttpGet("{token}/validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Validate(
        string token, CancellationToken ct)
    {
        return (await _inviteService.ValidateTokenAsync(token, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Accept an invitation and create the team member account.
    /// </summary>
    [HttpPost("{token}/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Accept(
        string token, [FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        return (await _inviteService.AcceptAsync(token, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cancel a pending invitation.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [DeptLead]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        return (await _inviteService.CancelAsync(id, ct)).ToActionResult(HttpContext);
    }
}
