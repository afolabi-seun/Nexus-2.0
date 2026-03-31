namespace ProfileService.Domain.Interfaces.Services.Invites;

public interface IInviteService
{
    Task<object> CreateAsync(Guid organizationId, Guid invitedByMemberId, Guid inviterDepartmentId, string inviterRole, object request, CancellationToken ct = default);
    Task<object> ListAsync(Guid organizationId, Guid? departmentId, string role, int page, int pageSize, CancellationToken ct = default);
    Task<object> ValidateTokenAsync(string token, CancellationToken ct = default);
    Task AcceptAsync(string token, object request, CancellationToken ct = default);
    Task CancelAsync(Guid inviteId, CancellationToken ct = default);
}
