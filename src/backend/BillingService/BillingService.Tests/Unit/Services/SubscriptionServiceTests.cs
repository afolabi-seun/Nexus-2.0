using BillingService.Domain.Results;
using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Domain.Interfaces.Services.Usage;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Services.ServiceClients;
using BillingService.Infrastructure.Services.Subscriptions;
using BillingService.Tests.Property.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Unit.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IStripePaymentService> _stripeSvc = new();
    private readonly Mock<IUsageService> _usageSvc = new();
    private readonly Mock<IOutboxService> _outboxSvc = new();
    private readonly Mock<IProfileServiceClient> _profileClient = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly Mock<ILogger<SubscriptionService>> _logger = new();

    private SubscriptionService CreateService()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);
        var dbContext = new BillingDbContext(new DbContextOptionsBuilder<BillingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return new SubscriptionService(
            dbContext,
            _subRepo.Object, _planRepo.Object, _stripeSvc.Object,
            _usageSvc.Object, _outboxSvc.Object, _profileClient.Object,
            _redis.Object, _logger.Object);
    }

    [Fact]
    public async Task DoubleCancellation_ThrowsAlreadyCancelled()
    {
        var plan = PlanGenerator.CreateStarterPlan();
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(plan, orgId);
        sub.Status = SubscriptionStatus.Cancelled;

        _subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var service = CreateService();
        await Assert.ThrowsAsync<SubscriptionAlreadyCancelledException>(
            () => service.CancelAsync(orgId, CancellationToken.None));
    }

    [Fact]
    public async Task NoSubscription_ThrowsNoActiveSubscription()
    {
        var orgId = Guid.NewGuid();
        _subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var service = CreateService();
        await Assert.ThrowsAsync<NoActiveSubscriptionException>(
            () => service.CancelAsync(orgId, CancellationToken.None));
    }

    [Fact]
    public async Task UpgradeFromFreeToStarter_CreatesStripeSubscription()
    {
        var free = PlanGenerator.CreateFreePlan();
        var starter = PlanGenerator.CreateStarterPlan();
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(free, orgId);

        _subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);
        _planRepo.Setup(r => r.GetByIdAsync(starter.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(starter);
        _stripeSvc.Setup(s => s.CreateSubscriptionAsync(orgId, starter.PlanCode, starter.PriceMonthly, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("cus_new", "sub_new"));

        var service = CreateService();
        await service.UpgradeAsync(orgId, new UpgradeSubscriptionRequest(starter.PlanId), CancellationToken.None);

        Assert.Equal(starter.PlanId, sub.PlanId);
        Assert.Equal("cus_new", sub.ExternalCustomerId);
        Assert.Equal("sub_new", sub.ExternalSubscriptionId);
    }

    [Fact]
    public async Task StripeFailure_ThrowsPaymentProviderException()
    {
        var starter = PlanGenerator.CreateStarterPlan();
        var orgId = Guid.NewGuid();

        _subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _planRepo.Setup(r => r.GetByIdAsync(starter.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(starter);
        _stripeSvc.Setup(s => s.CreateSubscriptionAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentProviderException("Stripe error: Card declined"));

        var service = CreateService();
        var ex = await Assert.ThrowsAsync<PaymentProviderException>(
            () => service.CreateAsync(orgId, new CreateSubscriptionRequest(starter.PlanId, "pm_test"), CancellationToken.None));

        Assert.Contains("Card declined", ex.Message);
        _subRepo.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrent_ReturnsSubscriptionWithPlanAndUsage()
    {
        var plan = PlanGenerator.CreateStarterPlan();
        var orgId = Guid.NewGuid();
        var sub = SubscriptionGenerator.CreateActive(plan, orgId);

        _subRepo.Setup(r => r.GetByOrganizationIdAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);
        _planRepo.Setup(r => r.GetByIdAsync(plan.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _usageSvc.Setup(u => u.GetUsageAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<object>.Ok(new UsageResponse(new List<UsageMetric>
            {
                new("active_members", 5, 25, 20.0),
                new("stories_created", 10, 500, 2.0),
                new("storage_bytes", 0, 0, 0)
            })));

        var service = CreateService();
        var result = await service.GetCurrentAsync(orgId, CancellationToken.None);
        var detail = result.Data as SubscriptionDetailResponse;

        Assert.NotNull(detail);
        Assert.Equal(sub.SubscriptionId, detail!.Subscription.SubscriptionId);
        Assert.Equal(plan.PlanName, detail.Plan.PlanName);
        Assert.Equal(3, detail.Usage.Metrics.Count);
    }
}
