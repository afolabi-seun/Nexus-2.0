using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Interfaces.Services.TeamMembers;
using ProfileService.Domain.Results;
using ProfileService.Application.Helpers;

namespace ProfileService.Api.Controllers;

/// <summary>
/// Manages team member profiles, department membership, roles, and availability.
/// </summary>
[ApiController]
[Route("api/v1/team-members")]
[Authorize]
public class TeamMemberController : ControllerBase
{
    private readonly ITeamMemberService _teamMemberService;

    public TeamMemberController(ITeamMemberService teamMemberService)
    {
        _teamMemberService = teamMemberService;
    }

    /// <summary>
    /// List team members in the current organization.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? departmentId = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? availability = null,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        return (await _teamMemberService.ListAsync(orgId, page, pageSize, departmentId, role, status, availability, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Search team members by name, email, or professional ID.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string query = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        return (await _teamMemberService.SearchAsync(orgId, query, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get a team member's detailed profile.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return (await _teamMemberService.GetByIdAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a team member's profile.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateTeamMemberRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

        if (roleName != "OrgAdmin" && roleName != "PlatformAdmin" && id != userId)
        {
            return ServiceResult<object>.Fail(
                ErrorCodes.InsufficientPermissionsValue, ErrorCodes.InsufficientPermissions,
                "You can only update your own profile.", 403).ToActionResult(HttpContext);
        }

        return (await _teamMemberService.UpdateAsync(id, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a team member's status (activate/suspend/deactivate).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] StatusChangeRequest request, CancellationToken ct)
    {
        return (await _teamMemberService.UpdateStatusAsync(id, request.Status, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a team member's availability status.
    /// </summary>
    [HttpPatch("{id:guid}/availability")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAvailability(
        Guid id, [FromBody] AvailabilityRequest request, CancellationToken ct)
    {
        return (await _teamMemberService.UpdateAvailabilityAsync(id, request.Availability, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Add a team member to a department.
    /// </summary>
    [HttpPost("{id:guid}/departments")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToDepartment(
        Guid id, [FromBody] AddDepartmentRequest request, CancellationToken ct)
    {
        return (await _teamMemberService.AddToDepartmentAsync(id, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Remove a team member from a department.
    /// </summary>
    [HttpDelete("{id:guid}/departments/{deptId:guid}")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveFromDepartment(
        Guid id, Guid deptId, CancellationToken ct)
    {
        return (await _teamMemberService.RemoveFromDepartmentAsync(id, deptId, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Change a team member's role within a department.
    /// </summary>
    [HttpPatch("{id:guid}/departments/{deptId:guid}/role")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeDepartmentRole(
        Guid id, Guid deptId, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        return (await _teamMemberService.ChangeDepartmentRoleAsync(id, deptId, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get a team member by email (service-to-service).
    /// </summary>
    [HttpGet("by-email/{email}")]
    [ServiceAuth]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEmail(
        string email, CancellationToken ct)
    {
        return (await _teamMemberService.GetByEmailAsync(email, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update a team member's password hash (service-to-service).
    /// </summary>
    [HttpPatch("{id:guid}/password")]
    [ServiceAuth]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePassword(
        Guid id, [FromBody] PasswordUpdateRequest request, CancellationToken ct)
    {
        return (await _teamMemberService.UpdatePasswordAsync(id, request.PasswordHash, ct)).ToActionResult(HttpContext);
    }
}

public class PasswordUpdateRequest
{
    public string PasswordHash { get; set; } = string.Empty;
}
