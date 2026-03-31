using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.Departments;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken ct = default);
    Task<Department?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default);
    Task<Department?> GetByCodeAsync(Guid organizationId, string code, CancellationToken ct = default);
    Task<Department> AddAsync(Department department, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Department> departments, CancellationToken ct = default);
    Task UpdateAsync(Department department, CancellationToken ct = default);
    Task<(IEnumerable<Department> Items, int TotalCount)> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetActiveMemberCountAsync(Guid departmentId, CancellationToken ct = default);
}
