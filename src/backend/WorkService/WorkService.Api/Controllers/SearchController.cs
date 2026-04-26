using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs.Search;
using WorkService.Domain.Interfaces.Services.Search;
using WorkService.Application.Helpers;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string query = "", [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = GetOrganizationId();
        var request = new SearchRequest { Query = query, Page = page, PageSize = pageSize };
        return (await _searchService.SearchAsync(orgId, request, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
