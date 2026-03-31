namespace UtilityService.Domain.Interfaces.Services.ReferenceData;

public interface IReferenceDataService
{
    Task<IEnumerable<object>> GetDepartmentTypesAsync(CancellationToken ct = default);
    Task<IEnumerable<object>> GetPriorityLevelsAsync(CancellationToken ct = default);
    Task<IEnumerable<object>> GetTaskTypesAsync(CancellationToken ct = default);
    Task<IEnumerable<object>> GetWorkflowStatesAsync(CancellationToken ct = default);
    Task<object> CreateDepartmentTypeAsync(object request, CancellationToken ct = default);
    Task<object> CreatePriorityLevelAsync(object request, CancellationToken ct = default);
}
