using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ErrorCodesClass = SecurityService.Domain.Exceptions.ErrorCodes;

namespace SecurityService.Infrastructure.Services.BackgroundServices;

/// <summary>
/// Validates on startup that all error codes defined in ErrorCodes.cs
/// exist in the UtilityService central error_code_entries registry.
/// Logs warnings for any missing codes — does not block startup.
/// </summary>
public class ErrorCodeValidationHostedService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ErrorCodeValidationHostedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ErrorCodeValidationHostedService(
        IHttpClientFactory httpClientFactory,
        ILogger<ErrorCodeValidationHostedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var localCodes = GetLocalErrorCodes();
            var registeredValues = await FetchRegisteredValuesAsync(cancellationToken);

            var missing = localCodes
                .Where(c => !registeredValues.Contains(c.Value))
                .ToList();

            if (missing.Count == 0)
            {
                _logger.LogInformation(
                    "ErrorCode validation passed. All {Count} local codes found in registry.",
                    localCodes.Count);
                return;
            }

            foreach (var (code, value) in missing)
            {
                _logger.LogWarning(
                    "ErrorCode {Code} ({Value}) is defined locally but missing from UtilityService registry.",
                    code, value);
            }

            _logger.LogWarning(
                "ErrorCode validation: {MissingCount}/{TotalCount} codes missing from registry.",
                missing.Count, localCodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ErrorCode validation skipped — could not reach UtilityService.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<HashSet<int>> FetchRegisteredValuesAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("UtilityService");
        var response = await client.GetAsync("/api/v1/error-codes", ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponseWrapper>(JsonOptions, ct);
        if (body?.Data is null) return new HashSet<int>();

        return body.Data.Select(e => e.Value).ToHashSet();
    }

    private static List<(string Code, int Value)> GetLocalErrorCodes()
    {
        var fields = typeof(ErrorCodesClass).GetFields(BindingFlags.Public | BindingFlags.Static);
        var stringFields = fields
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);
        var intFields = fields
            .Where(f => f.FieldType == typeof(int) && f.Name.EndsWith("Value"))
            .ToDictionary(f => f.Name.Replace("Value", ""), f => (int)f.GetValue(null)!);

        return stringFields
            .Where(kv => intFields.ContainsKey(kv.Key))
            .Select(kv => (Code: kv.Value, Value: intFields[kv.Key]))
            .ToList();
    }

    private class ApiResponseWrapper
    {
        public List<ErrorCodeEntry>? Data { get; set; }
    }

    private class ErrorCodeEntry
    {
        public string Code { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
