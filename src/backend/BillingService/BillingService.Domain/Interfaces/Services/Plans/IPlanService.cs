namespace BillingService.Domain.Interfaces.Services;

public interface IPlanService
{
    Task<object> GetAllActiveAsync(CancellationToken ct);
    Task SeedPlansAsync(CancellationToken ct);
}
