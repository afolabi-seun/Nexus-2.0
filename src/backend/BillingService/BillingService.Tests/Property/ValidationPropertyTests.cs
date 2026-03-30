using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.DTOs.Usage;
using BillingService.Application.Validators;
using BillingService.Domain.Enums;
using FsCheck;
using FsCheck.Xunit;

namespace BillingService.Tests.Property;

/// <summary>
/// Property-based tests for validation.
/// </summary>
public class ValidationPropertyTests
{
    /// <summary>
    /// Feature: billing-service, Property 29: Validation failures return field errors
    /// **Validates: Requirements 13.3, 18.1, 18.2, 18.3**
    /// FluentValidation failure → VALIDATION_ERROR + field details.
    /// </summary>
    [Fact]
    public void Property29_CreateSubscriptionValidation_EmptyGuidFails()
    {
        var validator = new CreateSubscriptionRequestValidator();
        var request = new CreateSubscriptionRequest(Guid.Empty, null);
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PlanId");
    }

    [Fact]
    public void Property29_UpgradeSubscriptionValidation_EmptyGuidFails()
    {
        var validator = new UpgradeSubscriptionRequestValidator();
        var request = new UpgradeSubscriptionRequest(Guid.Empty);
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPlanId");
    }

    [Fact]
    public void Property29_DowngradeSubscriptionValidation_EmptyGuidFails()
    {
        var validator = new DowngradeSubscriptionRequestValidator();
        var request = new DowngradeSubscriptionRequest(Guid.Empty);
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPlanId");
    }

    [Property(MaxTest = 100)]
    public void Property29_ValidGuidsPassValidation()
    {
        var planId = Guid.NewGuid();

        var createValidator = new CreateSubscriptionRequestValidator();
        Assert.True(createValidator.Validate(new CreateSubscriptionRequest(planId, null)).IsValid);

        var upgradeValidator = new UpgradeSubscriptionRequestValidator();
        Assert.True(upgradeValidator.Validate(new UpgradeSubscriptionRequest(planId)).IsValid);

        var downgradeValidator = new DowngradeSubscriptionRequestValidator();
        Assert.True(downgradeValidator.Validate(new DowngradeSubscriptionRequest(planId)).IsValid);
    }

    [Property(MaxTest = 100)]
    public void Property29_IncrementUsageValidation(NonEmptyString metricName, int value)
    {
        var validator = new IncrementUsageRequestValidator();
        var request = new IncrementUsageRequest(metricName.Get, value);
        var result = validator.Validate(request);

        var isValidMetric = MetricName.IsValid(metricName.Get);
        var isPositiveValue = value > 0;

        if (isValidMetric && isPositiveValue)
            Assert.True(result.IsValid);
        else
            Assert.False(result.IsValid);
    }
}
