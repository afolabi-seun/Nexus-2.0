using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;

namespace UtilityService.Infrastructure.Data;

public class UtilityDbContext : DbContext
{
    public Guid OrganizationId { get; set; }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ArchivedAuditLog> ArchivedAuditLogs => Set<ArchivedAuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<ErrorCodeEntry> ErrorCodeEntries => Set<ErrorCodeEntry>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<DepartmentType> DepartmentTypes => Set<DepartmentType>();
    public DbSet<PriorityLevel> PriorityLevels => Set<PriorityLevel>();
    public DbSet<TaskTypeRef> TaskTypeRefs => Set<TaskTypeRef>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();

    public UtilityDbContext(DbContextOptions<UtilityDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Organization-scoped global query filters
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(e => e.OrganizationId == OrganizationId);
        modelBuilder.Entity<ErrorLog>()
            .HasQueryFilter(e => e.OrganizationId == OrganizationId);
        modelBuilder.Entity<NotificationLog>()
            .HasQueryFilter(e => e.OrganizationId == OrganizationId);

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.DateCreated);
            entity.Property(e => e.ServiceName).IsRequired();
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.EntityId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CorrelationId).IsRequired();
        });

        // ArchivedAuditLog — no org-scoped query filter
        modelBuilder.Entity<ArchivedAuditLog>(entity =>
        {
            entity.HasKey(e => e.ArchivedAuditLogId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.DateCreated);
            entity.Property(e => e.ArchivedDate).IsRequired();
        });

        // ErrorLog
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.ErrorLogId);
            entity.HasIndex(e => e.OrganizationId);
            entity.Property(e => e.ServiceName).IsRequired();
            entity.Property(e => e.ErrorCode).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.CorrelationId).IsRequired();
            entity.Property(e => e.Severity).IsRequired();
        });

        // ErrorCodeEntry — not org-scoped
        modelBuilder.Entity<ErrorCodeEntry>(entity =>
        {
            entity.HasKey(e => e.ErrorCodeEntryId);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.ResponseCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.ServiceName).IsRequired();
        });

        // NotificationLog
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.NotificationLogId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.NotificationType).IsRequired();
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.Recipient).IsRequired();
            entity.Property(e => e.Status).IsRequired();
        });

        // Reference data — soft delete via FlgStatus global query filter
        modelBuilder.Entity<DepartmentType>(entity =>
        {
            entity.HasKey(e => e.DepartmentTypeId);
            entity.HasQueryFilter(e => e.FlgStatus != "D");
            entity.Property(e => e.TypeName).IsRequired();
            entity.Property(e => e.TypeCode).IsRequired();
        });

        modelBuilder.Entity<PriorityLevel>(entity =>
        {
            entity.HasKey(e => e.PriorityLevelId);
            entity.HasQueryFilter(e => e.FlgStatus != "D");
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Color).IsRequired();
        });

        modelBuilder.Entity<TaskTypeRef>(entity =>
        {
            entity.HasKey(e => e.TaskTypeRefId);
            entity.HasQueryFilter(e => e.FlgStatus != "D");
            entity.Property(e => e.TypeName).IsRequired();
            entity.Property(e => e.DefaultDepartmentCode).IsRequired();
        });

        modelBuilder.Entity<WorkflowState>(entity =>
        {
            entity.HasKey(e => e.WorkflowStateId);
            entity.HasQueryFilter(e => e.FlgStatus != "D");
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.StateName).IsRequired();
        });
    }
}
