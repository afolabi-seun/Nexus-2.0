namespace BillingService.Application.DTOs.Admin;

public record AdminUsageSummaryResponse(
    long TotalActiveMembers,
    long TotalStoriesCreated,
    long TotalStorageBytes,
    List<PlanTierBreakdown> ByPlanTier);
