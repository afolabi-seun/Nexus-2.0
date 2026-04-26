using Microsoft.AspNetCore.Mvc;
using SecurityService.Api.Attributes;
using SecurityService.Api.Extensions;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.ServiceToken;
using SecurityService.Domain.Interfaces.Services.ServiceToken;

namespace SecurityService.Api.Controllers;

[ApiController]
[Route("api/v1/service-tokens")]
public class ServiceTokenController : ControllerBase
{
    private readonly IServiceTokenService _serviceTokenService;

    public ServiceTokenController(IServiceTokenService serviceTokenService)
    {
        _serviceTokenService = serviceTokenService;
    }

    [HttpPost("issue")]
    [ServiceAuth]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> IssueToken(
        [FromBody] ServiceTokenIssueRequest request, CancellationToken ct)
    {
        var result = await _serviceTokenService.IssueTokenAsync(request.ServiceId, request.ServiceName, ct);

        var response = new ServiceTokenResponse
        {
            Token = result.Token,
            ExpiresInSeconds = result.ExpiresInSeconds
        };

        return ApiResponse<ServiceTokenResponse>.Ok(response, "Service token issued.").ToActionResult(HttpContext);
    }
}
