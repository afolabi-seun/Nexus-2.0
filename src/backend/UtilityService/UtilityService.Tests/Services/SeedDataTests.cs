using Microsoft.EntityFrameworkCore;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Tests.Services;

public class SeedDataTests
{
    private UtilityDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<UtilityDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new UtilityDbContext(options);
    }

    private async Task SeedAsync(UtilityDbContext context)
    {
        // Seed department types
        if (!await context.DepartmentTypes.IgnoreQueryFilters().AnyAsync())
        {
            context.DepartmentTypes.AddRange(
                new Domain.Entities.DepartmentType { TypeName = "Engineering", TypeCode = "ENG" },
                new Domain.Entities.DepartmentType { TypeName = "QA", TypeCode = "QA" },
                new Domain.Entities.DepartmentType { TypeName = "DevOps", TypeCode = "DEVOPS" },
                new Domain.Entities.DepartmentType { TypeName = "Product", TypeCode = "PROD" },
                new Domain.Entities.DepartmentType { TypeName = "Design", TypeCode = "DESIGN" });
        }

        // Seed priority levels
        if (!await context.PriorityLevels.IgnoreQueryFilters().AnyAsync())
        {
            context.PriorityLevels.AddRange(
                new Domain.Entities.PriorityLevel { Name = "Critical", SortOrder = 1, Color = "#DC2626" },
                new Domain.Entities.PriorityLevel { Name = "High", SortOrder = 2, Color = "#EA580C" },
                new Domain.Entities.PriorityLevel { Name = "Medium", SortOrder = 3, Color = "#CA8A04" },
                new Domain.Entities.PriorityLevel { Name = "Low", SortOrder = 4, Color = "#16A34A" });
        }

        // Seed task types
        if (!await context.TaskTypeRefs.IgnoreQueryFilters().AnyAsync())
        {
            context.TaskTypeRefs.AddRange(
                new Domain.Entities.TaskTypeRef { TypeName = "Development", DefaultDepartmentCode = "ENG" },
                new Domain.Entities.TaskTypeRef { TypeName = "Testing", DefaultDepartmentCode = "QA" },
                new Domain.Entities.TaskTypeRef { TypeName = "DevOps", DefaultDepartmentCode = "DEVOPS" },
                new Domain.Entities.TaskTypeRef { TypeName = "Design", DefaultDepartmentCode = "DESIGN" },
                new Domain.Entities.TaskTypeRef { TypeName = "Documentation", DefaultDepartmentCode = "PROD" },
                new Domain.Entities.TaskTypeRef { TypeName = "Bug", DefaultDepartmentCode = "ENG" });
        }

        // Seed workflow states
        if (!await context.WorkflowStates.IgnoreQueryFilters().AnyAsync())
        {
            context.WorkflowStates.AddRange(
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "Backlog", SortOrder = 1 },
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "Ready", SortOrder = 2 },
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "InProgress", SortOrder = 3 },
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "InReview", SortOrder = 4 },
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "QA", SortOrder = 5 },
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "Done", SortOrder = 6 },
                new Domain.Entities.WorkflowState { EntityType = "Story", StateName = "Closed", SortOrder = 7 },
                new Domain.Entities.WorkflowState { EntityType = "Task", StateName = "ToDo", SortOrder = 1 },
                new Domain.Entities.WorkflowState { EntityType = "Task", StateName = "InProgress", SortOrder = 2 },
                new Domain.Entities.WorkflowState { EntityType = "Task", StateName = "InReview", SortOrder = 3 },
                new Domain.Entities.WorkflowState { EntityType = "Task", StateName = "Done", SortOrder = 4 });
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Seed_CreatesExactCounts()
    {
        using var context = CreateInMemoryContext(nameof(Seed_CreatesExactCounts));
        await SeedAsync(context);

        Assert.Equal(5, await context.DepartmentTypes.IgnoreQueryFilters().CountAsync());
        Assert.Equal(4, await context.PriorityLevels.IgnoreQueryFilters().CountAsync());
        Assert.Equal(6, await context.TaskTypeRefs.IgnoreQueryFilters().CountAsync());
        Assert.Equal(11, await context.WorkflowStates.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Seed_Idempotent_RunTwiceNoDuplicates()
    {
        using var context = CreateInMemoryContext(nameof(Seed_Idempotent_RunTwiceNoDuplicates));

        await SeedAsync(context);
        await SeedAsync(context);

        Assert.Equal(5, await context.DepartmentTypes.IgnoreQueryFilters().CountAsync());
        Assert.Equal(4, await context.PriorityLevels.IgnoreQueryFilters().CountAsync());
        Assert.Equal(6, await context.TaskTypeRefs.IgnoreQueryFilters().CountAsync());
        Assert.Equal(11, await context.WorkflowStates.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Seed_DepartmentTypes_ExactValues()
    {
        using var context = CreateInMemoryContext(nameof(Seed_DepartmentTypes_ExactValues));
        await SeedAsync(context);

        var depts = await context.DepartmentTypes.IgnoreQueryFilters().ToListAsync();
        Assert.Contains(depts, d => d.TypeName == "Engineering" && d.TypeCode == "ENG");
        Assert.Contains(depts, d => d.TypeName == "QA" && d.TypeCode == "QA");
        Assert.Contains(depts, d => d.TypeName == "DevOps" && d.TypeCode == "DEVOPS");
        Assert.Contains(depts, d => d.TypeName == "Product" && d.TypeCode == "PROD");
        Assert.Contains(depts, d => d.TypeName == "Design" && d.TypeCode == "DESIGN");
    }

    [Fact]
    public async Task Seed_PriorityLevels_ExactValues()
    {
        using var context = CreateInMemoryContext(nameof(Seed_PriorityLevels_ExactValues));
        await SeedAsync(context);

        var levels = await context.PriorityLevels.IgnoreQueryFilters().OrderBy(p => p.SortOrder).ToListAsync();
        Assert.Equal("Critical", levels[0].Name);
        Assert.Equal("#DC2626", levels[0].Color);
        Assert.Equal("High", levels[1].Name);
        Assert.Equal("#EA580C", levels[1].Color);
        Assert.Equal("Medium", levels[2].Name);
        Assert.Equal("#CA8A04", levels[2].Color);
        Assert.Equal("Low", levels[3].Name);
        Assert.Equal("#16A34A", levels[3].Color);
    }
}
