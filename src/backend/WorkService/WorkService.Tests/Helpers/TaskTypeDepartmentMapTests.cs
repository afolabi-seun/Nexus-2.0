using WorkService.Domain.Exceptions;
using WorkService.Domain.Helpers;

namespace WorkService.Tests.Helpers;

public class TaskTypeDepartmentMapTests
{
    [Theory]
    [InlineData("Development", "ENG")]
    [InlineData("Testing", "QA")]
    [InlineData("DevOps", "DEVOPS")]
    [InlineData("Design", "DESIGN")]
    [InlineData("Documentation", "PROD")]
    [InlineData("Bug", "ENG")]
    public void GetDepartmentCode_ValidTaskType_ReturnsCorrectCode(string taskType, string expectedCode)
    {
        Assert.Equal(expectedCode, TaskTypeDepartmentMap.GetDepartmentCode(taskType));
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    [InlineData("development")]
    public void GetDepartmentCode_InvalidTaskType_ThrowsInvalidTaskTypeException(string taskType)
    {
        Assert.Throws<InvalidTaskTypeException>(() => TaskTypeDepartmentMap.GetDepartmentCode(taskType));
    }

    [Fact]
    public void GetAll_Returns6Mappings()
    {
        var all = TaskTypeDepartmentMap.GetAll();
        Assert.Equal(6, all.Count);
    }
}
