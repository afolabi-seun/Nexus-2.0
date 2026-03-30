using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UtilityService.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Uses the same DATABASE_URL env var as AppSettings.FromEnvironment(),
/// but with a localhost fallback since the full AppSettings requires
/// Redis/JWT vars that aren't available at design time.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UtilityDbContext>
{
    public UtilityDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=nexus_utility;Username=postgres;Password=pass.123";

        var optionsBuilder = new DbContextOptionsBuilder<UtilityDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new UtilityDbContext(optionsBuilder.Options);
    }
}
