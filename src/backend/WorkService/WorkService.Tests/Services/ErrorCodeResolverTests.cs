using WorkService.Infrastructure.Services.ErrorCodeResolver;

namespace WorkService.Tests.Services;

public class ErrorCodeResolverTests
{
    [Theory]
    [InlineData("STORY_NOT_FOUND", "07")]
    [InlineData("TASK_NOT_FOUND", "07")]
    [InlineData("SPRINT_NOT_FOUND", "07")]
    [InlineData("PROJECT_NOT_FOUND", "07")]
    [InlineData("LABEL_NOT_FOUND", "07")]
    [InlineData("COMMENT_NOT_FOUND", "07")]
    [InlineData("NOT_FOUND", "07")]
    [InlineData("PROJECT_KEY_DUPLICATE", "06")]
    [InlineData("PROJECT_NAME_DUPLICATE", "06")]
    [InlineData("LABEL_NAME_DUPLICATE", "06")]
    [InlineData("STORY_ALREADY_IN_SPRINT", "06")]
    [InlineData("SPRINT_ALREADY_ACTIVE", "06")]
    [InlineData("SPRINT_ALREADY_COMPLETED", "06")]
    [InlineData("ORGANIZATION_MISMATCH", "03")]
    [InlineData("INSUFFICIENT_PERMISSIONS", "03")]
    [InlineData("DEPARTMENT_ACCESS_DENIED", "03")]
    [InlineData("RATE_LIMIT_EXCEEDED", "08")]
    [InlineData("INVALID_STORY_TRANSITION", "09")]
    [InlineData("INVALID_TASK_TRANSITION", "09")]
    [InlineData("INVALID_STORY_POINTS", "09")]
    [InlineData("INVALID_PRIORITY", "09")]
    [InlineData("INVALID_TASK_TYPE", "09")]
    [InlineData("VALIDATION_ERROR", "96")]
    [InlineData("INTERNAL_ERROR", "98")]
    public void MapErrorToResponseCode_KnownCodes_ReturnsExpected(string errorCode, string expectedResponseCode)
    {
        var result = ErrorCodeResolverService.MapErrorToResponseCode(errorCode);
        Assert.Equal(expectedResponseCode, result);
    }

    [Fact]
    public void MapErrorToResponseCode_IsDeterministic()
    {
        var code = "STORY_NOT_FOUND";
        var result1 = ErrorCodeResolverService.MapErrorToResponseCode(code);
        var result2 = ErrorCodeResolverService.MapErrorToResponseCode(code);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void MapErrorToResponseCode_UnknownCode_Returns99()
    {
        var result = ErrorCodeResolverService.MapErrorToResponseCode("SOME_UNKNOWN_CODE");
        Assert.Equal("99", result);
    }

    [Fact]
    public void MapErrorToResponseCode_AllKnownCodes_ReturnNonEmpty()
    {
        var knownCodes = new[]
        {
            "STORY_NOT_FOUND", "TASK_NOT_FOUND", "SPRINT_NOT_FOUND",
            "INVALID_STORY_TRANSITION", "INVALID_TASK_TRANSITION",
            "PROJECT_KEY_DUPLICATE", "PROJECT_NAME_DUPLICATE",
            "ORGANIZATION_MISMATCH", "INSUFFICIENT_PERMISSIONS",
            "VALIDATION_ERROR", "INTERNAL_ERROR", "RATE_LIMIT_EXCEEDED"
        };

        foreach (var code in knownCodes)
        {
            var result = ErrorCodeResolverService.MapErrorToResponseCode(code);
            Assert.False(string.IsNullOrEmpty(result), $"Response code for {code} should not be empty");
        }
    }
}
