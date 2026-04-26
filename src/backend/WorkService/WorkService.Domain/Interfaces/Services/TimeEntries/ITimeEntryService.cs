using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.TimeEntries;

public interface ITimeEntryService
{
    Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid timeEntryId, Guid userId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid timeEntryId, Guid userId, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid orgId, Guid? storyId, Guid? projectId, Guid? sprintId,
        Guid? memberId, DateTime? dateFrom, DateTime? dateTo, bool? isBillable,
        string? status, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> ApproveAsync(Guid timeEntryId, Guid approverId, string approverRole,
        Guid approverDeptId, CancellationToken ct = default);
    Task<ServiceResult<object>> RejectAsync(Guid timeEntryId, Guid approverId, string approverRole,
        Guid approverDeptId, string reason, CancellationToken ct = default);
    Task<ServiceResult<object>> GetProjectCostSummaryAsync(Guid projectId, DateTime? dateFrom,
        DateTime? dateTo, CancellationToken ct = default);
    Task<ServiceResult<object>> GetProjectUtilizationAsync(Guid projectId, DateTime? dateFrom,
        DateTime? dateTo, CancellationToken ct = default);
    Task<ServiceResult<object>> GetSprintVelocityAsync(Guid sprintId, CancellationToken ct = default);
}
