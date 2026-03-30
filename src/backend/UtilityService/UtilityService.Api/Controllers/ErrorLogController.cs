using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ErrorLogs;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1/error-logs")]
public class ErrorLogController : ControllerBase
{
    private readonly IErrorLogService _errorLogService;

    public ErrorLogController(IErrorLogService errorLogService) => _errorLogService = errorLogService;

    [HttpPost]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateErrorLogRequest request, CancellationToken ct)
    {
        var result = await _errorLogService.CreateAsync(request, ct);
        return StatusCode(201, Wrap(result, "Error log created."));
    }

    [HttpGet]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> Query(
        [FromQuery] ErrorLogFilterRequest filter,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var result = await _errorLogService.QueryAsync(orgId, filter, page, pageSize, ct);
        return Ok(Wrap(result, "Error logs retrieved."));
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message,
        CorrelationId = HttpContext.Items["CorrelationId"]?.ToString()
    };
}
