using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Workflows;

public interface IWorkflowService
{
    Task<ServiceResult<object>> GetWorkflowsAsync(Guid organizationId, CancellationToken ct = default);
    Task<ServiceResult<object>> SaveOrganizationOverrideAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> SaveDepartmentOverrideAsync(Guid organizationId, Guid departmentId, object request, CancellationToken ct = default);
}
