using UtilityService.Infrastructure.Services.PiiRedaction;

namespace UtilityService.Tests.Services;

public class PiiRedactionServiceTests
{
    private readonly PiiRedactionService _sut = new();

    [Fact]
    public void Redact_EmailAddress_ReplacesWithRedacted()
    {
        var input = "Contact user@example.com for details";
        var result = _sut.Redact(input);
        Assert.DoesNotContain("user@example.com", result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void Redact_Ipv4Address_ReplacesWithRedacted()
    {
        var input = "Request from 192.168.1.100 was blocked";
        var result = _sut.Redact(input);
        Assert.DoesNotContain("192.168.1.100", result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void Redact_Ipv6Address_ReplacesWithRedacted()
    {
        var input = "Client at 2001:0db8:85a3:0000:0000:8a2e:0370:7334 connected";
        var result = _sut.Redact(input);
        Assert.DoesNotContain("2001:0db8:85a3:0000:0000:8a2e:0370:7334", result);
        Assert.Contains("[REDACTED]", result);
    }

    [Fact]
    public void Redact_NoPii_ReturnsInputUnchanged()
    {
        var input = "This is a normal log message with no PII";
        var result = _sut.Redact(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Redact_MixedContent_RedactsAllPiiPatterns()
    {
        var input = "User admin@corp.io from 10.0.0.1 and fe80:0000:0000:0000:abcd:1234:5678:9abc logged in";
        var result = _sut.Redact(input);
        Assert.DoesNotContain("admin@corp.io", result);
        Assert.DoesNotContain("10.0.0.1", result);
        Assert.DoesNotContain("fe80:", result);
        Assert.Contains("User [REDACTED] from [REDACTED] and [REDACTED] logged in", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Redact_NullOrEmpty_ReturnsSameValue(string? input)
    {
        var result = _sut.Redact(input!);
        Assert.Equal(input, result);
    }
}
