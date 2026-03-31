namespace BillingService.Application.DTOs.Admin;

public record AdminPlanResponse(
    Guid PlanId,
    string PlanName,
    string PlanCode,
    int TierLevel,
    int MaxTeamMembers,
    int MaxDepartments,
    int MaxStoriesPerMonth,
    string? FeaturesJson,
    decimal PriceMonthly,
    decimal PriceYearly,
    bool IsActive,
    DateTime DateCreated);
