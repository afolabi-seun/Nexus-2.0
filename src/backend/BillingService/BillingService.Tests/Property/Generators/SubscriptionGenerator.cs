using BillingService.Domain.Entities;
using BillingService.Domain.Enums;

namespace BillingService.Tests.Property.Generators;

public static class SubscriptionGenerator
{
    public static Subscription CreateTrialing(Plan plan, Guid? orgId = null) => new()
    {
        SubscriptionId = Guid.NewGuid(),
        OrganizationId = orgId ?? Guid.NewGuid(),
        PlanId = plan.PlanId,
        Plan = plan,
        Status = SubscriptionStatus.Trialing,
        CurrentPeriodStart = DateTime.UtcNow,
        CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
        TrialEndDate = DateTime.UtcNow.AddDays(14),
        ExternalSubscriptionId = $"sub_{Guid.NewGuid():N}",
        ExternalCustomerId = $"cus_{Guid.NewGuid():N}",
        DateCreated = DateTime.UtcNow,
        DateUpdated = DateTime.UtcNow
    };

    public static Subscription CreateActive(Plan plan, Guid? orgId = null) => new()
    {
        SubscriptionId = Guid.NewGuid(),
        OrganizationId = orgId ?? Guid.NewGuid(),
        PlanId = plan.PlanId,
        Plan = plan,
        Status = SubscriptionStatus.Active,
        CurrentPeriodStart = DateTime.UtcNow,
        CurrentPeriodEnd = plan.PlanCode == "free" ? null : DateTime.UtcNow.AddMonths(1),
        ExternalSubscriptionId = plan.PlanCode != "free" ? $"sub_{Guid.NewGuid():N}" : null,
        ExternalCustomerId = plan.PlanCode != "free" ? $"cus_{Guid.NewGuid():N}" : null,
        DateCreated = DateTime.UtcNow,
        DateUpdated = DateTime.UtcNow
    };

    public static Subscription CreateCancelled(Plan plan, Guid? orgId = null) => new()
    {
        SubscriptionId = Guid.NewGuid(),
        OrganizationId = orgId ?? Guid.NewGuid(),
        PlanId = plan.PlanId,
        Plan = plan,
        Status = SubscriptionStatus.Cancelled,
        CancelledAt = DateTime.UtcNow,
        DateCreated = DateTime.UtcNow.AddDays(-30),
        DateUpdated = DateTime.UtcNow
    };
}
