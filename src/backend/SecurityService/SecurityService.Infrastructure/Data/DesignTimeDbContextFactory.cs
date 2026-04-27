using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecurityService.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=nexusDb;Username=postgres;Password=pass.123";
        var schema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA");

        var optionsBuilder = new DbContextOptionsBuilder<SecurityDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            if (!string.IsNullOrEmpty(schema))
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            }
        });

        return new SecurityDbContext(optionsBuilder.Options, schema);
    }
}
