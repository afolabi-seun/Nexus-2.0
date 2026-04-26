using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.StoryTemplates;
using WorkService.Application.Helpers;
using WorkService.Domain.Interfaces.Services.StoryTemplates;

namespace WorkService.Api.Controllers;

/// <summary>
/// Manages reusable story templates for pre-filling story creation forms.
/// </summary>
[ApiController]
[Route("api/v1/story-templates")]
[Authorize]
public class StoryTemplateController : ControllerBase
{
    private readonly IStoryTemplateService _service;

    public StoryTemplateController(IStoryTemplateService service)
    {
        _service = service;
    }

    /// <summary>
    /// List story templates for the current organization.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        return (await _service.ListAsync(orgId, page, pageSize, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get a story template by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return (await _service.GetByIdAsync(id, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Create a new story template.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStoryTemplateRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        return (await _service.CreateAsync(orgId, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Update an existing story template.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateStoryTemplateRequest request, CancellationToken ct)
    {
        return (await _service.UpdateAsync(id, request, ct)).ToActionResult(HttpContext);
    }

    /// <summary>
    /// Delete a story template (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return (await _service.DeleteAsync(id, ct)).ToActionResult(HttpContext);
    }
}
