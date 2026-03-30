namespace BillingService.Application.DTOs.Plans;

public record PlanResponse(
    Guid PlanId,
    string PlanName,
    string PlanCode,
    int TierLevel,
    int MaxTeamMembers,
    int MaxDepartments,
    int MaxStoriesPerMonth,
    string? FeaturesJson,
    decimal PriceMonthly,
    decimal PriceYearly);
