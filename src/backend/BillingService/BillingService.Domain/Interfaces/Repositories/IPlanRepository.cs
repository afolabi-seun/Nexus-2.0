using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces.Repositories;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct);
    Task<Plan?> GetByCodeAsync(string planCode, CancellationToken ct);
    Task<List<Plan>> GetAllActiveAsync(CancellationToken ct);
    Task CreateAsync(Plan plan, CancellationToken ct);
    Task<bool> ExistsByCodeAsync(string planCode, CancellationToken ct);
    Task<List<Plan>> GetAllAsync(CancellationToken ct);
    Task UpdateAsync(Plan plan, CancellationToken ct);
}
