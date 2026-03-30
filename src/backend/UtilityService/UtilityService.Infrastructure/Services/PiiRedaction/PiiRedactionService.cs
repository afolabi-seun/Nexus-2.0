using System.Text.RegularExpressions;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Infrastructure.Services.PiiRedaction;

public class PiiRedactionService : IPiiRedactionService
{
    private static readonly Regex EmailPattern = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex Ipv4Pattern = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled);
    private static readonly Regex Ipv6Pattern = new(@"([0-9a-fA-F]{1,4}:){2,7}[0-9a-fA-F]{1,4}", RegexOptions.Compiled);

    public string Redact(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = EmailPattern.Replace(input, "[REDACTED]");
        result = Ipv4Pattern.Replace(result, "[REDACTED]");
        result = Ipv6Pattern.Replace(result, "[REDACTED]");
        return result;
    }
}
