namespace BillingService.Application.DTOs.Subscriptions;

public record SubscriptionResponse(
    Guid SubscriptionId,
    Guid OrganizationId,
    Guid PlanId,
    string PlanName,
    string PlanCode,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndDate,
    DateTime? CancelledAt,
    Guid? ScheduledPlanId,
    string? ScheduledPlanName);
