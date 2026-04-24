using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityService.Application.Contracts;
using SecurityService.Application.DTOs;
using SecurityService.Domain.Exceptions;

namespace SecurityService.Infrastructure.Services.ServiceClients;

public class UtilityServiceClient : IUtilityServiceClient
{
    private const string ClientName = "UtilityService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UtilityServiceClient> _logger;

    public UtilityServiceClient(IHttpClientFactory httpClientFactory, ILogger<UtilityServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ErrorCodeResponse> GetErrorCodeAsync(string errorCode, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var endpoint = $"/api/v1/error-codes/{Uri.EscapeDataString(errorCode)}";

        var response = await client.GetAsync(endpoint, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UtilityService returned {StatusCode} for error code {ErrorCode}",
                response.StatusCode, errorCode);
            throw new ServiceUnavailableException("UtilityService is unavailable.");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ErrorCodeResponse>>(JsonOptions, ct);
        if (apiResponse?.Data is null)
            throw new ServiceUnavailableException("UtilityService returned an empty response.");

        return apiResponse.Data;
    }

    public async Task<Dictionary<string, (string ResponseCode, string ResponseDescription)>> GetAllErrorCodesAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var response = await client.GetAsync("/api/v1/error-codes", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("UtilityService returned {StatusCode} when fetching all error codes.",
                response.StatusCode);
            throw new ServiceUnavailableException("UtilityService is unavailable.");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseWrapper>(JsonOptions, ct);
        if (apiResponse?.Data is null)
            return new Dictionary<string, (string, string)>();

        return apiResponse.Data.ToDictionary(
            e => e.Code,
            e => (e.ResponseCode, e.Description));
    }

    private class ApiResponseWrapper
    {
        public List<ErrorCodeEntry>? Data { get; set; }
    }

    private class ErrorCodeEntry
    {
        public string Code { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
