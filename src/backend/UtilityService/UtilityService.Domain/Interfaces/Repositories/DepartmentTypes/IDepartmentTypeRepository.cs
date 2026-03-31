using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories.DepartmentTypes;

public interface IDepartmentTypeRepository
{
    Task<DepartmentType?> GetByNameAsync(string typeName, CancellationToken ct = default);
    Task<DepartmentType?> GetByCodeAsync(string typeCode, CancellationToken ct = default);
    Task<DepartmentType> AddAsync(DepartmentType departmentType, CancellationToken ct = default);
    Task<IEnumerable<DepartmentType>> ListAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<DepartmentType> types, CancellationToken ct = default);
    Task<bool> ExistsAsync(string typeName, CancellationToken ct = default);
}
