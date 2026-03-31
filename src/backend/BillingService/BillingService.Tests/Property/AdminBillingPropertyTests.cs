using BillingService.Api.Attributes;
using BillingService.Api.Middleware;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories.UsageRecords;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Repositories.Plans;
using BillingService.Infrastructure.Repositories.Subscriptions;
using BillingService.Infrastructure.Services.AdminBilling;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Tests.Property;

/// <summary>
/// Property-based tests for Platform Admin Billing Management.
/// </summary>
public class AdminBillingPropertyTests
{
    // ---------------------------------------------------------------
    // Feature: platform-admin-billing, Property 1: PlatformAdmin authorization gate
    // **Validates: Requirements 1.4, 9.2, 9.3**
    // For any non-PlatformAdmin role string, the RoleAuthorizationMiddleware
    // should return 403 when the endpoint has PlatformAdminAttribute.
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public async void Property1_NonPlatformAdminRole_Returns403(NonEmptyString roleInput)
    {
        var role = roleInput.Get;
        if (role == "PlatformAdmin") return; // skip the one valid role

        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RoleAuthorizationMiddleware(next);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", "user1") },
                "Bearer"));
        httpContext.Items["roleName"] = role;
        httpContext.Items["CorrelationId"] = Guid.NewGuid().ToString();

        // Set up endpoint with PlatformAdminAttribute metadata
        var endpoint = new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(new PlatformAdminAttribute()),
            displayName: "TestEndpoint");
        httpContext.SetEndpoint(endpoint);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal(403, httpContext.Response.StatusCode);
        Assert.False(nextCalled, $"Next delegate should NOT be called for role '{role}'");
    }

    // ---------------------------------------------------------------
    // Feature: platform-admin-billing, Property 6: Override sets Active status and current period
    // **Validates: Requirements 3.1**
    // For any org and valid plan, after override the subscription should have
    // status="Active", correct planId, and currentPeriodStart within delta of UTC now.
    // ---------------------------------------------------------------

    private static BillingDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new BillingDbContext(options);
    }

    [Property(MaxTest = 100)]
    public async void Property6_OverrideSubscription_SetsActiveStatusAndCurrentPeriod(Guid orgId, NonEmptyString planNameInput)
    {
        if (orgId == Guid.Empty) return; // skip empty guid

        var planName = planNameInput.Get;

        // Arrange — unique in-memory DB per test iteration
        var dbName = $"Property6_{Guid.NewGuid()}";
        using var dbContext = CreateInMemoryDbContext(dbName);

        var plan = new Plan
        {
            PlanId = Guid.NewGuid(),
            PlanName = planName.Length > 50 ? planName[..50] : planName,
            PlanCode = $"CODE_{Guid.NewGuid():N}"[..20],
            TierLevel = 1,
            MaxTeamMembers = 10,
            MaxDepartments = 5,
            MaxStoriesPerMonth = 100,
            PriceMonthly = 29.00m,
            PriceYearly = 290.00m,
            IsActive = true
        };
        dbContext.Plans.Add(plan);
        await dbContext.SaveChangesAsync();

        var planRepo = new PlanRepository(dbContext);
        var subRepo = new SubscriptionRepository(dbContext);
        var mockStripe = new Mock<IStripePaymentService>();
        var mockOutbox = new Mock<IOutboxService>();
        var mockUsageRepo = new Mock<IUsageRecordRepository>();
        var mockLogger = new Mock<ILogger<AdminBillingService>>();

        var service = new AdminBillingService(
            dbContext, planRepo, subRepo, mockUsageRepo.Object,
            mockStripe.Object, mockOutbox.Object, mockLogger.Object);

        var adminId = Guid.NewGuid();
        var beforeCall = DateTime.UtcNow;

        // Act
        await service.OverrideSubscriptionAsync(orgId, plan.PlanId, "test reason", adminId, CancellationToken.None);

        var afterCall = DateTime.UtcNow;

        // Assert — read back from DB
        var sub = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId);

        Assert.NotNull(sub);
        Assert.Equal(SubscriptionStatus.Active, sub!.Status);
        Assert.Equal(plan.PlanId, sub.PlanId);
        Assert.InRange(sub.CurrentPeriodStart, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    // ---------------------------------------------------------------
    // Feature: platform-admin-billing, Property 9: Admin cancellation is immediate
    // **Validates: Requirements 4.1**
    // For any org with Active/Trialing subscription, after admin cancel the
    // subscription should have status="Cancelled" and cancelledAt within delta of UTC now.
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public async void Property9_AdminCancellation_SetsImmediateCancelledStatus(Guid orgId, bool useActiveStatus)
    {
        if (orgId == Guid.Empty) return; // skip empty guid

        var status = useActiveStatus ? SubscriptionStatus.Active : SubscriptionStatus.Trialing;

        // Arrange — unique in-memory DB per test iteration
        var dbName = $"Property9_{Guid.NewGuid()}";
        using var dbContext = CreateInMemoryDbContext(dbName);

        var plan = new Plan
        {
            PlanId = Guid.NewGuid(),
            PlanName = "TestPlan",
            PlanCode = $"CODE_{Guid.NewGuid():N}"[..20],
            TierLevel = 1,
            MaxTeamMembers = 10,
            MaxDepartments = 5,
            MaxStoriesPerMonth = 100,
            PriceMonthly = 29.00m,
            PriceYearly = 290.00m,
            IsActive = true
        };
        dbContext.Plans.Add(plan);

        var subscription = new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            OrganizationId = orgId,
            PlanId = plan.PlanId,
            Status = status,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-10),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(20),
            DateCreated = DateTime.UtcNow.AddDays(-10),
            DateUpdated = DateTime.UtcNow.AddDays(-10)
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var planRepo = new PlanRepository(dbContext);
        var subRepo = new SubscriptionRepository(dbContext);
        var mockStripe = new Mock<IStripePaymentService>();
        var mockOutbox = new Mock<IOutboxService>();
        var mockUsageRepo = new Mock<IUsageRecordRepository>();
        var mockLogger = new Mock<ILogger<AdminBillingService>>();

        var service = new AdminBillingService(
            dbContext, planRepo, subRepo, mockUsageRepo.Object,
            mockStripe.Object, mockOutbox.Object, mockLogger.Object);

        var adminId = Guid.NewGuid();
        var beforeCall = DateTime.UtcNow;

        // Act
        await service.AdminCancelSubscriptionAsync(orgId, "policy violation", adminId, CancellationToken.None);

        var afterCall = DateTime.UtcNow;

        // Assert — read back from DB
        var sub = await dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId);

        Assert.NotNull(sub);
        Assert.Equal(SubscriptionStatus.Cancelled, sub!.Status);
        Assert.NotNull(sub.CancelledAt);
        Assert.InRange(sub.CancelledAt!.Value, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }
}
