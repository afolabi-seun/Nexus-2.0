using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Attributes;
using UtilityService.Api.Extensions;
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
    public async Task<IActionResult> Create(
        [FromBody] CreateErrorCodeRequest request, CancellationToken ct)
    {
        var result = await _errorCodeService.CreateAsync(request, ct);
        return ApiResponse<object>.Ok(result, "Error code created.").ToActionResult(HttpContext, 201);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _errorCodeService.ListAsync(ct);
        return ApiResponse<object>.Ok(result, "Error codes retrieved.").ToActionResult(HttpContext);
    }

    [HttpPut("{code}")]
    [OrgAdmin]
    public async Task<IActionResult> Update(
        string code, [FromBody] UpdateErrorCodeRequest request, CancellationToken ct)
    {
        var result = await _errorCodeService.UpdateAsync(code, request, ct);
        return ApiResponse<object>.Ok(result, "Error code updated.").ToActionResult(HttpContext);
    }

    [HttpDelete("{code}")]
    [OrgAdmin]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        await _errorCodeService.DeleteAsync(code, ct);
        return NoContent();
    }
}
