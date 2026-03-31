using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ErrorCodes;
using UtilityService.Domain.Interfaces.Services.ErrorCodes;

namespace UtilityService.Api.Controllers;

[ApiController]
[Route("api/v1/error-codes")]
public class ErrorCodeController : ControllerBase
{
    private readonly IErrorCodeService _errorCodeService;

    public ErrorCodeController(IErrorCodeService errorCodeService) => _errorCodeService = errorCodeService;

    [HttpPost]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateErrorCodeRequest request, CancellationToken ct)
    {
        var result = await _errorCodeService.CreateAsync(request, ct);
        return StatusCode(201, Wrap(result, "Error code created."));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> List(CancellationToken ct)
    {
        var result = await _errorCodeService.ListAsync(ct);
        return Ok(Wrap(result, "Error codes retrieved."));
    }

    [HttpPut("{code}")]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        string code, [FromBody] UpdateErrorCodeRequest request, CancellationToken ct)
    {
        var result = await _errorCodeService.UpdateAsync(code, request, ct);
        return Ok(Wrap(result, "Error code updated."));
    }

    [HttpDelete("{code}")]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string code, CancellationToken ct)
    {
        await _errorCodeService.DeleteAsync(code, ct);
        return NoContent();
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message,
        CorrelationId = HttpContext.Items["CorrelationId"]?.ToString()
    };
}
