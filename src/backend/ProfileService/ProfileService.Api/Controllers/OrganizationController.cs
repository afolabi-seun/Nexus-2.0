using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Domain.Interfaces.Services.Organizations;

namespace ProfileService.Api.Controllers;

/// <summary>
/// Manages organizations including CRUD, settings, status changes, and admin provisioning.
/// </summary>
[ApiController]
[Route("api/v1/organizations")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// Create a new organization.
    /// </summary>
    /// <param name="request">Organization name and story ID prefix</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created organization</returns>
    /// <response code="201">Organization created — 5 default departments seeded</response>
    /// <response code="409">Organization name or story prefix already exists</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/organizations
    ///     {
    ///         "name": "Acme Corp",
    ///         "storyIdPrefix": "ACME"
    ///     }
    ///
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        var result = await _organizationService.CreateAsync(request, ct);
        var response = Wrap(result, "Organization created successfully.");
        return StatusCode(201, response);
    }

    /// <summary>
    /// List all organizations (PlatformAdmin only).
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of organizations</returns>
    /// <response code="200">Organizations retrieved</response>
    /// <response code="403">Requires PlatformAdmin role</response>
    [HttpGet]
    [PlatformAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _organizationService.ListAllAsync(page, pageSize, ct);
        return Ok(Wrap(result, "Organizations retrieved."));
    }

    /// <summary>
    /// Get organization details by ID.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Organization details including settings and status</returns>
    /// <response code="200">Organization found</response>
    /// <response code="404">Organization not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _organizationService.GetByIdAsync(id, ct);
        return Ok(Wrap(result));
    }

    /// <summary>
    /// Update organization details.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Updated organization data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated organization</returns>
    /// <response code="200">Organization updated</response>
    /// <response code="404">Organization not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        var result = await _organizationService.UpdateAsync(id, request, ct);
        return Ok(Wrap(result, "Organization updated."));
    }

    /// <summary>
    /// Update organization status (activate/suspend/deactivate).
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">New status value</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of status change</returns>
    /// <response code="200">Status updated</response>
    /// <response code="404">Organization not found</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        Guid id, [FromBody] StatusChangeRequest request, CancellationToken ct)
    {
        await _organizationService.UpdateStatusAsync(id, request.Status, ct);
        return Ok(Wrap(null!, "Organization status updated."));
    }

    /// <summary>
    /// Update organization settings (sprint duration, story point scale, etc.).
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Settings to update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated settings</returns>
    /// <response code="200">Settings updated</response>
    /// <response code="404">Organization not found</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/v1/organizations/{id}/settings
    ///     {
    ///         "sprintDuration": 14,
    ///         "storyPointScale": "Fibonacci",
    ///         "defaultBoardView": "Kanban"
    ///     }
    ///
    /// </remarks>
    [HttpPut("{id:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSettings(
        Guid id, [FromBody] OrganizationSettingsRequest request, CancellationToken ct)
    {
        var result = await _organizationService.UpdateSettingsAsync(id, request, ct);
        return Ok(Wrap(result, "Organization settings updated."));
    }

    /// <summary>
    /// Provision an OrgAdmin for an organization (PlatformAdmin only).
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Admin email, first name, and last name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The provisioned team member</returns>
    /// <response code="201">Admin provisioned — credentials generated via SecurityService</response>
    /// <response code="403">Requires PlatformAdmin role</response>
    /// <response code="409">Email already registered</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/organizations/{id}/provision-admin
    ///     {
    ///         "email": "admin@acme.com",
    ///         "firstName": "Jane",
    ///         "lastName": "Admin"
    ///     }
    ///
    /// </remarks>
    [HttpPost("{id:guid}/provision-admin")]
    [PlatformAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> ProvisionAdmin(
        Guid id, [FromBody] ProvisionAdminRequest request, CancellationToken ct)
    {
        var result = await _organizationService.ProvisionAdminAsync(id, request, ct);
        return StatusCode(201, Wrap(result, "Admin provisioned successfully."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
