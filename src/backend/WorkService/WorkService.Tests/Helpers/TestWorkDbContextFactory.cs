using Microsoft.EntityFrameworkCore;
using WorkService.Infrastructure.Data;

namespace WorkService.Tests.Helpers;

public static class TestWorkDbContextFactory
{
    public static WorkDbContext Create()
    {
        var options = new DbContextOptionsBuilder<WorkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestWorkDbContext(options);
    }
}

/// <summary>
/// WorkDbContext subclass that ignores NpgsqlTsVector properties for in-memory testing.
/// </summary>
public class TestWorkDbContext : WorkDbContext
{
    public TestWorkDbContext(DbContextOptions<WorkDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore NpgsqlTsVector computed columns — not supported by in-memory provider
        modelBuilder.Entity<Domain.Entities.Story>().Ignore(e => e.SearchVector);
        modelBuilder.Entity<Domain.Entities.Task>().Ignore(e => e.SearchVector);
    }
}
