using BillingService.Domain.Common;

namespace BillingService.Domain.Entities;

public class Subscription : IOrganizationEntity
{
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? ScheduledPlanId { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public Plan? Plan { get; set; }
    public Plan? ScheduledPlan { get; set; }
}
