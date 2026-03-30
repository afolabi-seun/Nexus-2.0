namespace BillingService.Infrastructure.Services.ServiceClients;

public interface IProfileServiceClient
{
    Task UpdateOrganizationPlanTierAsync(Guid organizationId, string planCode, CancellationToken ct);
}
