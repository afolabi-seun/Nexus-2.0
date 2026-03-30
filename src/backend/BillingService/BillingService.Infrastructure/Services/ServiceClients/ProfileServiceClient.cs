using System.Net.Http.Headers;
using System.Net.Http.Json;
using BillingService.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Services.ServiceClients;

public class ProfileServiceClient : IProfileServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecurityServiceClient _securityServiceClient;
    private readonly ILogger<ProfileServiceClient> _logger;

    public ProfileServiceClient(
        IHttpClientFactory httpClientFactory,
        ISecurityServiceClient securityServiceClient,
        ILogger<ProfileServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _securityServiceClient = securityServiceClient;
        _logger = logger;
    }

    public async Task UpdateOrganizationPlanTierAsync(Guid organizationId, string planCode, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("ProfileService");
        var token = await _securityServiceClient.GetServiceTokenAsync(ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new OrganizationSettingsUpdateRequest { PlanTier = planCode };
        var response = await client.PutAsJsonAsync($"api/v1/organizations/{organizationId}/settings", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to update ProfileService plan tier for org {OrgId}: {StatusCode}",
                organizationId, response.StatusCode);
        }
    }
}
