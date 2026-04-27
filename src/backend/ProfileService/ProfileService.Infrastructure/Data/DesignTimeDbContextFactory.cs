using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProfileService.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.Load();

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=nexusDb;Username=postgres;Password=pass.123";
        var schema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA");

        var optionsBuilder = new DbContextOptionsBuilder<ProfileDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            if (!string.IsNullOrEmpty(schema))
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            }
        });

        return new ProfileDbContext(optionsBuilder.Options, databaseSchema: schema);
    }
}
