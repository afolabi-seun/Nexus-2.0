namespace BillingService.Application.DTOs.Admin;

public record AdminSubscriptionListItem(
    Guid SubscriptionId,
    Guid OrganizationId,
    string OrganizationName,
    Guid PlanId,
    string PlanName,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndDate);
