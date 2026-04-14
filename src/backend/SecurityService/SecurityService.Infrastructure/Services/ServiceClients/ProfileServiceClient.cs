using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityService.Application.Contracts;
using SecurityService.Application.DTOs;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services.ServiceToken;
using SecurityService.Infrastructure.Configuration;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.ServiceClients;

public class ProfileServiceClient : IProfileServiceClient
{
    private const string ClientName = "ProfileService";
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceTokenService _serviceTokenService;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ProfileServiceClient> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ProfileServiceClient(
        IHttpClientFactory httpClientFactory,
        IServiceTokenService serviceTokenService,
        IConnectionMultiplexer redis,
        ILogger<ProfileServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _serviceTokenService = serviceTokenService;
        _redis = redis;
        _logger = logger;
    }

    public async Task<ProfileUserResponse> GetTeamMemberByEmailAsync(string email, CancellationToken ct = default)
    {
        // Check Redis cache first
        var db = _redis.GetDatabase();
        var cacheKey = $"user_cache:{email}";
        var cached = await db.StringGetAsync(cacheKey);

        if (cached.HasValue)
        {
            var cachedUser = JsonSerializer.Deserialize<ProfileUserResponse>(cached!, JsonOptions);
            if (cachedUser is not null)
                return cachedUser;
        }

        var client = _httpClientFactory.CreateClient(ClientName);
        await AttachServiceTokenAsync(client);

        var endpoint = $"/api/v1/team-members/by-email/{Uri.EscapeDataString(email)}";
        var response = await client.GetAsync(endpoint, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProfileUserResponse>>(JsonOptions, ct);
        if (apiResponse?.Data is null)
            throw new ServiceUnavailableException("ProfileService returned an empty response.");

        // Cache the result
        var json = JsonSerializer.Serialize(apiResponse.Data, JsonOptions);
        await db.StringSetAsync(cacheKey, json, UserCacheTtl);

        return apiResponse.Data;
    }

    public async Task UpdatePasswordHashAsync(Guid memberId, string passwordHash, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        await AttachServiceTokenAsync(client);

        var endpoint = $"/api/v1/team-members/{memberId}/password";
        var response = await client.PutAsJsonAsync(endpoint, new { PasswordHash = passwordHash }, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }
    }

    public async Task SetIsFirstTimeUserAsync(Guid memberId, bool isFirstTimeUser, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        await AttachServiceTokenAsync(client);

        var endpoint = $"/api/v1/team-members/{memberId}/first-time-user";
        var response = await client.PutAsJsonAsync(endpoint, new { IsFirstTimeUser = isFirstTimeUser }, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }
    }

    public async Task<PlatformAdminResponse> GetPlatformAdminByUsernameAsync(string username, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        await AttachServiceTokenAsync(client);

        var endpoint = $"/api/v1/platform-admins/by-username/{Uri.EscapeDataString(username)}";
        var response = await client.GetAsync(endpoint, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformAdminResponse>>(JsonOptions, ct);
        if (apiResponse?.Data is null)
            throw new ServiceUnavailableException("ProfileService returned an empty response.");

        return apiResponse.Data;
    }

    public async Task UpdatePlatformAdminPasswordAsync(Guid platformAdminId, string passwordHash, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        await AttachServiceTokenAsync(client);

        var endpoint = $"/api/v1/platform-admins/{platformAdminId}/password";
        var response = await client.PatchAsJsonAsync(endpoint, new { PasswordHash = passwordHash }, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }
    }

    private async Task AttachServiceTokenAsync(HttpClient client)
    {
        if (_cachedToken is not null && DateTime.UtcNow.AddSeconds(30) < _tokenExpiry)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
            return;
        }

        var result = await _serviceTokenService.IssueTokenAsync("SecurityService", "SecurityService");
        _cachedToken = result.Token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresInSeconds);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, ct);
            if (errorResponse is not null && !string.IsNullOrEmpty(errorResponse.ErrorCode))
            {
                throw new DomainException(
                    errorResponse.ErrorValue ?? ErrorCodes.ServiceUnavailableValue,
                    errorResponse.ErrorCode,
                    errorResponse.Message ?? "Downstream service error.",
                    response.StatusCode);
            }
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize error response from ProfileService. Status: {StatusCode}",
                response.StatusCode);
        }

        throw new ServiceUnavailableException("ProfileService is unavailable.");
    }
}
