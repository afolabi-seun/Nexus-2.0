using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Infrastructure.Configuration;
using StackExchange.Redis;

namespace WorkService.Infrastructure.Services.ServiceClients;

public class SecurityServiceClient : ISecurityServiceClient
{
    private const string ClientName = "SecurityService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SecurityServiceClient> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public SecurityServiceClient(
        IHttpClientFactory httpClientFactory,
        IConnectionMultiplexer redis,
        AppSettings appSettings,
        ILogger<SecurityServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<string> GetServiceTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null && DateTime.UtcNow.AddSeconds(30) < _tokenExpiry)
            return _cachedToken;

        // Check Redis cache
        var db = _redis.GetDatabase();
        var redisCached = await db.StringGetAsync("work_service_token");
        if (redisCached.HasValue)
        {
            _cachedToken = redisCached.ToString();
            _tokenExpiry = DateTime.UtcNow.AddMinutes(10);
            return _cachedToken;
        }

        var client = _httpClientFactory.CreateClient(ClientName);
        var request = new { ServiceId = _appSettings.ServiceId, ServiceName = _appSettings.ServiceName };
        var response = await client.PostAsJsonAsync("/api/v1/service-tokens/issue", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceTokenResult>>(JsonOptions, ct);
        _cachedToken = result!.Data!.Token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(result.Data.ExpiresInSeconds);

        await db.StringSetAsync("work_service_token", _cachedToken,
            TimeSpan.FromSeconds(result.Data.ExpiresInSeconds - 60));

        return _cachedToken;
    }

    private class ServiceTokenResult
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
    }
}
