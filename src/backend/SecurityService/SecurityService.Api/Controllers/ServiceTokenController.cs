using Microsoft.AspNetCore.Mvc;
using SecurityService.Api.Attributes;
using SecurityService.Application.DTOs;
using SecurityService.Application.DTOs.ServiceToken;
using SecurityService.Domain.Interfaces.Services;

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
    public async Task<ActionResult<ApiResponse<ServiceTokenResponse>>> IssueToken(
        [FromBody] ServiceTokenIssueRequest request, CancellationToken ct)
    {
        var result = await _serviceTokenService.IssueTokenAsync(request.ServiceId, request.ServiceName, ct);

        var response = new ServiceTokenResponse
        {
            Token = result.Token,
            ExpiresInSeconds = result.ExpiresInSeconds
        };

        var apiResponse = ApiResponse<ServiceTokenResponse>.Ok(response, "Service token issued.");
        apiResponse.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(apiResponse);
    }
}
