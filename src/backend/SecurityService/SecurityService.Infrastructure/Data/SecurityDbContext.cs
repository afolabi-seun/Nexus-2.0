using Microsoft.EntityFrameworkCore;
using SecurityService.Domain.Entities;

namespace SecurityService.Infrastructure.Data;

public class SecurityDbContext : DbContext
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<ServiceToken> ServiceTokens => Set<ServiceToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
