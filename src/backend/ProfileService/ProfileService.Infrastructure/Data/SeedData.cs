using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;

namespace ProfileService.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedRolesAsync(ProfileDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var roles = new[]
        {
            new Role { RoleName = "OrgAdmin", Description = "Full access to everything in the organization", PermissionLevel = 100, IsSystemRole = true },
            new Role { RoleName = "DeptLead", Description = "Full access within department", PermissionLevel = 75, IsSystemRole = true },
            new Role { RoleName = "Member", Description = "Standard access within department", PermissionLevel = 50, IsSystemRole = true },
            new Role { RoleName = "Viewer", Description = "Read-only access", PermissionLevel = 25, IsSystemRole = true },
        };
        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    public static async Task SeedNotificationTypesAsync(ProfileDbContext context)
    {
        if (await context.NotificationTypes.AnyAsync()) return;

        var types = new[]
        {
            new NotificationType { TypeName = "StoryAssigned" },
            new NotificationType { TypeName = "TaskAssigned" },
            new NotificationType { TypeName = "SprintStarted" },
            new NotificationType { TypeName = "SprintEnded" },
            new NotificationType { TypeName = "MentionedInComment" },
            new NotificationType { TypeName = "StoryStatusChanged" },
            new NotificationType { TypeName = "TaskStatusChanged" },
            new NotificationType { TypeName = "DueDateApproaching" },
        };
        await context.NotificationTypes.AddRangeAsync(types);
        await context.SaveChangesAsync();
    }

    public static async Task SeedDefaultDepartmentsAsync(ProfileDbContext context, Guid organizationId)
    {
        var defaults = new[]
        {
            new Department { OrganizationId = organizationId, DepartmentName = "Engineering", DepartmentCode = "ENG", IsDefault = true },
            new Department { OrganizationId = organizationId, DepartmentName = "QA", DepartmentCode = "QA", IsDefault = true },
            new Department { OrganizationId = organizationId, DepartmentName = "DevOps", DepartmentCode = "DEVOPS", IsDefault = true },
            new Department { OrganizationId = organizationId, DepartmentName = "Product", DepartmentCode = "PROD", IsDefault = true },
            new Department { OrganizationId = organizationId, DepartmentName = "Design", DepartmentCode = "DESIGN", IsDefault = true },
        };
        await context.Departments.AddRangeAsync(defaults);
        await context.SaveChangesAsync();
    }

    public static async Task SeedNavigationItemsAsync(ProfileDbContext context)
    {
        if (await context.NavigationItems.AnyAsync()) return;

        // Permission levels: Viewer=25, Member=50, DeptLead=75, OrgAdmin=100
        var boardsParent = new NavigationItem { Label = "Boards", Path = "/boards", Icon = "Columns3", SortOrder = 4, MinPermissionLevel = 25 };

        var items = new[]
        {
            new NavigationItem { Label = "Dashboard", Path = "/", Icon = "LayoutDashboard", SortOrder = 1, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Projects", Path = "/projects", Icon = "FolderKanban", SortOrder = 2, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Stories", Path = "/stories", Icon = "BookOpen", SortOrder = 3, MinPermissionLevel = 25 },
            boardsParent,
            new NavigationItem { Label = "Sprints", Path = "/sprints", Icon = "Timer", SortOrder = 5, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Members", Path = "/members", Icon = "Users", SortOrder = 6, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Departments", Path = "/departments", Icon = "Building2", SortOrder = 7, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Reports", Path = "/reports", Icon = "BarChart3", SortOrder = 8, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Search", Path = "/search", Icon = "Search", SortOrder = 9, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Settings", Path = "/settings", Icon = "Settings", SortOrder = 10, MinPermissionLevel = 100 },
            new NavigationItem { Label = "Invites", Path = "/invites", Icon = "Mail", SortOrder = 11, MinPermissionLevel = 75 },
        };

        await context.NavigationItems.AddRangeAsync(items);
        await context.SaveChangesAsync();

        // Add board children
        var boardChildren = new[]
        {
            new NavigationItem { Label = "Kanban", Path = "/boards/kanban", Icon = "Kanban", SortOrder = 1, ParentId = boardsParent.NavigationItemId, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Sprint Board", Path = "/boards/sprint", Icon = "CalendarDays", SortOrder = 2, ParentId = boardsParent.NavigationItemId, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Department Board", Path = "/boards/department", Icon = "Building2", SortOrder = 3, ParentId = boardsParent.NavigationItemId, MinPermissionLevel = 25 },
            new NavigationItem { Label = "Backlog", Path = "/boards/backlog", Icon = "Archive", SortOrder = 4, ParentId = boardsParent.NavigationItemId, MinPermissionLevel = 25 },
        };

        await context.NavigationItems.AddRangeAsync(boardChildren);
        await context.SaveChangesAsync();
    }
}
