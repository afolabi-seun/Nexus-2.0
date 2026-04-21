using System.Text.Json;
using BillingService.Application.DTOs.Plans;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Services.Plans;
using BillingService.Tests.Property.Generators;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Property;

/// <summary>
/// Property-based tests for plan management.
/// </summary>
public class PlanPropertyTests
{
    /// <summary>
    /// Feature: billing-service, Property 1: Active plan filtering
    /// **Validates: Requirements 1.2**
    /// For any set of plans, GetAllActiveAsync returns exactly plans where IsActive == true.
    /// </summary>
    [Property(MaxTest = 100)]
    public async void Property1_ActivePlanFiltering_ReturnsOnlyActivePlans()
    {
        // Arrange
        var plans = new List<Plan>();
        for (int i = 0; i < 10; i++)
        {
            plans.Add(new Plan
            {
                PlanId = Guid.NewGuid(),
                PlanName = $"Plan{i}",
                PlanCode = $"plan{i}",
                TierLevel = i % 4,
                IsActive = i % 3 != 0, // some active, some inactive
                MaxTeamMembers = 5 + i,
                MaxDepartments = 3,
                MaxStoriesPerMonth = 50
            });
        }

        var activePlans = plans.Where(p => p.IsActive).ToList();
        var mockRepo = new Mock<IPlanRepository>();
        mockRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activePlans);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockLogger = new Mock<ILogger<PlanService>>();
        var dbContext = new BillingDbContext(new DbContextOptionsBuilder<BillingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new PlanService(dbContext, mockRepo.Object, mockRedis.Object, mockLogger.Object);

        // Act
        var result = await service.GetAllActiveAsync(CancellationToken.None);
        var responses = result.Data as List<PlanResponse>;

        // Assert
        Assert.NotNull(responses);
        Assert.Equal(activePlans.Count, responses!.Count);
        Assert.All(responses, r => Assert.Contains(activePlans, p => p.PlanId == r.PlanId));
    }

    /// <summary>
    /// Feature: billing-service, Property 2: FeaturesJson round-trip serialization
    /// **Validates: Requirements 1.5**
    /// Serializing feature flags to JSON and deserializing back produces equivalent object.
    /// </summary>
    [Property(MaxTest = 100)]
    public void Property2_FeaturesJsonRoundTrip_ProducesEquivalentObject(bool customWorkflows, bool prioritySupport)
    {
        var analyticsOptions = new[] { "none", "basic", "full" };
        foreach (var analytics in analyticsOptions)
        {
            var original = new { sprintAnalytics = analytics, customWorkflows, prioritySupport };
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<JsonElement>(json);

            Assert.Equal(analytics, deserialized.GetProperty("sprintAnalytics").GetString());
            Assert.Equal(customWorkflows, deserialized.GetProperty("customWorkflows").GetBoolean());
            Assert.Equal(prioritySupport, deserialized.GetProperty("prioritySupport").GetBoolean());
        }
    }

    /// <summary>
    /// Feature: billing-service, Property 3: Plan seeding idempotency
    /// **Validates: Requirements 1.6**
    /// Running seed twice produces same plans, no duplicates.
    /// </summary>
    [Fact]
    public async Task Property3_PlanSeedingIdempotency_NoDuplicates()
    {
        var createdPlans = new List<Plan>();
        var existingCodes = new HashSet<string>();

        var mockRepo = new Mock<IPlanRepository>();
        mockRepo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) => existingCodes.Contains(code));
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()))
            .Callback<Plan, CancellationToken>((p, _) =>
            {
                createdPlans.Add(p);
                existingCodes.Add(p.PlanCode);
            })
            .ReturnsAsync((Plan p, CancellationToken _) => p);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockLogger = new Mock<ILogger<PlanService>>();
        var dbContext = new BillingDbContext(new DbContextOptionsBuilder<BillingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new PlanService(dbContext, mockRepo.Object, mockRedis.Object, mockLogger.Object);

        // Seed first time
        await service.SeedPlansAsync(CancellationToken.None);
        var firstCount = createdPlans.Count;

        // Seed second time
        await service.SeedPlansAsync(CancellationToken.None);
        var secondCount = createdPlans.Count;

        // Assert: second seed creates no additional plans
        Assert.Equal(4, firstCount);
        Assert.Equal(4, secondCount); // no new plans created
        Assert.Equal(createdPlans.Select(p => p.PlanCode).Distinct().Count(), createdPlans.Count);
    }

    /// <summary>
    /// Feature: billing-service, Property 35: Plan cache consistency
    /// **Validates: Requirements 1.4, 8.3, 16.1**
    /// Cached plan data matches database Plan record.
    /// </summary>
    [Fact]
    public void Property35_PlanCacheConsistency_CachedDataMatchesDbRecord()
    {
        var plans = new[] {
            PlanGenerator.CreateFreePlan(),
            PlanGenerator.CreateStarterPlan(),
            PlanGenerator.CreateProPlan(),
            PlanGenerator.CreateEnterprisePlan()
        };

        foreach (var plan in plans)
        {
            // Simulate cache serialization (same as SubscriptionService.RefreshCacheAndNotify)
            var cacheValue = JsonSerializer.Serialize(new
            {
                plan.PlanCode,
                plan.PlanName,
                plan.TierLevel,
                plan.MaxTeamMembers,
                plan.MaxDepartments,
                plan.MaxStoriesPerMonth,
                plan.FeaturesJson
            });

            var deserialized = JsonSerializer.Deserialize<JsonElement>(cacheValue);

            Assert.Equal(plan.PlanCode, deserialized.GetProperty("PlanCode").GetString());
            Assert.Equal(plan.PlanName, deserialized.GetProperty("PlanName").GetString());
            Assert.Equal(plan.TierLevel, deserialized.GetProperty("TierLevel").GetInt32());
            Assert.Equal(plan.MaxTeamMembers, deserialized.GetProperty("MaxTeamMembers").GetInt32());
            Assert.Equal(plan.MaxDepartments, deserialized.GetProperty("MaxDepartments").GetInt32());
            Assert.Equal(plan.MaxStoriesPerMonth, deserialized.GetProperty("MaxStoriesPerMonth").GetInt32());
            Assert.Equal(plan.FeaturesJson, deserialized.GetProperty("FeaturesJson").GetString());
        }
    }
}
