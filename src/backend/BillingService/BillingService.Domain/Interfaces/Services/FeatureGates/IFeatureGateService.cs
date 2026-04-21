using BillingService.Domain.Results;

namespace BillingService.Domain.Interfaces.Services.FeatureGates;

public interface IFeatureGateService
{
    Task<ServiceResult<object>> CheckFeatureAsync(Guid organizationId, string feature, CancellationToken ct);
}
