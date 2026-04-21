using BillingService.Domain.Results;

namespace BillingService.Domain.Interfaces.Services.Plans;

public interface IPlanService
{
    Task<ServiceResult<object>> GetAllActiveAsync(CancellationToken ct);
    Task SeedPlansAsync(CancellationToken ct);
}
