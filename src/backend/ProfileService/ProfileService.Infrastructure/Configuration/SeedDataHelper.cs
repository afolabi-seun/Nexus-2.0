using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Configuration;

public static class SeedDataHelper
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProfileDbContext>>();

        logger.LogInformation("Seeding ProfileService reference data...");
        await SeedData.SeedAllAsync(context);
        logger.LogInformation("ProfileService reference data seeded.");
    }
}
