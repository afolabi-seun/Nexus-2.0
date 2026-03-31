namespace WorkService.Domain.Interfaces.Services.Reports;

public interface IReportService
{
    Task<object> GetVelocityChartAsync(Guid organizationId, int count, CancellationToken ct = default);
    Task<object> GetDepartmentWorkloadAsync(Guid organizationId, Guid? sprintId, CancellationToken ct = default);
    Task<object> GetCapacityUtilizationAsync(Guid organizationId, Guid? departmentId, CancellationToken ct = default);
    Task<object> GetCycleTimeAsync(Guid organizationId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<object> GetTaskCompletionAsync(Guid organizationId, Guid? sprintId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}
