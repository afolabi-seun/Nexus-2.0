using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Infrastructure.Services.Usage;
using BillingService.Tests.Property.Generators;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Unit.Services;

public class UsageServiceTests
{
    [Fact]
    public async Task NoSubscription_DefaultsToFreeLimits()
    {
        var orgId = Guid.NewGuid();
        var freePlan = PlanGenerator.CreateFreePlan();

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        mockDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("0"));

        var mockSubRepo = new Mock<ISubscriptionRepository>();
        mockSubRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var mockPlanRepo = new Mock<IPlanRepository>();
        mockPlanRepo.Setup(r => r.GetByCodeAsync("free", It.IsAny<CancellationToken>()))
            .ReturnsAsync(freePlan);

        var mockLogger = new Mock<ILogger<UsageService>>();
        var service = new UsageService(mockRedis.Object, mockSubRepo.Object, mockPlanRepo.Object, mockLogger.Object);

        var result = await service.GetUsageAsync(orgId, CancellationToken.None);
        var response = result as UsageResponse;

        Assert.NotNull(response);
        var membersMetric = response!.Metrics.First(m => m.MetricName == MetricName.ActiveMembers);
        Assert.Equal(freePlan.MaxTeamMembers, membersMetric.Limit);
    }

    [Fact]
    public async Task IncrementAsync_CallsRedisIncrBy()
    {
        var orgId = Guid.NewGuid();
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        var mockSubRepo = new Mock<ISubscriptionRepository>();
        var mockPlanRepo = new Mock<IPlanRepository>();
        var mockLogger = new Mock<ILogger<UsageService>>();

        var service = new UsageService(mockRedis.Object, mockSubRepo.Object, mockPlanRepo.Object, mockLogger.Object);
        await service.IncrementAsync(orgId, MetricName.StoriesCreated, 5, CancellationToken.None);

        mockDb.Verify(d => d.StringIncrementAsync(
            It.Is<RedisKey>(k => k.ToString() == $"usage:{orgId}:{MetricName.StoriesCreated}"),
            5, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetUsage_ReturnsAllThreeMetrics()
    {
        var plan = PlanGenerator.CreateStarterPlan();
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(plan, orgId);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        mockDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("10"));

        var mockSubRepo = new Mock<ISubscriptionRepository>();
        mockSubRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var mockPlanRepo = new Mock<IPlanRepository>();
        var mockLogger = new Mock<ILogger<UsageService>>();

        var service = new UsageService(mockRedis.Object, mockSubRepo.Object, mockPlanRepo.Object, mockLogger.Object);
        var result = await service.GetUsageAsync(orgId, CancellationToken.None);
        var response = result as UsageResponse;

        Assert.NotNull(response);
        Assert.Equal(3, response!.Metrics.Count);
        Assert.Contains(response.Metrics, m => m.MetricName == MetricName.ActiveMembers);
        Assert.Contains(response.Metrics, m => m.MetricName == MetricName.StoriesCreated);
        Assert.Contains(response.Metrics, m => m.MetricName == MetricName.StorageBytes);
    }
}
