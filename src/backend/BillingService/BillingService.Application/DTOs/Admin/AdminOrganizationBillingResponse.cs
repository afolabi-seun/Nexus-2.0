using BillingService.Application.DTOs.Plans;
using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.DTOs.Usage;

namespace BillingService.Application.DTOs.Admin;

public record AdminOrganizationBillingResponse(
    SubscriptionResponse Subscription,
    PlanResponse Plan,
    UsageResponse Usage);
