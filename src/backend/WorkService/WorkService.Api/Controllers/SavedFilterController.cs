using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.SavedFilters;
using WorkService.Domain.Interfaces.Services.SavedFilters;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/saved-filters")]
[Authorize]
public class SavedFilterController : ControllerBase
{
    private readonly ISavedFilterService _savedFilterService;

    public SavedFilterController(ISavedFilterService savedFilterService)
    {
        _savedFilterService = savedFilterService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSavedFilterRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        return (await _savedFilterService.CreateAsync(orgId, userId, request, ct)).ToActionResult(HttpContext);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        return (await _savedFilterService.ListAsync(orgId, userId, ct)).ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return (await _savedFilterService.DeleteAsync(id, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
}
