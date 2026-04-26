using Microsoft.EntityFrameworkCore;
using SecurityService.Domain.Entities;

namespace SecurityService.Infrastructure.Data;

public class SecurityDbContext : DbContext
{
    private readonly string? _databaseSchema;

    public SecurityDbContext(DbContextOptions<SecurityDbContext> options, string? databaseSchema = null) : base(options)
    {
        _databaseSchema = databaseSchema ?? Environment.GetEnvironmentVariable("DATABASE_SCHEMA");
    }

    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<ServiceToken> ServiceTokens => Set<ServiceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrEmpty(_databaseSchema) && Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.HasDefaultSchema(_databaseSchema);
        }

        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.HasKey(e => e.PasswordHistoryId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<ServiceToken>(entity =>
        {
            entity.HasKey(e => e.ServiceTokenId);
            entity.HasIndex(e => e.ServiceId);
            entity.Property(e => e.ServiceId).IsRequired();
            entity.Property(e => e.ServiceName).IsRequired();
            entity.Property(e => e.TokenHash).IsRequired();
        });
    }
}
