namespace BillingService.Application.DTOs.Admin;

public record AdminCreatePlanRequest(
    string PlanName,
    string PlanCode,
    int TierLevel,
    int MaxTeamMembers,
    int MaxDepartments,
    int MaxStoriesPerMonth,
    decimal PriceMonthly,
    decimal PriceYearly,
    string? FeaturesJson);
