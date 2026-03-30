using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Exceptions;
using ProfileService.Infrastructure.Configuration;

namespace ProfileService.Infrastructure.Services.ServiceClients;

public class SecurityServiceClient : ISecurityServiceClient
{
    private const string ClientName = "SecurityService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SecurityServiceClient> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public SecurityServiceClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        AppSettings appSettings,
        ILogger<SecurityServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task GenerateCredentialsAsync(Guid memberId, string email, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        await AttachHeadersAsync(client);

        var request = new { MemberId = memberId, Email = email };
        var response = await client.PostAsJsonAsync("/api/v1/auth/credentials/generate", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await TryDeserializeError(response, ct);
            throw errorBody is not null
                ? new DomainException(
                    errorBody.ErrorValue ?? ErrorCodes.ServiceUnavailableValue,
                    errorBody.ErrorCode ?? ErrorCodes.ServiceUnavailable,
                    errorBody.Message ?? "Downstream error",
                    (HttpStatusCode)response.StatusCode)
                : new ServiceUnavailableException("SecurityService credential generation failed");
        }
    }

    private async Task AttachHeadersAsync(HttpClient client)
    {
        var token = await GetServiceTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (_httpContextAccessor.HttpContext?.Items.TryGetValue("OrganizationId", out var orgId) == true)
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Organization-Id", orgId?.ToString());
    }

    private async Task<string> GetServiceTokenAsync()
    {
        if (_cachedToken is not null && DateTime.UtcNow.AddSeconds(30) < _tokenExpiry)
            return _cachedToken;

        var client = _httpClientFactory.CreateClient(ClientName);
        var request = new { ServiceId = _appSettings.ServiceId, ServiceName = _appSettings.ServiceName };
        var response = await client.PostAsJsonAsync("/api/v1/service-tokens/issue", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceTokenResult>>(JsonOptions);
        _cachedToken = result!.Data!.Token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(result.Data.ExpiresInSeconds);
        return _cachedToken;
    }

    private static async Task<ApiResponse<object>?> TryDeserializeError(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    private class ServiceTokenResult
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }
}
