using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WorkService.Application.Contracts;
using WorkService.Application.DTOs;

namespace WorkService.Infrastructure.Services.ServiceClients;

public class ProfileServiceClient : IProfileServiceClient
{
    private const string ClientName = "ProfileService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecurityServiceClient _securityClient;
    private readonly ILogger<ProfileServiceClient> _logger;

    public ProfileServiceClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ISecurityServiceClient securityClient,
        ILogger<ProfileServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _securityClient = securityClient;
        _logger = logger;
    }

    public async Task<OrganizationSettingsResponse> GetOrganizationSettingsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/organizations/{organizationId}/settings", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationSettingsResponse>>(JsonOptions, ct);
        return result?.Data ?? new OrganizationSettingsResponse { DefaultSprintDurationWeeks = 2 };
    }

    public async Task<TeamMemberResponse?> GetTeamMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/team-members/{memberId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamMemberResponse>>(JsonOptions, ct);
        return result?.Data;
    }

    public async Task<IEnumerable<TeamMemberResponse>> GetDepartmentMembersAsync(Guid departmentId, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/departments/{departmentId}/members", ct);
        if (!response.IsSuccessStatusCode) return [];
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<TeamMemberResponse>>>(JsonOptions, ct);
        return result?.Data ?? [];
    }

    public async Task<DepartmentResponse?> GetDepartmentByCodeAsync(Guid organizationId, string departmentCode, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/departments/by-code/{departmentCode}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>(JsonOptions, ct);
        return result?.Data;
    }

    public async Task<TeamMemberResponse?> ResolveUserByDisplayNameAsync(Guid organizationId, string displayName, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/team-members/by-name/{Uri.EscapeDataString(displayName)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamMemberResponse>>(JsonOptions, ct);
        return result?.Data;
    }

    public async Task<TeamMemberResponse?> ResolveUserByEmailAsync(Guid organizationId, string email, CancellationToken ct = default)
    {
        var client = await CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/team-members/by-email/{Uri.EscapeDataString(email)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeamMemberResponse>>(JsonOptions, ct);
        return result?.Data;
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var token = await _securityClient.GetServiceTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (_httpContextAccessor.HttpContext?.Items.TryGetValue("OrganizationId", out var orgId) == true)
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Organization-Id", orgId?.ToString());

        return client;
    }
}
