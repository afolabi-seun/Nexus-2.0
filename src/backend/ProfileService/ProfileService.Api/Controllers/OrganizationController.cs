using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Domain.Interfaces.Services.Organizations;
using ProfileService.Application.Helpers;


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
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        return (await _organizationService.CreateAsync(request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// List all organizations (PlatformAdmin only).
    /// </summary>
    [HttpGet]
    [PlatformAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        return (await _organizationService.ListAllAsync(page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get organization details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return (await _organizationService.GetByIdAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update organization details.
    /// </summary>
    [HttpPut("{id:guid}")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        return (await _organizationService.UpdateAsync(id, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update organization status (activate/suspend/deactivate).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] StatusChangeRequest request, CancellationToken ct)
    {
        return (await _organizationService.UpdateStatusAsync(id, request.Status, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update organization settings (sprint duration, story point scale, etc.).
    /// </summary>
    [HttpPut("{id:guid}/settings")]
    [OrgAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(
        Guid id, [FromBody] OrganizationSettingsRequest request, CancellationToken ct)
    {
        return (await _organizationService.UpdateSettingsAsync(id, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Provision an OrgAdmin for an organization (PlatformAdmin only).
    /// </summary>
    [HttpPost("{id:guid}/provision-admin")]
    [PlatformAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProvisionAdmin(
        Guid id, [FromBody] ProvisionAdminRequest request, CancellationToken ct)
    {
        return (await _organizationService.ProvisionAdminAsync(id, request, ct)).ToActionResult(HttpContext);
    }
}
