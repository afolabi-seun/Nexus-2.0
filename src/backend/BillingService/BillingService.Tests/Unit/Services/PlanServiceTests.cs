using BillingService.Application.DTOs.Plans;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Infrastructure.Services.Plans;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Unit.Services;

public class PlanServiceTests
{
    [Fact]
    public async Task SeedPlans_CreatesExactly4Plans()
    {
        var createdPlans = new List<Plan>();
        var mockRepo = new Mock<IPlanRepository>();
        mockRepo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()))
            .Callback<Plan, CancellationToken>((p, _) => createdPlans.Add(p))
            .Returns(Task.CompletedTask);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockLogger = new Mock<ILogger<PlanService>>();
        var service = new PlanService(mockRepo.Object, mockRedis.Object, mockLogger.Object);

        await service.SeedPlansAsync(CancellationToken.None);

        Assert.Equal(4, createdPlans.Count);
        Assert.Contains(createdPlans, p => p.PlanCode == "free" && p.TierLevel == 0);
        Assert.Contains(createdPlans, p => p.PlanCode == "starter" && p.TierLevel == 1);
        Assert.Contains(createdPlans, p => p.PlanCode == "pro" && p.TierLevel == 2);
        Assert.Contains(createdPlans, p => p.PlanCode == "enterprise" && p.TierLevel == 3);
    }

    [Fact]
    public async Task SeedPlans_MatchesRequirementValues()
    {
        var createdPlans = new List<Plan>();
        var mockRepo = new Mock<IPlanRepository>();
        mockRepo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()))
            .Callback<Plan, CancellationToken>((p, _) => createdPlans.Add(p))
            .Returns(Task.CompletedTask);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockLogger = new Mock<ILogger<PlanService>>();
        var service = new PlanService(mockRepo.Object, mockRedis.Object, mockLogger.Object);

        await service.SeedPlansAsync(CancellationToken.None);

        var free = createdPlans.First(p => p.PlanCode == "free");
        Assert.Equal(5, free.MaxTeamMembers);
        Assert.Equal(3, free.MaxDepartments);
        Assert.Equal(50, free.MaxStoriesPerMonth);
        Assert.Equal(0m, free.PriceMonthly);

        var starter = createdPlans.First(p => p.PlanCode == "starter");
        Assert.Equal(25, starter.MaxTeamMembers);
        Assert.Equal(5, starter.MaxDepartments);
        Assert.Equal(500, starter.MaxStoriesPerMonth);
        Assert.Equal(29.00m, starter.PriceMonthly);
        Assert.Equal(290.00m, starter.PriceYearly);

        var pro = createdPlans.First(p => p.PlanCode == "pro");
        Assert.Equal(100, pro.MaxTeamMembers);
        Assert.Equal(0, pro.MaxDepartments); // unlimited
        Assert.Equal(0, pro.MaxStoriesPerMonth); // unlimited
        Assert.Equal(99.00m, pro.PriceMonthly);

        var enterprise = createdPlans.First(p => p.PlanCode == "enterprise");
        Assert.Equal(0, enterprise.MaxTeamMembers); // unlimited
        Assert.Equal(299.00m, enterprise.PriceMonthly);
        Assert.Equal(2990.00m, enterprise.PriceYearly);
    }

    [Fact]
    public async Task GetAllActive_ReturnsOnlyActivePlans()
    {
        var plans = new List<Plan>
        {
            new() { PlanId = Guid.NewGuid(), PlanName = "Free", PlanCode = "free", TierLevel = 0, IsActive = true },
            new() { PlanId = Guid.NewGuid(), PlanName = "Starter", PlanCode = "starter", TierLevel = 1, IsActive = true }
        };

        var mockRepo = new Mock<IPlanRepository>();
        mockRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockLogger = new Mock<ILogger<PlanService>>();
        var service = new PlanService(mockRepo.Object, mockRedis.Object, mockLogger.Object);

        var result = await service.GetAllActiveAsync(CancellationToken.None);
        var responses = result as List<PlanResponse>;

        Assert.NotNull(responses);
        Assert.Equal(2, responses!.Count);
    }
}
