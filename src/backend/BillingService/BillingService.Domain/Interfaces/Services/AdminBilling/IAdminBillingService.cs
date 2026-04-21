using BillingService.Domain.Results;

namespace BillingService.Domain.Interfaces.Services.AdminBilling;

public interface IAdminBillingService
{
    Task<ServiceResult<object>> GetAllSubscriptionsAsync(string? status, string? search, int page, int pageSize, CancellationToken ct);
    Task<ServiceResult<object>> GetOrganizationBillingAsync(Guid organizationId, CancellationToken ct);
    Task<ServiceResult<object>> OverrideSubscriptionAsync(Guid organizationId, Guid planId, string? reason, Guid adminId, CancellationToken ct);
    Task<ServiceResult<object>> AdminCancelSubscriptionAsync(Guid organizationId, string? reason, Guid adminId, CancellationToken ct);
    Task<ServiceResult<object>> GetUsageSummaryAsync(CancellationToken ct);
    Task<ServiceResult<object>> GetOrganizationUsageListAsync(int? threshold, int page, int pageSize, CancellationToken ct);
}
