using Microsoft.EntityFrameworkCore;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ProfileDbContext Create(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ProfileDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ProfileDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
