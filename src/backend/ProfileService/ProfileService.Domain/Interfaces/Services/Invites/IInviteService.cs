using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Invites;

public interface IInviteService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid invitedByMemberId, Guid inviterDepartmentId, string inviterRole, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid organizationId, Guid? departmentId, string role, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> ValidateTokenAsync(string token, CancellationToken ct = default);
    Task<ServiceResult<object>> AcceptAsync(string token, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> CancelAsync(Guid inviteId, CancellationToken ct = default);
}
