using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Interfaces.Services;

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
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="role">Optional role filter</param>
    /// <param name="status">Optional status filter (A=Active, S=Suspended, D=Deactivated)</param>
    /// <param name="availability">Optional availability filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of team members</returns>
    /// <response code="200">Team members retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? departmentId = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? availability = null,
        CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _teamMemberService.ListAsync(orgId, page, pageSize, departmentId, role, status, availability, ct);
        return Ok(Wrap(result, "Team members retrieved."));
    }

    /// <summary>
    /// Get a team member's detailed profile.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Team member details including departments, roles, and professional ID</returns>
    /// <response code="200">Team member found</response>
    /// <response code="404">Team member not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _teamMemberService.GetByIdAsync(id, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Update a team member's profile.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="request">Updated profile data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated team member</returns>
    /// <response code="200">Team member updated</response>
    /// <response code="404">Team member not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/v1/team-members/{id}
    ///     {
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "phoneNumber": "+1234567890"
    ///     }
    ///
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateTeamMemberRequest request, CancellationToken ct)
    {
        var result = await _teamMemberService.UpdateAsync(id, request, ct);
        return Ok(Wrap(result, "Team member updated."));
    }

    /// <summary>
    /// Update a team member's status (activate/suspend/deactivate).
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="request">New status value</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of status change</returns>
    /// <response code="200">Status updated</response>
    /// <response code="400">Cannot deactivate the last OrgAdmin</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        Guid id, [FromBody] StatusChangeRequest request, CancellationToken ct)
    {
        await _teamMemberService.UpdateStatusAsync(id, request.Status, ct);
        return Ok(Wrap(null!, "Team member status updated."));
    }

    /// <summary>
    /// Update a team member's availability status.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="request">New availability status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of availability change</returns>
    /// <response code="200">Availability updated</response>
    [HttpPatch("{id:guid}/availability")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAvailability(
        Guid id, [FromBody] AvailabilityRequest request, CancellationToken ct)
    {
        await _teamMemberService.UpdateAvailabilityAsync(id, request.Availability, ct);
        return Ok(Wrap(null!, "Availability updated."));
    }

    /// <summary>
    /// Add a team member to a department.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="request">Department and role assignment</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of department addition</returns>
    /// <response code="200">Member added to department</response>
    /// <response code="409">Member already in department</response>
    [HttpPost("{id:guid}/departments")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> AddToDepartment(
        Guid id, [FromBody] AddDepartmentRequest request, CancellationToken ct)
    {
        await _teamMemberService.AddToDepartmentAsync(id, request, ct);
        return Ok(Wrap(null!, "Member added to department."));
    }

    /// <summary>
    /// Remove a team member from a department.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="deptId">Department ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of removal</returns>
    /// <response code="200">Member removed from department</response>
    /// <response code="400">Member must belong to at least one department</response>
    [HttpDelete("{id:guid}/departments/{deptId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveFromDepartment(
        Guid id, Guid deptId, CancellationToken ct)
    {
        await _teamMemberService.RemoveFromDepartmentAsync(id, deptId, ct);
        return Ok(Wrap(null!, "Member removed from department."));
    }

    /// <summary>
    /// Change a team member's role within a department.
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="deptId">Department ID</param>
    /// <param name="request">New role assignment</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of role change</returns>
    /// <response code="200">Department role updated</response>
    [HttpPatch("{id:guid}/departments/{deptId:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ChangeDepartmentRole(
        Guid id, Guid deptId, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        await _teamMemberService.ChangeDepartmentRoleAsync(id, deptId, request, ct);
        return Ok(Wrap(null!, "Department role updated."));
    }

    /// <summary>
    /// Get a team member by email (service-to-service).
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Team member internal response</returns>
    /// <response code="200">Team member found</response>
    /// <response code="404">Team member not found</response>
    [HttpGet("by-email/{email}")]
    [ServiceAuth]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetByEmail(
        string email, CancellationToken ct)
    {
        var result = await _teamMemberService.GetByEmailAsync(email, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Update a team member's password hash (service-to-service).
    /// </summary>
    /// <param name="id">Team member ID</param>
    /// <param name="request">New password hash</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of password update</returns>
    /// <response code="200">Password updated</response>
    [HttpPatch("{id:guid}/password")]
    [ServiceAuth]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePassword(
        Guid id, [FromBody] PasswordUpdateRequest request, CancellationToken ct)
    {
        await _teamMemberService.UpdatePasswordAsync(id, request.PasswordHash, ct);
        return Ok(Wrap(null!, "Password updated."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}

public class PasswordUpdateRequest
{
    public string PasswordHash { get; set; } = string.Empty;
}
