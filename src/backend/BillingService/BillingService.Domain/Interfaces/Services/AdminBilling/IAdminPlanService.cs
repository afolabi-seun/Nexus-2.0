namespace BillingService.Domain.Interfaces.Services.AdminBilling;

public interface IAdminPlanService
{
    Task<object> GetAllPlansAsync(CancellationToken ct);
    Task<object> CreatePlanAsync(object request, CancellationToken ct);
    Task<object> UpdatePlanAsync(Guid planId, object request, CancellationToken ct);
    Task<object> DeactivatePlanAsync(Guid planId, Guid adminId, CancellationToken ct);
}
