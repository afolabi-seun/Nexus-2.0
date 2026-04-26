using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UtilityService.Domain.Entities;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Configuration;

public static class SeedDataHelper
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UtilityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UtilityDbContext>>();

        logger.LogInformation("Seeding UtilityService reference data...");
        await SeedCoreDataAsync(context);
        logger.LogInformation("UtilityService reference data seeded.");
    }

    private static async Task SeedCoreDataAsync(UtilityDbContext context)
    {
        if (!await context.DepartmentTypes.AnyAsync())
        {
            context.DepartmentTypes.AddRange(
                new DepartmentType { TypeName = "Engineering", TypeCode = "ENG" },
                new DepartmentType { TypeName = "QA", TypeCode = "QA" },
                new DepartmentType { TypeName = "DevOps", TypeCode = "DEVOPS" },
                new DepartmentType { TypeName = "Product", TypeCode = "PROD" },
                new DepartmentType { TypeName = "Design", TypeCode = "DESIGN" }
            );
        }

        if (!await context.PriorityLevels.AnyAsync())
        {
            context.PriorityLevels.AddRange(
                new PriorityLevel { Name = "Critical", SortOrder = 1, Color = "#DC2626" },
                new PriorityLevel { Name = "High", SortOrder = 2, Color = "#EA580C" },
                new PriorityLevel { Name = "Medium", SortOrder = 3, Color = "#CA8A04" },
                new PriorityLevel { Name = "Low", SortOrder = 4, Color = "#16A34A" }
            );
        }

        if (!await context.TaskTypeRefs.AnyAsync())
        {
            context.TaskTypeRefs.AddRange(
                new TaskTypeRef { TypeName = "Development", DefaultDepartmentCode = "ENG" },
                new TaskTypeRef { TypeName = "Testing", DefaultDepartmentCode = "QA" },
                new TaskTypeRef { TypeName = "DevOps", DefaultDepartmentCode = "DEVOPS" },
                new TaskTypeRef { TypeName = "Design", DefaultDepartmentCode = "DESIGN" },
                new TaskTypeRef { TypeName = "Documentation", DefaultDepartmentCode = "PROD" },
                new TaskTypeRef { TypeName = "Bug", DefaultDepartmentCode = "ENG" }
            );
        }

        if (!await context.WorkflowStates.Where(w => w.EntityType == "Story").IgnoreQueryFilters().AnyAsync())
        {
            context.WorkflowStates.AddRange(
                new WorkflowState { EntityType = "Story", StateName = "Backlog", SortOrder = 1 },
                new WorkflowState { EntityType = "Story", StateName = "Ready", SortOrder = 2 },
                new WorkflowState { EntityType = "Story", StateName = "InProgress", SortOrder = 3 },
                new WorkflowState { EntityType = "Story", StateName = "InReview", SortOrder = 4 },
                new WorkflowState { EntityType = "Story", StateName = "QA", SortOrder = 5 },
                new WorkflowState { EntityType = "Story", StateName = "Done", SortOrder = 6 },
                new WorkflowState { EntityType = "Story", StateName = "Closed", SortOrder = 7 }
            );
        }

        if (!await context.WorkflowStates.Where(w => w.EntityType == "Task").IgnoreQueryFilters().AnyAsync())
        {
            context.WorkflowStates.AddRange(
                new WorkflowState { EntityType = "Task", StateName = "ToDo", SortOrder = 1 },
                new WorkflowState { EntityType = "Task", StateName = "InProgress", SortOrder = 2 },
                new WorkflowState { EntityType = "Task", StateName = "InReview", SortOrder = 3 },
                new WorkflowState { EntityType = "Task", StateName = "Done", SortOrder = 4 }
            );
        }

        await context.SaveChangesAsync();

        // Seed role restriction error codes
        var roleCodes = new[]
        {
            new ErrorCodeEntry { Code = "INSUFFICIENT_PERMISSIONS", Value = 1001, HttpStatusCode = 403, ResponseCode = "03", Description = "You don't have permission to perform this action.", ServiceName = "Shared" },
            new ErrorCodeEntry { Code = "ORGADMIN_REQUIRED", Value = 1002, HttpStatusCode = 403, ResponseCode = "03", Description = "This action requires OrgAdmin access.", ServiceName = "Shared" },
            new ErrorCodeEntry { Code = "DEPTLEAD_REQUIRED", Value = 1003, HttpStatusCode = 403, ResponseCode = "03", Description = "This action requires DeptLead or higher access.", ServiceName = "Shared" },
            new ErrorCodeEntry { Code = "PLATFORM_ADMIN_REQUIRED", Value = 1004, HttpStatusCode = 403, ResponseCode = "03", Description = "This action requires PlatformAdmin access.", ServiceName = "Shared" },
        };

        foreach (var code in roleCodes)
        {
            if (!await context.ErrorCodeEntries.AnyAsync(e => e.Code == code.Code))
                context.ErrorCodeEntries.Add(code);
        }

        await context.SaveChangesAsync();
    }
}
