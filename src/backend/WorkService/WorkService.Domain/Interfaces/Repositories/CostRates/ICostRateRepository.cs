using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.CostRates;

public interface ICostRateRepository : IGenericRepository<CostRate>
{
    Task<(IEnumerable<CostRate> Items, int TotalCount)> ListAsync(
        Guid organizationId, string? rateType, Guid? memberId,
        Guid? departmentId, string? roleName, int page, int pageSize,
        CancellationToken ct = default);
    Task<bool> ExistsDuplicateAsync(Guid organizationId, string rateType,
        Guid? memberId, string? roleName, Guid? departmentId,
        CancellationToken ct = default);
    Task<IEnumerable<CostRate>> GetActiveRatesForMemberAsync(
        Guid organizationId, Guid memberId, DateTime asOfDate,
        CancellationToken ct = default);
    Task<IEnumerable<CostRate>> GetActiveRatesForRoleDepartmentAsync(
        Guid organizationId, string roleName, Guid departmentId, DateTime asOfDate,
        CancellationToken ct = default);
    Task<CostRate?> GetOrgDefaultAsync(Guid organizationId, DateTime asOfDate,
        CancellationToken ct = default);
}
