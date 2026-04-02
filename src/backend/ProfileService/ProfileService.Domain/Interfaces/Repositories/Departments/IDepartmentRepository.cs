using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.Departments;

public interface IDepartmentRepository : IGenericRepository<Department>
{
    Task<Department?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default);
    Task<Department?> GetByCodeAsync(Guid organizationId, string code, CancellationToken ct = default);
    Task<(IEnumerable<Department> Items, int TotalCount)> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetActiveMemberCountAsync(Guid departmentId, CancellationToken ct = default);
}
