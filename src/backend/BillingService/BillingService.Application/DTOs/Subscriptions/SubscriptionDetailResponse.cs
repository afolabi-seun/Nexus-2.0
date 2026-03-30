using BillingService.Application.DTOs.Plans;
using BillingService.Application.DTOs.Usage;

namespace BillingService.Application.DTOs.Subscriptions;

public record SubscriptionDetailResponse(
    SubscriptionResponse Subscription,
    PlanResponse Plan,
    UsageResponse Usage);
