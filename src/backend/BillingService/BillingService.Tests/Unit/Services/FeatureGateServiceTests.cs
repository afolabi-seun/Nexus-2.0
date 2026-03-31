using BillingService.Application.DTOs.FeatureGates;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Infrastructure.Services.FeatureGates;
using BillingService.Tests.Property.Generators;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Unit.Services;

public class FeatureGateServiceTests
{
    private FeatureGateService CreateService(
        Mock<ISubscriptionRepository> subRepo,
        Mock<IPlanRepository> planRepo,
        Mock<IDatabase> redisDb)
    {
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDb.Object);
        return new FeatureGateService(subRepo.Object, planRepo.Object, mockRedis.Object,
            new Mock<ILogger<FeatureGateService>>().Object);
    }

    [Fact]
    public async Task UnlimitedLimit_ReturnsAllowed()
    {
        var pro = PlanGenerator.CreateProPlan(); // MaxStoriesPerMonth = 0 (unlimited)
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(pro, orgId);

        var subRepo = new Mock<ISubscriptionRepository>();
        subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var planRepo = new Mock<IPlanRepository>();
        var redisDb = new Mock<IDatabase>();
        redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = CreateService(subRepo, planRepo, redisDb);
        var result = await service.CheckFeatureAsync(orgId, "max_stories_per_month", CancellationToken.None);
        var response = result as FeatureGateResponse;

        Assert.NotNull(response);
        Assert.True(response!.Allowed);
        Assert.Equal(0, response.Limit); // unlimited
    }

    [Fact]
    public async Task BooleanFeatureFlag_CustomWorkflows_ProPlan_Allowed()
    {
        var pro = PlanGenerator.CreateProPlan(); // customWorkflows = true
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(pro, orgId);

        var subRepo = new Mock<ISubscriptionRepository>();
        subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var planRepo = new Mock<IPlanRepository>();
        var redisDb = new Mock<IDatabase>();
        redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = CreateService(subRepo, planRepo, redisDb);
        var result = await service.CheckFeatureAsync(orgId, "custom_workflows", CancellationToken.None);
        var response = result as FeatureGateResponse;

        Assert.NotNull(response);
        Assert.True(response!.Allowed); // customWorkflows = true → limit = 0 (unlimited) → allowed
    }

    [Fact]
    public async Task BooleanFeatureFlag_CustomWorkflows_FreePlan_Denied()
    {
        var free = PlanGenerator.CreateFreePlan(); // customWorkflows = false
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(free, orgId);

        var subRepo = new Mock<ISubscriptionRepository>();
        subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var planRepo = new Mock<IPlanRepository>();
        var redisDb = new Mock<IDatabase>();
        redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = CreateService(subRepo, planRepo, redisDb);
        var result = await service.CheckFeatureAsync(orgId, "custom_workflows", CancellationToken.None);
        var response = result as FeatureGateResponse;

        Assert.NotNull(response);
        Assert.False(response!.Allowed); // customWorkflows = false → limit = -1 → denied
    }

    [Fact]
    public async Task NoSubscription_DefaultsToFreePlan()
    {
        var orgId = Guid.NewGuid();
        var freePlan = PlanGenerator.CreateFreePlan();

        var subRepo = new Mock<ISubscriptionRepository>();
        subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var planRepo = new Mock<IPlanRepository>();
        planRepo.Setup(r => r.GetByCodeAsync("free", It.IsAny<CancellationToken>()))
            .ReturnsAsync(freePlan);

        var redisDb = new Mock<IDatabase>();
        redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = CreateService(subRepo, planRepo, redisDb);
        var result = await service.CheckFeatureAsync(orgId, "max_team_members", CancellationToken.None);
        var response = result as FeatureGateResponse;

        Assert.NotNull(response);
        Assert.Equal(freePlan.MaxTeamMembers, response!.Limit);
    }
}
