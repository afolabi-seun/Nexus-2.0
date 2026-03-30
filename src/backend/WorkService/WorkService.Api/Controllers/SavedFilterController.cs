using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.SavedFilters;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/saved-filters")]
[Authorize]
public class SavedFilterController : ControllerBase
{
    private readonly ISavedFilterRepository _savedFilterRepository;

    public SavedFilterController(ISavedFilterRepository savedFilterRepository)
    {
        _savedFilterRepository = savedFilterRepository;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateSavedFilterRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var filter = new SavedFilter
        {
            SavedFilterId = Guid.NewGuid(),
            OrganizationId = orgId,
            TeamMemberId = userId,
            Name = request.Name,
            Filters = request.Filters,
            DateCreated = DateTime.UtcNow
        };
        var result = await _savedFilterRepository.AddAsync(filter, ct);
        var response = new SavedFilterResponse
        {
            SavedFilterId = result.SavedFilterId,
            Name = result.Name,
            Filters = result.Filters,
            DateCreated = result.DateCreated
        };
        return StatusCode(201, Wrap(response, "Saved filter created."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> List(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var filters = await _savedFilterRepository.ListByMemberAsync(orgId, userId, ct);
        var result = filters.Select(f => new SavedFilterResponse
        {
            SavedFilterId = f.SavedFilterId,
            Name = f.Name,
            Filters = f.Filters,
            DateCreated = f.DateCreated
        });
        return Ok(Wrap(result, "Saved filters retrieved."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var filter = await _savedFilterRepository.GetByIdAsync(id, ct);
        if (filter is null)
            return NotFound(Wrap<object>(null!, "Saved filter not found."));

        await _savedFilterRepository.RemoveAsync(filter, ct);
        return Ok(Wrap<object>(null!, "Saved filter deleted."));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => Wrap<object>(data, message);
}
