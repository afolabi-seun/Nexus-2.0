namespace WorkService.Domain.Interfaces.Services.TimePolicies;

public interface ITimePolicyService
{
    Task<object> GetPolicyAsync(Guid orgId, CancellationToken ct = default);
    Task<object> UpsertAsync(Guid orgId, Guid userId, string userRole, object request, CancellationToken ct = default);
}
