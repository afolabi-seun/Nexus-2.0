namespace BillingService.Domain.Interfaces.Services.AdminBilling;

public interface IAdminBillingService
{
    Task<object> GetAllSubscriptionsAsync(string? status, string? search, int page, int pageSize, CancellationToken ct);
    Task<object> GetOrganizationBillingAsync(Guid organizationId, CancellationToken ct);
    Task<object> OverrideSubscriptionAsync(Guid organizationId, Guid planId, string? reason, Guid adminId, CancellationToken ct);
    Task<object> AdminCancelSubscriptionAsync(Guid organizationId, string? reason, Guid adminId, CancellationToken ct);
    Task<object> GetUsageSummaryAsync(CancellationToken ct);
    Task<object> GetOrganizationUsageListAsync(int? threshold, int page, int pageSize, CancellationToken ct);
}
