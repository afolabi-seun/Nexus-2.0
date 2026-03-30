using System.Net.Http.Json;
using BillingService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.ServiceClients;

public class SecurityServiceClient : ISecurityServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SecurityServiceClient> _logger;

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

    public async Task<string> GetServiceTokenAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync("service_token:billing");
        if (cached.HasValue) return cached!;

        var client = _httpClientFactory.CreateClient("SecurityService");
        var response = await client.PostAsJsonAsync("api/v1/service-tokens/issue", new
        {
            serviceId = _appSettings.ServiceId,
            serviceName = _appSettings.ServiceName,
            serviceSecret = _appSettings.ServiceSecret
        }, ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ServiceTokenResponse>(cancellationToken: ct);
        var token = result?.Data?.Token ?? throw new InvalidOperationException("Failed to obtain service token.");

        await db.StringSetAsync("service_token:billing", token, TimeSpan.FromHours(23));
        return token;
    }

    private record ServiceTokenResponse(ServiceTokenData? Data);
    private record ServiceTokenData(string Token);
}
