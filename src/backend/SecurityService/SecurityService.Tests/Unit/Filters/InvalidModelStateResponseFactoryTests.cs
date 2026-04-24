using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SecurityService.Api.Extensions;
using SecurityService.Application.DTOs;
using System.Text.Json;

namespace SecurityService.Tests.Unit.Filters;

/// <summary>
/// Unit tests for InvalidModelStateResponseFactory edge cases.
/// Requirements: 3.2, 3.3
/// </summary>
public class InvalidModelStateResponseFactoryTests
{
    private static readonly Func<ActionContext, IActionResult> Factory;

    static InvalidModelStateResponseFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiControllers();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>();
        Factory = options.Value.InvalidModelStateResponseFactory;
    }

    // ── 1. Empty ModelState does not produce field errors ──

    [Fact]
    public void Factory_WithEmptyModelState_ReturnsResponseWithEmptyDataArray()
    {
        // Arrange: ModelState with no errors at all
        var modelState = new ModelStateDictionary();
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "corr-empty";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);

        // Act
        var result = Factory(actionContext);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(422, objectResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("VALIDATION_ERROR", apiResponse.ErrorCode);
        Assert.Equal(1000, apiResponse.ErrorValue);
        Assert.Equal("96", apiResponse.ResponseCode);
        Assert.Equal("corr-empty", apiResponse.CorrelationId);

        // Data should be an empty list (no field errors to extract)
        Assert.NotNull(apiResponse.Data);
        var dataJson = JsonSerializer.Serialize(apiResponse.Data);
        using var doc = JsonDocument.Parse(dataJson);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    // ── 2. Single field with multiple errors produces multiple entries ──

    [Fact]
    public void Factory_SingleFieldMultipleErrors_ProducesMultipleDataEntries()
    {
        // Arrange: one field key with three distinct errors
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required.");
        modelState.AddModelError("Email", "Email must be a valid email address.");
        modelState.AddModelError("Email", "Email must not exceed 256 characters.");

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "corr-multi";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);

        // Act
        var result = Factory(actionContext);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(422, objectResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("VALIDATION_ERROR", apiResponse.ErrorCode);
        Assert.Equal(1000, apiResponse.ErrorValue);
        Assert.Equal("96", apiResponse.ResponseCode);
        Assert.Equal("corr-multi", apiResponse.CorrelationId);

        // Serialize Data to inspect the anonymous objects
        Assert.NotNull(apiResponse.Data);
        var dataJson = JsonSerializer.Serialize(apiResponse.Data);
        using var doc = JsonDocument.Parse(dataJson);
        var dataArray = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, dataArray.ValueKind);
        Assert.Equal(3, dataArray.GetArrayLength());

        // Each entry should have Field = "Email" and the corresponding message
        var expectedMessages = new[]
        {
            "Email is required.",
            "Email must be a valid email address.",
            "Email must not exceed 256 characters."
        };

        for (var i = 0; i < expectedMessages.Length; i++)
        {
            var item = dataArray[i];
            Assert.Equal("Email", item.GetProperty("Field").GetString());
            Assert.Equal(expectedMessages[i], item.GetProperty("Message").GetString());
        }
    }
}
