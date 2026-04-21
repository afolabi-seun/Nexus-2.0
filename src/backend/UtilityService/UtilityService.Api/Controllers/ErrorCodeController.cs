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
    [PlatformAdmin]
    public async Task<IActionResult> Create(
        [FromBody] CreateErrorCodeRequest request, CancellationToken ct)
    {
        return (await _errorCodeService.CreateAsync(request, ct)).ToActionResult();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        return (await _errorCodeService.ListAsync(ct)).ToActionResult();
    }

    [HttpPut("{code}")]
    [PlatformAdmin]
    public async Task<IActionResult> Update(
        string code, [FromBody] UpdateErrorCodeRequest request, CancellationToken ct)
    {
        return (await _errorCodeService.UpdateAsync(code, request, ct)).ToActionResult();
    }

    [HttpDelete("{code}")]
    [PlatformAdmin]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        return (await _errorCodeService.DeleteAsync(code, ct)).ToActionResult();
    }
}
