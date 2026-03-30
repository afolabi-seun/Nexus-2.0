using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.Validators;

namespace BillingService.Tests.Unit.Validators;

public class DowngradeSubscriptionRequestValidatorTests
{
    private readonly DowngradeSubscriptionRequestValidator _validator = new();

    [Fact]
    public void EmptyGuid_Fails()
    {
        var result = _validator.Validate(new DowngradeSubscriptionRequest(Guid.Empty));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPlanId");
    }

    [Fact]
    public void ValidGuid_Passes()
    {
        var result = _validator.Validate(new DowngradeSubscriptionRequest(Guid.NewGuid()));
        Assert.True(result.IsValid);
    }
}
