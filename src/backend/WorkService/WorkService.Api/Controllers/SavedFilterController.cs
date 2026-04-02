using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.SavedFilters;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.SavedFilters;

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
    public async Task<IActionResult> Create(
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
        return ApiResponse<object>.Ok(response, "Saved filter created.").ToActionResult(HttpContext, 201);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
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
        return ApiResponse<object>.Ok(result, "Saved filters retrieved.").ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var filter = await _savedFilterRepository.GetByIdAsync(id, ct);
        if (filter is null)
        {
            var notFound = new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "FILTER_NOT_FOUND",
                Message = "Saved filter not found."
            };
            return notFound.ToActionResult(HttpContext);
        }

        await _savedFilterRepository.RemoveAsync(filter, ct);
        return ApiResponse<object>.Ok(null!, "Saved filter deleted.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
}
