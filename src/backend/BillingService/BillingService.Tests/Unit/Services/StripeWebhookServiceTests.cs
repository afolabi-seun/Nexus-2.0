using BillingService.Domain.Entities;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.StripeEvents;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Infrastructure.Services.ServiceClients;
using BillingService.Infrastructure.Services.Stripe;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace BillingService.Tests.Unit.Services;

public class StripeWebhookServiceTests
{
    private readonly Mock<IStripePaymentService> _stripeSvc = new();
    private readonly Mock<IStripeEventRepository> _eventRepo = new();
    private readonly Mock<ISubscriptionRepository> _subRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IOutboxService> _outboxSvc = new();
    private readonly Mock<IProfileServiceClient> _profileClient = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<ILogger<StripeWebhookService>> _logger = new();

    private StripeWebhookService CreateService()
    {
        var mockDb = new Mock<IDatabase>();
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        return new StripeWebhookService(
            _stripeSvc.Object, _eventRepo.Object, _subRepo.Object,
            _planRepo.Object, _outboxSvc.Object, _profileClient.Object,
            _redis.Object, _logger.Object);
    }

    [Fact]
    public async Task InvalidSignature_ThrowsInvalidWebhookSignature()
    {
        object? outEvent = null;
        _stripeSvc.Setup(s => s.VerifyWebhookSignature("payload", "bad", out outEvent))
            .Returns(false);

        var service = CreateService();
        await Assert.ThrowsAsync<InvalidWebhookSignatureException>(
            () => service.ProcessWebhookAsync("payload", "bad", CancellationToken.None));
    }

    [Fact]
    public async Task InvalidPayload_ThrowsInvalidWebhookPayload()
    {
        // VerifyWebhookSignature returns true but with non-Stripe.Event object
        object? outEvent = "not a stripe event";
        _stripeSvc.Setup(s => s.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), out outEvent))
            .Returns(true);

        var service = CreateService();
        await Assert.ThrowsAsync<InvalidWebhookPayloadException>(
            () => service.ProcessWebhookAsync("payload", "sig", CancellationToken.None));
    }

    [Fact]
    public async Task DuplicateEvent_SkipsProcessing()
    {
        var stripeEvent = new Stripe.Event { Id = "evt_dup", Type = "invoice.payment_succeeded" };
        object? outEvent = stripeEvent;

        _stripeSvc.Setup(s => s.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), out outEvent))
            .Returns(true);
        _eventRepo.Setup(r => r.ExistsAsync("evt_dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();
        await service.ProcessWebhookAsync("payload", "sig", CancellationToken.None);

        _eventRepo.Verify(r => r.CreateAsync(It.IsAny<StripeEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidNewEvent_RecordsAndPublishes()
    {
        var stripeEvent = new Stripe.Event
        {
            Id = "evt_new",
            Type = "invoice.payment_succeeded",
            Data = new Stripe.EventData { Object = new Stripe.Invoice { SubscriptionId = "sub_x" } }
        };
        object? outEvent = stripeEvent;

        _stripeSvc.Setup(s => s.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<string>(), out outEvent))
            .Returns(true);
        _eventRepo.Setup(r => r.ExistsAsync("evt_new", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();
        await service.ProcessWebhookAsync("payload", "sig", CancellationToken.None);

        _eventRepo.Verify(r => r.CreateAsync(
            It.Is<StripeEvent>(e => e.StripeEventId == "evt_new"),
            It.IsAny<CancellationToken>()), Times.Once);
        _outboxSvc.Verify(o => o.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
