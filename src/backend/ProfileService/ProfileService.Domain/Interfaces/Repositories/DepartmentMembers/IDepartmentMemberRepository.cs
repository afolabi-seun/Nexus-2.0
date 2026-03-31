using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;

public interface IDepartmentMemberRepository
{
    Task<DepartmentMember?> GetAsync(Guid memberId, Guid departmentId, CancellationToken ct = default);
    Task<DepartmentMember> AddAsync(DepartmentMember departmentMember, CancellationToken ct = default);
    Task RemoveAsync(DepartmentMember departmentMember, CancellationToken ct = default);
    Task UpdateAsync(DepartmentMember departmentMember, CancellationToken ct = default);
    Task<IEnumerable<DepartmentMember>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<(IEnumerable<DepartmentMember> Items, int TotalCount)> ListByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default);
}
