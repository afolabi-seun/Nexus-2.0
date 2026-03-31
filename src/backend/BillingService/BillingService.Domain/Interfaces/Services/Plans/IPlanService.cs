namespace BillingService.Domain.Interfaces.Services.Plans;

public interface IPlanService
{
    Task<object> GetAllActiveAsync(CancellationToken ct);
    Task SeedPlansAsync(CancellationToken ct);
}
