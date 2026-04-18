using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;

namespace ProfileService.Infrastructure.Data;

public class ProfileDbContext : DbContext
{
    private readonly Guid? _organizationId;
    private Guid OrganizationIdValue => _organizationId ?? Guid.Empty;

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        if (httpContextAccessor?.HttpContext?.Items.TryGetValue("OrganizationId", out var orgId) == true
            && orgId is string orgIdStr && Guid.TryParse(orgIdStr, out var parsed))
        {
            _organizationId = parsed;
        }
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<DepartmentMember> DepartmentMembers => Set<DepartmentMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<NotificationType> NotificationTypes => Set<NotificationType>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<NavigationItem> NavigationItems => Set<NavigationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOrganization(modelBuilder);
        ConfigureDepartment(modelBuilder);
        ConfigureTeamMember(modelBuilder);
        ConfigureDepartmentMember(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureInvite(modelBuilder);
        ConfigureDevice(modelBuilder);
        ConfigureNotificationSetting(modelBuilder);
        ConfigureNotificationType(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigurePlatformAdmin(modelBuilder);
        ConfigureNavigationItem(modelBuilder);
    }

    private void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.OrganizationId);
            entity.Property(e => e.OrganizationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.StoryIdPrefix).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.TimeZone).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SettingsJson).HasColumnType("jsonb");
            entity.Property(e => e.FlgStatus).IsRequired().HasMaxLength(1);

            entity.HasIndex(e => e.OrganizationName).IsUnique();
            entity.HasIndex(e => e.StoryIdPrefix).IsUnique();
        });
    }

    private void ConfigureDepartment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DepartmentCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PreferencesJson).HasColumnType("jsonb");
            entity.Property(e => e.FlgStatus).IsRequired().HasMaxLength(1);

            entity.HasIndex(e => new { e.OrganizationId, e.DepartmentName }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.DepartmentCode }).IsUnique();

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Departments)
                .HasForeignKey(e => e.OrganizationId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureTeamMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.TeamMemberId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.ProfessionalId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Skills).HasColumnType("jsonb");
            entity.Property(e => e.Availability).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FlgStatus).IsRequired().HasMaxLength(1);

            entity.HasIndex(e => new { e.OrganizationId, e.Email }).IsUnique();
            entity.HasIndex(e => e.ProfessionalId);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.TeamMembers)
                .HasForeignKey(e => e.OrganizationId);

            entity.HasOne(e => e.PrimaryDepartment)
                .WithMany()
                .HasForeignKey(e => e.PrimaryDepartmentId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureDepartmentMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartmentMember>(entity =>
        {
            entity.HasKey(e => e.DepartmentMemberId);

            entity.HasIndex(e => new { e.TeamMemberId, e.DepartmentId }).IsUnique();

            entity.HasOne(e => e.TeamMember)
                .WithMany(t => t.DepartmentMemberships)
                .HasForeignKey(e => e.TeamMemberId);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.DepartmentMembers)
                .HasForeignKey(e => e.DepartmentId);

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.RoleName).IsUnique();
        });
    }

    private void ConfigureInvite(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(e => e.InviteId);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(128);
            entity.Property(e => e.FlgStatus).IsRequired().HasMaxLength(1);

            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId);

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId);

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId);

            entity.HasOne(e => e.InvitedByMember)
                .WithMany()
                .HasForeignKey(e => e.InvitedByMemberId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureDevice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.FlgStatus).IsRequired().HasMaxLength(1);

            entity.HasOne(e => e.TeamMember)
                .WithMany()
                .HasForeignKey(e => e.TeamMemberId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureNotificationSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationSetting>(entity =>
        {
            entity.HasKey(e => e.NotificationSettingId);

            entity.HasIndex(e => new { e.TeamMemberId, e.NotificationTypeId }).IsUnique();

            entity.HasOne(e => e.NotificationType)
                .WithMany()
                .HasForeignKey(e => e.NotificationTypeId);

            entity.HasOne(e => e.TeamMember)
                .WithMany()
                .HasForeignKey(e => e.TeamMemberId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigureNotificationType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.NotificationTypeId);
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.TypeName).IsUnique();
        });
    }

    private void ConfigureUserPreferences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.UserPreferencesId);
            entity.Property(e => e.Theme).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TimezoneOverride).HasMaxLength(100);
            entity.Property(e => e.DefaultBoardView).HasMaxLength(20);
            entity.Property(e => e.DefaultBoardFilters).HasColumnType("jsonb");
            entity.Property(e => e.DashboardLayout).HasColumnType("jsonb");
            entity.Property(e => e.EmailDigestFrequency).HasMaxLength(20);
            entity.Property(e => e.DateFormat).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TimeFormat).IsRequired().HasMaxLength(10);

            entity.HasIndex(e => e.TeamMemberId).IsUnique();

            entity.HasOne(e => e.TeamMember)
                .WithMany()
                .HasForeignKey(e => e.TeamMemberId);

            entity.HasQueryFilter(e => _organizationId == null || e.OrganizationId == OrganizationIdValue);
        });
    }

    private void ConfigurePlatformAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformAdmin>(entity =>
        {
            entity.HasKey(e => e.PlatformAdminId);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FlgStatus).IsRequired().HasMaxLength(1);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private void ConfigureNavigationItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NavigationItem>(entity =>
        {
            entity.HasKey(e => e.NavigationItemId);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Icon).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Section).IsRequired().HasMaxLength(50).HasDefaultValue("");

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Path, e.ParentId }).IsUnique();
        });

        // Seed navigation items
        var boardsId = new Guid("a0000000-0000-0000-0000-000000000004");

        modelBuilder.Entity<NavigationItem>().HasData(
            // Work section
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000001"), Label = "Dashboard", Path = "/", Icon = "LayoutDashboard", Section = "Work", SortOrder = 1, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000002"), Label = "Projects", Path = "/projects", Icon = "FolderKanban", Section = "Work", SortOrder = 2, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000003"), Label = "Stories", Path = "/stories", Icon = "BookOpen", Section = "Work", SortOrder = 3, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = boardsId, Label = "Boards", Path = "/boards", Icon = "Columns3", Section = "Work", SortOrder = 4, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000041"), Label = "Kanban", Path = "/boards/kanban", Icon = "Kanban", Section = "Work", SortOrder = 1, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)boardsId },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000042"), Label = "Sprint Board", Path = "/boards/sprint", Icon = "CalendarDays", Section = "Work", SortOrder = 2, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)boardsId },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000043"), Label = "Dept Board", Path = "/boards/department", Icon = "Building2", Section = "Work", SortOrder = 3, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)boardsId },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000044"), Label = "Backlog", Path = "/boards/backlog", Icon = "Archive", Section = "Work", SortOrder = 4, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)boardsId },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000005"), Label = "Sprints", Path = "/sprints", Icon = "Timer", Section = "Work", SortOrder = 5, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            // Tracking section
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000006"), Label = "Time Tracking", Path = "/time-tracking", Icon = "Clock", Section = "Tracking", SortOrder = 1, MinPermissionLevel = 50, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000007"), Label = "Analytics", Path = "/analytics", Icon = "TrendingUp", Section = "Tracking", SortOrder = 2, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000008"), Label = "Reports", Path = "/reports", Icon = "BarChart3", Section = "Tracking", SortOrder = 3, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            // Team section
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000009"), Label = "Members", Path = "/members", Icon = "Users", Section = "Team", SortOrder = 1, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000010"), Label = "Departments", Path = "/departments", Icon = "Building2", Section = "Team", SortOrder = 2, MinPermissionLevel = 25, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000011"), Label = "Invites", Path = "/invites", Icon = "Mail", Section = "Team", SortOrder = 3, MinPermissionLevel = 75, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            // Organization section
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000012"), Label = "Settings", Path = "/settings", Icon = "Settings", Section = "Organization", SortOrder = 1, MinPermissionLevel = 100, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000013"), Label = "Billing", Path = "/billing", Icon = "CreditCard", Section = "Organization", SortOrder = 2, MinPermissionLevel = 100, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000014"), Label = "Audit Logs", Path = "/audit-logs", Icon = "ClipboardList", Section = "Organization", SortOrder = 3, MinPermissionLevel = 100, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null },
            new { NavigationItemId = new Guid("a0000000-0000-0000-0000-000000000015"), Label = "Notifications", Path = "/notifications", Icon = "Bell", Section = "Organization", SortOrder = 4, MinPermissionLevel = 75, IsEnabled = true, DateCreated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), DateUpdated = DateTime.Parse("2025-01-01T00:00:00Z").ToUniversalTime(), ParentId = (Guid?)null }
        );
    }
}
