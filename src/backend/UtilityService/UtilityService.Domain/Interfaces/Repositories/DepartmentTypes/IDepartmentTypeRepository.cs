using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.DepartmentTypes;

public interface IDepartmentTypeRepository : IGenericRepository<DepartmentType>
{
    Task<DepartmentType?> GetByNameAsync(string typeName, CancellationToken ct = default);
    Task<DepartmentType?> GetByCodeAsync(string typeCode, CancellationToken ct = default);
    Task<IEnumerable<DepartmentType>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string typeName, CancellationToken ct = default);
}
