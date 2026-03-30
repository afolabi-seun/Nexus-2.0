using BillingService.Application.DTOs.Subscriptions;
using BillingService.Application.Validators;

namespace BillingService.Tests.Unit.Validators;

public class CreateSubscriptionRequestValidatorTests
{
    private readonly CreateSubscriptionRequestValidator _validator = new();

    [Fact]
    public void EmptyGuid_Fails()
    {
        var result = _validator.Validate(new CreateSubscriptionRequest(Guid.Empty, null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PlanId");
    }

    [Fact]
    public void ValidGuid_Passes()
    {
        var result = _validator.Validate(new CreateSubscriptionRequest(Guid.NewGuid(), null));
        Assert.True(result.IsValid);
    }
}
