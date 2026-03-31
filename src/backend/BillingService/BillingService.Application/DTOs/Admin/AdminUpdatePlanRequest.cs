namespace BillingService.Application.DTOs.Admin;

public record AdminUpdatePlanRequest(
    string PlanName,
    int TierLevel,
    int MaxTeamMembers,
    int MaxDepartments,
    int MaxStoriesPerMonth,
    decimal PriceMonthly,
    decimal PriceYearly,
    string? FeaturesJson);
