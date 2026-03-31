namespace BillingService.Domain.Interfaces.Services;

public interface IFeatureGateService
{
    Task<object> CheckFeatureAsync(Guid organizationId, string feature, CancellationToken ct);
}
