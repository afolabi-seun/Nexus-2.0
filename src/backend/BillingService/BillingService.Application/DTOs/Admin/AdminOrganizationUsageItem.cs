namespace BillingService.Application.DTOs.Admin;

public record AdminOrganizationUsageItem(
    Guid OrganizationId,
    string OrganizationName,
    string PlanName,
    List<UsageMetricWithLimit> Metrics);
