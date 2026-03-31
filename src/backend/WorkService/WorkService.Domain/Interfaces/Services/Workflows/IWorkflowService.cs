namespace WorkService.Domain.Interfaces.Services.Workflows;

public interface IWorkflowService
{
    Task<object> GetWorkflowsAsync(Guid organizationId, CancellationToken ct = default);
    Task SaveOrganizationOverrideAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task SaveDepartmentOverrideAsync(Guid organizationId, Guid departmentId, object request, CancellationToken ct = default);
}
