using System.Text.Json;
using BillingService.Application.DTOs.Plans;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Services.Plans;
using BillingService.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.Plans;

public class PlanService : IPlanService
{
    private readonly BillingDbContext _dbContext;
    private readonly IPlanRepository _planRepository;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<PlanService> _logger;

    public PlanService(BillingDbContext dbContext, IPlanRepository planRepository, IConnectionMultiplexer redis, ILogger<PlanService> logger)
    {
        _dbContext = dbContext;
        _planRepository = planRepository;
        _redis = redis;
        _logger = logger;
    }

    public async Task<object> GetAllActiveAsync(CancellationToken ct)
    {
        var plans = await _planRepository.GetAllActiveAsync(ct);
        return plans.Select(p => new PlanResponse(
            p.PlanId, p.PlanName, p.PlanCode, p.TierLevel,
            p.MaxTeamMembers, p.MaxDepartments, p.MaxStoriesPerMonth,
            p.FeaturesJson, p.PriceMonthly, p.PriceYearly)).ToList();
    }

    public async Task SeedPlansAsync(CancellationToken ct)
    {
        var seedPlans = GetSeedPlans();
        foreach (var plan in seedPlans)
        {
            if (!await _planRepository.ExistsByCodeAsync(plan.PlanCode, ct))
            {
                await _planRepository.AddAsync(plan, ct);
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded plan: {PlanCode}", plan.PlanCode);
            }
        }
    }

    private static List<Plan> GetSeedPlans() =>
    [
        new Plan
        {
            PlanName = "Free", PlanCode = "free", TierLevel = 0,
            MaxTeamMembers = 5, MaxDepartments = 3, MaxStoriesPerMonth = 50,
            FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "none", customWorkflows = false, prioritySupport = false }),
            PriceMonthly = 0m, PriceYearly = 0m
        },
        new Plan
        {
            PlanName = "Starter", PlanCode = "starter", TierLevel = 1,
            MaxTeamMembers = 25, MaxDepartments = 5, MaxStoriesPerMonth = 500,
            FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "basic", customWorkflows = false, prioritySupport = false }),
            PriceMonthly = 29.00m, PriceYearly = 290.00m
        },
        new Plan
        {
            PlanName = "Professional", PlanCode = "pro", TierLevel = 2,
            MaxTeamMembers = 100, MaxDepartments = 0, MaxStoriesPerMonth = 0,
            FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "full", customWorkflows = true, prioritySupport = true }),
            PriceMonthly = 99.00m, PriceYearly = 990.00m
        },
        new Plan
        {
            PlanName = "Enterprise", PlanCode = "enterprise", TierLevel = 3,
            MaxTeamMembers = 0, MaxDepartments = 0, MaxStoriesPerMonth = 0,
            FeaturesJson = JsonSerializer.Serialize(new { sprintAnalytics = "full", customWorkflows = true, prioritySupport = true }),
            PriceMonthly = 299.00m, PriceYearly = 2990.00m
        }
    ];
}
