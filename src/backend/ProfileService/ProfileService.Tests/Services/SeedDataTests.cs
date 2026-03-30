using Microsoft.EntityFrameworkCore;
using ProfileService.Infrastructure.Data;
using ProfileService.Tests.Helpers;

namespace ProfileService.Tests.Services;

public class SeedDataTests
{
    [Fact]
    public async Task SeedRolesAsync_CreatesExactly4Roles()
    {
        using var context = TestDbContextFactory.Create();

        await SeedData.SeedRolesAsync(context);

        var roles = await context.Roles.ToListAsync();
        Assert.Equal(4, roles.Count);
        Assert.Contains(roles, r => r.RoleName == "OrgAdmin" && r.PermissionLevel == 100);
        Assert.Contains(roles, r => r.RoleName == "DeptLead" && r.PermissionLevel == 75);
        Assert.Contains(roles, r => r.RoleName == "Member" && r.PermissionLevel == 50);
        Assert.Contains(roles, r => r.RoleName == "Viewer" && r.PermissionLevel == 25);
    }

    [Fact]
    public async Task SeedNotificationTypesAsync_CreatesExactly8Types()
    {
        using var context = TestDbContextFactory.Create();

        await SeedData.SeedNotificationTypesAsync(context);

        var types = await context.NotificationTypes.ToListAsync();
        Assert.Equal(8, types.Count);
        Assert.Contains(types, t => t.TypeName == "StoryAssigned");
        Assert.Contains(types, t => t.TypeName == "TaskAssigned");
        Assert.Contains(types, t => t.TypeName == "SprintStarted");
        Assert.Contains(types, t => t.TypeName == "SprintEnded");
        Assert.Contains(types, t => t.TypeName == "MentionedInComment");
        Assert.Contains(types, t => t.TypeName == "StoryStatusChanged");
        Assert.Contains(types, t => t.TypeName == "TaskStatusChanged");
        Assert.Contains(types, t => t.TypeName == "DueDateApproaching");
    }

    [Fact]
    public async Task SeedRolesAsync_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        using var context = TestDbContextFactory.Create();

        await SeedData.SeedRolesAsync(context);
        await SeedData.SeedRolesAsync(context); // second call

        var roles = await context.Roles.ToListAsync();
        Assert.Equal(4, roles.Count);
    }

    [Fact]
    public async Task SeedNotificationTypesAsync_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        using var context = TestDbContextFactory.Create();

        await SeedData.SeedNotificationTypesAsync(context);
        await SeedData.SeedNotificationTypesAsync(context); // second call

        var types = await context.NotificationTypes.ToListAsync();
        Assert.Equal(8, types.Count);
    }
}
