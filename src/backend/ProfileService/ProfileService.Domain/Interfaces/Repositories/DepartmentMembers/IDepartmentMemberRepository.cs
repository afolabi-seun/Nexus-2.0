using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;

public interface IDepartmentMemberRepository : IGenericRepository<DepartmentMember>
{
    Task<DepartmentMember?> GetAsync(Guid memberId, Guid departmentId, CancellationToken ct = default);
    Task<IEnumerable<DepartmentMember>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<(IEnumerable<DepartmentMember> Items, int TotalCount)> ListByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default);
}
