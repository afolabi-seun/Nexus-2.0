using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Configuration;

public static class DatabaseMigrationHelper
{
    public static void ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WorkDbContext>>();

        var providerName = context.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("In-memory database detected. Calling EnsureCreated.");
            context.Database.EnsureCreated();
            return;
        }

        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count, string.Join(", ", pendingMigrations));
            context.Database.Migrate();
        }
        else
        {
            logger.LogInformation("No pending migrations.");
        }
    }
}
