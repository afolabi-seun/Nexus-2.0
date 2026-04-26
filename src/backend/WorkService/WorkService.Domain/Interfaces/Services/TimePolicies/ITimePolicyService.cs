using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.TimePolicies;

public interface ITimePolicyService
{
    Task<ServiceResult<object>> GetPolicyAsync(Guid orgId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpsertAsync(Guid orgId, Guid userId, string userRole, object request, CancellationToken ct = default);
}
