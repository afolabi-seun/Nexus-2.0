using System.Text.Json;
using BillingService.Application.DTOs.FeatureGates;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Domain.Interfaces.Services.Usage;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Services.FeatureGates;
using BillingService.Infrastructure.Services.ServiceClients;
using BillingService.Infrastructure.Services.Subscriptions;
using BillingService.Tests.Property.Generators;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Property;

/// <summary>
/// Property-based tests for feature gates.
/// </summary>
public class FeatureGatePropertyTests
{
    /// <summary>
    /// Feature: billing-service, Property 17: Trialing subscriptions grant full plan access
    /// **Validates: Requirements 7.3**
    /// </summary>
    [Fact]
    public async Task Property17_TrialingSubscriptionsGrantFullPlanAccess()
    {
        var pro = PlanGenerator.CreateProPlan();
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateTrialing(pro, orgId);

        var mockSubRepo = new Mock<ISubscriptionRepository>();
        mockSubRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var mockPlanRepo = new Mock<IPlanRepository>();
        mockPlanRepo.Setup(r => r.GetByIdAsync(pro.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pro);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        // Return empty from cache to force DB lookup
        mockDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        var mockLogger = new Mock<ILogger<FeatureGateService>>();
        var service = new FeatureGateService(mockSubRepo.Object, mockPlanRepo.Object, mockRedis.Object, mockLogger.Object);

        // Test numeric features — pro plan has 100 team members, 0 (unlimited) departments/stories
        var result = await service.CheckFeatureAsync(orgId, "max_team_members", CancellationToken.None);
        var response = result as FeatureGateResponse;
        Assert.NotNull(response);
        Assert.True(response!.Allowed); // 0 usage < 100 limit

        // Test unlimited feature (0 means unlimited)
        result = await service.CheckFeatureAsync(orgId, "max_stories_per_month", CancellationToken.None);
        response = result as FeatureGateResponse;
        Assert.NotNull(response);
        Assert.True(response!.Allowed); // 0 limit = unlimited
    }

    /// <summary>
    /// Feature: billing-service, Property 18: Feature gate response correctness
    /// **Validates: Requirements 8.1, 8.5, 8.6**
    /// </summary>
    [Fact]
    public async Task Property18_FeatureGateResponseCorrectness()
    {
        var freePlan = PlanGenerator.CreateFreePlan(); // MaxTeamMembers = 5
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(freePlan, orgId);

        var mockSubRepo = new Mock<ISubscriptionRepository>();
        mockSubRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var mockPlanRepo = new Mock<IPlanRepository>();
        mockPlanRepo.Setup(r => r.GetByIdAsync(freePlan.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(freePlan);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        // Simulate usage below limit
        mockDb.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString().Contains("plan:")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        mockDb.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString().Contains("usage:")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("3")); // 3 < 5 limit

        var mockLogger = new Mock<ILogger<FeatureGateService>>();
        var service = new FeatureGateService(mockSubRepo.Object, mockPlanRepo.Object, mockRedis.Object, mockLogger.Object);

        var result = await service.CheckFeatureAsync(orgId, "max_team_members", CancellationToken.None);
        var response = result as FeatureGateResponse;
        Assert.NotNull(response);
        Assert.True(response!.Allowed);
        Assert.Equal(3, response.CurrentUsage);
        Assert.Equal(5, response.Limit);

        // Now simulate usage at limit
        mockDb.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString().Contains("usage:")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("5")); // 5 >= 5 limit

        result = await service.CheckFeatureAsync(orgId, "max_team_members", CancellationToken.None);
        response = result as FeatureGateResponse;
        Assert.NotNull(response);
        Assert.False(response!.Allowed);
        Assert.Equal(5, response.CurrentUsage);
        Assert.Equal(5, response.Limit);
    }

    /// <summary>
    /// Feature: billing-service, Property 36: ProfileService unavailability still updates Redis
    /// **Validates: Requirements 12.5**
    /// </summary>
    [Fact]
    public async Task Property36_ProfileServiceUnavailabilityStillUpdatesRedis()
    {
        var freePlan = PlanGenerator.CreateFreePlan();
        var orgId = Guid.NewGuid();

        var mockSubRepo = new Mock<ISubscriptionRepository>();
        mockSubRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        mockSubRepo.Setup(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        var mockPlanRepo = new Mock<IPlanRepository>();
        mockPlanRepo.Setup(r => r.GetByIdAsync(freePlan.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(freePlan);

        var mockStripe = new Mock<IStripePaymentService>();
        var mockUsage = new Mock<IUsageService>();
        var mockOutbox = new Mock<IOutboxService>();

        // ProfileService throws
        var mockProfile = new Mock<IProfileServiceClient>();
        mockProfile.Setup(p => p.UpdateOrganizationPlanTierAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("ProfileService unavailable"));

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        var mockLogger = new Mock<ILogger<SubscriptionService>>();
        var dbContext = new BillingDbContext(new DbContextOptionsBuilder<BillingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new SubscriptionService(
            dbContext,
            mockSubRepo.Object, mockPlanRepo.Object, mockStripe.Object,
            mockUsage.Object, mockOutbox.Object, mockProfile.Object,
            mockRedis.Object, mockLogger.Object);

        // Should not throw even though ProfileService is down
        await service.CreateAsync(orgId,
            new BillingService.Application.DTOs.Subscriptions.CreateSubscriptionRequest(freePlan.PlanId, null),
            CancellationToken.None);

        // Redis should still have been called
        Assert.Contains(mockDb.Invocations,
            inv => inv.Method.Name == "StringSetAsync" &&
                   inv.Arguments[0].ToString()!.Contains($"plan:{orgId}"));
    }
}
