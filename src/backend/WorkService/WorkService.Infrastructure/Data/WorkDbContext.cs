using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;

namespace WorkService.Infrastructure.Data;

public class WorkDbContext : DbContext
{
    private readonly Guid? _organizationId;

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<Domain.Entities.Task> Tasks => Set<Domain.Entities.Task>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintStory> SprintStories => Set<SprintStory>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<StoryLabel> StoryLabels => Set<StoryLabel>();
    public DbSet<StoryLink> StoryLinks => Set<StoryLink>();
    public DbSet<StorySequence> StorySequences => Set<StorySequence>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<CostRate> CostRates => Set<CostRate>();
    public DbSet<TimePolicy> TimePolicies => Set<TimePolicy>();
    public DbSet<TimeApproval> TimeApprovals => Set<TimeApproval>();
    public DbSet<CostSnapshot> CostSnapshots => Set<CostSnapshot>();
    public DbSet<VelocitySnapshot> VelocitySnapshots => Set<VelocitySnapshot>();
    public DbSet<ProjectHealthSnapshot> ProjectHealthSnapshots => Set<ProjectHealthSnapshot>();
    public DbSet<ResourceAllocationSnapshot> ResourceAllocationSnapshots => Set<ResourceAllocationSnapshot>();
    public DbSet<RiskRegister> RiskRegisters => Set<RiskRegister>();
    public DbSet<StoryTemplate> StoryTemplates => Set<StoryTemplate>();

    public WorkDbContext(DbContextOptions<WorkDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        if (httpContextAccessor?.HttpContext?.Items.TryGetValue("OrganizationId", out var orgId) == true)
        {
            if (orgId is Guid id) _organizationId = id;
            else if (orgId is string orgIdStr && Guid.TryParse(orgIdStr, out var parsed)) _organizationId = parsed;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProject(modelBuilder);
        ConfigureStory(modelBuilder);
        ConfigureTask(modelBuilder);
        ConfigureSprint(modelBuilder);
        ConfigureSprintStory(modelBuilder);
        ConfigureComment(modelBuilder);
        ConfigureActivityLog(modelBuilder);
        ConfigureLabel(modelBuilder);
        ConfigureStoryLabel(modelBuilder);
        ConfigureStoryLink(modelBuilder);
        ConfigureStorySequence(modelBuilder);
        ConfigureSavedFilter(modelBuilder);
        ConfigureTimeEntry(modelBuilder);
        ConfigureCostRate(modelBuilder);
        ConfigureTimePolicy(modelBuilder);
        ConfigureTimeApproval(modelBuilder);
        ConfigureCostSnapshot(modelBuilder);
        ConfigureVelocitySnapshot(modelBuilder);
        ConfigureProjectHealthSnapshot(modelBuilder);
        ConfigureResourceAllocationSnapshot(modelBuilder);
        ConfigureRiskRegister(modelBuilder);
    }

    private void ConfigureProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId);
            entity.HasIndex(e => e.ProjectKey).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.ProjectName }).IsUnique();
            entity.Property(e => e.ProjectName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProjectKey).IsRequired().HasMaxLength(10);
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureStory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Story>(entity =>
        {
            entity.HasKey(e => e.StoryId);
            entity.HasIndex(e => new { e.ProjectId, e.StoryKey }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => new { e.OrganizationId, e.ProjectId });
            entity.HasIndex(e => new { e.OrganizationId, e.SprintId });
            entity.HasIndex(e => new { e.OrganizationId, e.AssigneeId });
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.AcceptanceCriteria).HasMaxLength(5000);
            entity.Property(e => e.StoryKey).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Priority).IsRequired().HasDefaultValue("Medium");
            entity.Property(e => e.StoryType).IsRequired().HasDefaultValue("Feature").HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasDefaultValue("Backlog");
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId);

            entity.Property(e => e.SearchVector)
                .HasColumnType("tsvector")
                .HasComputedColumnSql(
                    "setweight(to_tsvector('english', coalesce(\"StoryKey\", '')), 'A') || " +
                    "setweight(to_tsvector('english', coalesce(\"Title\", '')), 'A') || " +
                    "setweight(to_tsvector('english', coalesce(\"Description\", '')), 'B')",
                    stored: true);
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");

            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureTask(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Entities.Task>(entity =>
        {
            entity.HasKey(e => e.TaskId);
            entity.HasIndex(e => new { e.OrganizationId, e.StoryId });
            entity.HasIndex(e => new { e.OrganizationId, e.AssigneeId });
            entity.HasIndex(e => new { e.OrganizationId, e.DepartmentId });
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(3000);
            entity.Property(e => e.TaskType).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasDefaultValue("ToDo");
            entity.Property(e => e.Priority).IsRequired().HasDefaultValue("Medium");
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.Property(e => e.ActualHours).HasDefaultValue(0m);
            entity.HasOne<Story>().WithMany().HasForeignKey(e => e.StoryId);

            entity.Property(e => e.SearchVector)
                .HasColumnType("tsvector")
                .HasComputedColumnSql(
                    "setweight(to_tsvector('english', coalesce(\"Title\", '')), 'A') || " +
                    "setweight(to_tsvector('english', coalesce(\"Description\", '')), 'B')",
                    stored: true);
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");

            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureSprint(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(e => e.SprintId);
            entity.HasIndex(e => new { e.OrganizationId, e.ProjectId, e.Status });
            entity.Property(e => e.SprintName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Goal).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasDefaultValue("Planning");
            entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId);
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureSprintStory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SprintStory>(entity =>
        {
            entity.HasKey(e => e.SprintStoryId);
            entity.HasIndex(e => new { e.SprintId, e.StoryId })
                .IsUnique()
                .HasFilter("\"RemovedDate\" IS NULL");
            entity.HasOne<Sprint>().WithMany().HasForeignKey(e => e.SprintId);
            entity.HasOne<Story>().WithMany().HasForeignKey(e => e.StoryId);
        });
    }

    private void ConfigureComment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureActivityLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.ActivityLogId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.ActorName).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureLabel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasKey(e => e.LabelId);
            entity.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureStoryLabel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoryLabel>(entity =>
        {
            entity.HasKey(e => e.StoryLabelId);
            entity.HasIndex(e => new { e.StoryId, e.LabelId }).IsUnique();
        });
    }

    private void ConfigureStoryLink(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoryLink>(entity =>
        {
            entity.HasKey(e => e.StoryLinkId);
            entity.HasOne<Story>().WithMany().HasForeignKey(e => e.SourceStoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Story>().WithMany().HasForeignKey(e => e.TargetStoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureStorySequence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StorySequence>(entity =>
        {
            entity.HasKey(e => e.ProjectId);
            entity.Property(e => e.CurrentValue).IsRequired().HasDefaultValue(0L);
        });
    }

    private void ConfigureSavedFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedFilter>(entity =>
        {
            entity.HasKey(e => e.SavedFilterId);
            entity.HasIndex(e => new { e.OrganizationId, e.TeamMemberId });
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Filters).IsRequired().HasColumnType("jsonb");
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureTimeEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.HasKey(e => e.TimeEntryId);
            entity.HasIndex(e => new { e.OrganizationId, e.StoryId });
            entity.HasIndex(e => new { e.OrganizationId, e.MemberId, e.Date });
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.Property(e => e.Status).IsRequired().HasDefaultValue("Pending");
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureCostRate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostRate>(entity =>
        {
            entity.HasKey(e => e.CostRateId);
            entity.HasIndex(e => new { e.OrganizationId, e.RateType, e.MemberId, e.RoleName, e.DepartmentId })
                .IsUnique()
                .HasFilter("\"FlgStatus\" = 'A'");
            entity.Property(e => e.RateType).IsRequired();
            entity.Property(e => e.HourlyRate).IsRequired();
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureTimePolicy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimePolicy>(entity =>
        {
            entity.HasKey(e => e.TimePolicyId);
            entity.HasIndex(e => e.OrganizationId)
                .IsUnique()
                .HasFilter("\"FlgStatus\" = 'A'");
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }

    private void ConfigureTimeApproval(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeApproval>(entity =>
        {
            entity.HasKey(e => e.TimeApprovalId);
            entity.HasIndex(e => e.TimeEntryId);
            entity.Property(e => e.Action).IsRequired();
        });
    }

    private void ConfigureCostSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostSnapshot>(entity =>
        {
            entity.HasKey(e => e.CostSnapshotId);
            entity.HasIndex(e => new { e.ProjectId, e.PeriodStart, e.PeriodEnd });
            entity.Property(e => e.TotalCost).IsRequired();
        });
    }

    private void ConfigureVelocitySnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VelocitySnapshot>(entity =>
        {
            entity.HasKey(e => e.VelocitySnapshotId);
            entity.HasIndex(e => new { e.ProjectId, e.SprintId }).IsUnique();
            entity.HasIndex(e => new { e.ProjectId, e.EndDate });
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureProjectHealthSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectHealthSnapshot>(entity =>
        {
            entity.HasKey(e => e.ProjectHealthSnapshotId);
            entity.HasIndex(e => new { e.ProjectId, e.SnapshotDate });
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureResourceAllocationSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceAllocationSnapshot>(entity =>
        {
            entity.HasKey(e => e.ResourceAllocationSnapshotId);
            entity.HasIndex(e => new { e.ProjectId, e.MemberId, e.PeriodStart, e.PeriodEnd }).IsUnique();
            entity.HasIndex(e => new { e.ProjectId, e.PeriodStart });
            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == _organizationId);
        });
    }

    private void ConfigureRiskRegister(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RiskRegister>(entity =>
        {
            entity.HasKey(e => e.RiskRegisterId);
            entity.HasIndex(e => new { e.OrganizationId, e.ProjectId });
            entity.HasIndex(e => new { e.OrganizationId, e.ProjectId, e.SprintId });
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Severity).IsRequired().HasDefaultValue("Medium");
            entity.Property(e => e.Likelihood).IsRequired().HasDefaultValue("Medium");
            entity.Property(e => e.MitigationStatus).IsRequired().HasDefaultValue("Open");
            entity.Property(e => e.FlgStatus).IsRequired().HasDefaultValue("A");
            entity.HasQueryFilter(e => (_organizationId == null || e.OrganizationId == _organizationId) && e.FlgStatus == "A");
        });
    }
}
