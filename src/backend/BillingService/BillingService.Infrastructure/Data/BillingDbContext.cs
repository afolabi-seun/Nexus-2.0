using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BillingService.Domain.Entities;

namespace BillingService.Infrastructure.Data;

public class BillingDbContext : DbContext
{
    private readonly Guid? _organizationId;
    private Guid OrganizationIdValue => _organizationId ?? Guid.Empty;

    public BillingDbContext(DbContextOptions<BillingDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        if (httpContextAccessor?.HttpContext?.Items.TryGetValue("organizationId", out var orgId) == true
            && orgId is string orgIdStr && Guid.TryParse(orgIdStr, out var parsed))
        {
            _organizationId = parsed;
        }
    }

    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<StripeEvent> StripeEvents => Set<StripeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureSubscription(modelBuilder);
        ConfigurePlan(modelBuilder);
        ConfigureUsageRecord(modelBuilder);
        ConfigureStripeEvent(modelBuilder);
    }

    private void ConfigureSubscription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ExternalSubscriptionId).HasMaxLength(200);
            entity.Property(e => e.ExternalCustomerId).HasMaxLength(200);

            entity.HasIndex(e => e.OrganizationId).IsUnique();

            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ScheduledPlan)
                .WithMany()
                .HasForeignKey(e => e.ScheduledPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigurePlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.PlanId);
            entity.Property(e => e.PlanName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PlanCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PriceMonthly).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PriceYearly).HasColumnType("decimal(10,2)");
            entity.Property(e => e.FeaturesJson).HasColumnType("jsonb");

            entity.HasIndex(e => e.PlanCode).IsUnique();
        });
    }

    private void ConfigureUsageRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(e => e.UsageRecordId);
            entity.Property(e => e.MetricName).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => new { e.OrganizationId, e.MetricName, e.PeriodStart });

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureStripeEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StripeEvent>(entity =>
        {
            entity.HasKey(e => e.StripeEventId);
            entity.Property(e => e.StripeEventId).HasMaxLength(200);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        });
    }
}
