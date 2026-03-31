using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.Invites;

public interface IInviteRepository
{
    Task<Invite?> GetByIdAsync(Guid inviteId, CancellationToken ct = default);
    Task<Invite?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<Invite> AddAsync(Invite invite, CancellationToken ct = default);
    Task UpdateAsync(Invite invite, CancellationToken ct = default);
    Task<(IEnumerable<Invite> Items, int TotalCount)> ListPendingAsync(Guid organizationId, Guid? departmentId, int page, int pageSize, CancellationToken ct = default);
}
