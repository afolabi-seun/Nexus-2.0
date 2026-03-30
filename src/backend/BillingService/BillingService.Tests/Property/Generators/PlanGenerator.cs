using BillingService.Domain.Entities;
using System.Text.Json;

namespace BillingService.Tests.Property.Generators;

public static class PlanGenerator
{
    public static Plan CreateFreePlan() => new()
    {
        PlanId = Guid.NewGuid(),
        PlanName = "Free", PlanCode = "free", TierLevel = 0,
        MaxTeamMembers = 5, MaxDepartments = 3, MaxStoriesPerMonth = 50,
        FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "none", customWorkflows = false, prioritySupport = false }),
        PriceMonthly = 0m, PriceYearly = 0m, IsActive = true
    };

    public static Plan CreateStarterPlan() => new()
    {
        PlanId = Guid.NewGuid(),
        PlanName = "Starter", PlanCode = "starter", TierLevel = 1,
        MaxTeamMembers = 25, MaxDepartments = 5, MaxStoriesPerMonth = 500,
        FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "basic", customWorkflows = false, prioritySupport = false }),
        PriceMonthly = 29.00m, PriceYearly = 290.00m, IsActive = true
    };

    public static Plan CreateProPlan() => new()
    {
        PlanId = Guid.NewGuid(),
        PlanName = "Professional", PlanCode = "pro", TierLevel = 2,
        MaxTeamMembers = 100, MaxDepartments = 0, MaxStoriesPerMonth = 0,
        FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "full", customWorkflows = true, prioritySupport = true }),
        PriceMonthly = 99.00m, PriceYearly = 990.00m, IsActive = true
    };

    public static Plan CreateEnterprisePlan() => new()
    {
        PlanId = Guid.NewGuid(),
        PlanName = "Enterprise", PlanCode = "enterprise", TierLevel = 3,
        MaxTeamMembers = 0, MaxDepartments = 0, MaxStoriesPerMonth = 0,
        FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "full", customWorkflows = true, prioritySupport = true }),
        PriceMonthly = 299.00m, PriceYearly = 2990.00m, IsActive = true
    };

    public static Plan CreatePlanWithTier(int tier, bool isActive = true) => tier switch
    {
        0 => new Plan { PlanId = Guid.NewGuid(), PlanName = "Free", PlanCode = "free", TierLevel = 0, IsActive = isActive, MaxTeamMembers = 5, MaxDepartments = 3, MaxStoriesPerMonth = 50 },
        1 => new Plan { PlanId = Guid.NewGuid(), PlanName = "Starter", PlanCode = "starter", TierLevel = 1, IsActive = isActive, MaxTeamMembers = 25, MaxDepartments = 5, MaxStoriesPerMonth = 500 },
        2 => new Plan { PlanId = Guid.NewGuid(), PlanName = "Professional", PlanCode = "pro", TierLevel = 2, IsActive = isActive, MaxTeamMembers = 100, MaxDepartments = 0, MaxStoriesPerMonth = 0 },
        3 => new Plan { PlanId = Guid.NewGuid(), PlanName = "Enterprise", PlanCode = "enterprise", TierLevel = 3, IsActive = isActive, MaxTeamMembers = 0, MaxDepartments = 0, MaxStoriesPerMonth = 0 },
        _ => CreateFreePlan()
    };
}
