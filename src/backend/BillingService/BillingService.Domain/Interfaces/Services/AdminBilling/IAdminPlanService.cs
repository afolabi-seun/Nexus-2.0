using BillingService.Domain.Results;

namespace BillingService.Domain.Interfaces.Services.AdminBilling;

public interface IAdminPlanService
{
    Task<ServiceResult<object>> GetAllPlansAsync(CancellationToken ct);
    Task<ServiceResult<object>> CreatePlanAsync(object request, CancellationToken ct);
    Task<ServiceResult<object>> UpdatePlanAsync(Guid planId, object request, CancellationToken ct);
    Task<ServiceResult<object>> DeactivatePlanAsync(Guid planId, Guid adminId, CancellationToken ct);
}
