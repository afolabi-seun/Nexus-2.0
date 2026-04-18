using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Api.Extensions;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ErrorLogs;
using UtilityService.Domain.Interfaces.Services.ErrorLogs;
using UtilityService.Application.Helpers;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1/error-logs")]
[Authorize]
public class ErrorLogController : ControllerBase
{
    private readonly IErrorLogService _errorLogService;

    public ErrorLogController(IErrorLogService errorLogService) => _errorLogService = errorLogService;

    [HttpPost]
    [ServiceAuth]
    public async Task<IActionResult> Create(
        [FromBody] CreateErrorLogRequest request, CancellationToken ct)
    {
        var result = await _errorLogService.CreateAsync(request, ct);
        return ApiResponse<object>.Ok(result, "Error log created.").ToActionResult(HttpContext, 201);
    }

    [HttpGet]
    [OrgAdmin]
    public async Task<IActionResult> Query(
        [FromQuery] ErrorLogFilterRequest filter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _errorLogService.QueryAsync(orgId, filter, page, pageSize, ct);
        return ApiResponse<object>.Ok(result, "Error logs retrieved.").ToActionResult(HttpContext);
    }
}
