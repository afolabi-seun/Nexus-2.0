using BillingService.Application.DTOs.Usage;
using BillingService.Application.Validators;

namespace BillingService.Tests.Unit.Validators;

public class IncrementUsageRequestValidatorTests
{
    private readonly IncrementUsageRequestValidator _validator = new();

    [Theory]
    [InlineData("invalid_metric", 1)]
    [InlineData("", 1)]
    [InlineData("foo", 5)]
    public void InvalidMetricName_Fails(string metricName, long value)
    {
        var result = _validator.Validate(new IncrementUsageRequest(metricName, value));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MetricName");
    }

    [Theory]
    [InlineData("active_members", 0)]
    [InlineData("stories_created", -1)]
    [InlineData("storage_bytes", -100)]
    public void ZeroOrNegativeValue_Fails(string metricName, long value)
    {
        var result = _validator.Validate(new IncrementUsageRequest(metricName, value));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Value");
    }

    [Theory]
    [InlineData("active_members", 1)]
    [InlineData("stories_created", 100)]
    [InlineData("storage_bytes", 1024)]
    public void ValidInputs_Pass(string metricName, long value)
    {
        var result = _validator.Validate(new IncrementUsageRequest(metricName, value));
        Assert.True(result.IsValid);
    }
}
