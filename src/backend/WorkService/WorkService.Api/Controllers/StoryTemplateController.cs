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
/// Templates store default values for title, description, acceptance criteria,
/// priority, story points, labels, and task types. Scoped to organization.
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
    /// <response code="200">Templates retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _service.ListAsync(orgId, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Story templates retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Get a story template by ID.
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Template retrieved</response>
    /// <response code="404">Template not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return ApiResponse<object>.Ok(result, "Story template retrieved.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Create a new story template.
    /// </summary>
    /// <param name="request">Template name and default values</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="201">Template created</response>
    /// <response code="409">Template name already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStoryTemplateRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _service.CreateAsync(orgId, request, ct);
        return ApiResponse<object>.Ok(result, "Story template created.").ToActionResult(HttpContext, 201);
    }

    /// <summary>
    /// Update an existing story template.
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Fields to update (null fields are skipped)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Template updated</response>
    /// <response code="404">Template not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateStoryTemplateRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return ApiResponse<object>.Ok(result, "Story template updated.").ToActionResult(HttpContext);
    }

    /// <summary>
    /// Delete a story template (soft delete).
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Template deleted</response>
    /// <response code="404">Template not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return ApiResponse<object>.Ok(null!, "Story template deleted.").ToActionResult(HttpContext);
    }
}
