namespace BillingService.Domain.Interfaces.Services.FeatureGates;

public interface IFeatureGateService
{
    Task<object> CheckFeatureAsync(Guid organizationId, string feature, CancellationToken ct);
}
