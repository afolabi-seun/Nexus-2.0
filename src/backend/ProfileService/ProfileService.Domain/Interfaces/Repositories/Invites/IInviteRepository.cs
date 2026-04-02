using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.Invites;

public interface IInviteRepository : IGenericRepository<Invite>
{
    Task<Invite?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<(IEnumerable<Invite> Items, int TotalCount)> ListPendingAsync(Guid organizationId, Guid? departmentId, int page, int pageSize, CancellationToken ct = default);
}
