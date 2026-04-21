using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ErrorCodesClass = UtilityService.Domain.Exceptions.ErrorCodes;
using UtilityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace UtilityService.Infrastructure.Services.BackgroundServices;

/// <summary>
/// Validates on startup that all error codes defined in ErrorCodes.cs
/// exist in the central error_code_entries registry.
/// Logs warnings for any missing codes — does not block startup.
/// </summary>
public class ErrorCodeValidationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorCodeValidationHostedService> _logger;

    public ErrorCodeValidationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ErrorCodeValidationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UtilityDbContext>();

            var localCodes = GetLocalErrorCodes();
            var registeredValues = (await db.ErrorCodeEntries
                .IgnoreQueryFilters()
                .Select(e => e.Value)
                .ToListAsync(cancellationToken))
                .ToHashSet();

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
                    "ErrorCode {Code} ({Value}) is defined in UtilityService.ErrorCodes but missing from error_code_entries registry.",
                    code, value);
            }

            _logger.LogWarning(
                "ErrorCode validation: {MissingCount}/{TotalCount} codes missing from registry. Run seed migration to sync.",
                missing.Count, localCodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ErrorCode validation skipped — could not query registry.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
}
