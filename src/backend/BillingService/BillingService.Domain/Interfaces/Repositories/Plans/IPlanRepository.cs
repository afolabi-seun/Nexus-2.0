using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Generics;

namespace BillingService.Domain.Interfaces.Repositories.Plans;

public interface IPlanRepository : IGenericRepository<Plan>
{
    Task<Plan?> GetByCodeAsync(string planCode, CancellationToken ct);
    Task<List<Plan>> GetAllActiveAsync(CancellationToken ct);
    Task<bool> ExistsByCodeAsync(string planCode, CancellationToken ct);
}
