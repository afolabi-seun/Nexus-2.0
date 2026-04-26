using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Reports;

public interface IReportService
{
    Task<ServiceResult<object>> GetVelocityChartAsync(Guid organizationId, int count, CancellationToken ct = default);
    Task<ServiceResult<object>> GetDepartmentWorkloadAsync(Guid organizationId, Guid? sprintId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetCapacityUtilizationAsync(Guid organizationId, Guid? departmentId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetCycleTimeAsync(Guid organizationId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<ServiceResult<object>> GetTaskCompletionAsync(Guid organizationId, Guid? sprintId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}
