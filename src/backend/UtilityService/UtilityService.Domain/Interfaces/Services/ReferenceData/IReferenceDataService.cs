using UtilityService.Domain.Results;

namespace UtilityService.Domain.Interfaces.Services.ReferenceData;

public interface IReferenceDataService
{
    Task<ServiceResult<IEnumerable<object>>> GetDepartmentTypesAsync(CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<object>>> GetPriorityLevelsAsync(CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<object>>> GetTaskTypesAsync(CancellationToken ct = default);
    Task<ServiceResult<IEnumerable<object>>> GetWorkflowStatesAsync(CancellationToken ct = default);
    Task<ServiceResult<object>> CreateDepartmentTypeAsync(object request, CancellationToken ct = default);
    Task<ServiceResult<object>> CreatePriorityLevelAsync(object request, CancellationToken ct = default);
}
