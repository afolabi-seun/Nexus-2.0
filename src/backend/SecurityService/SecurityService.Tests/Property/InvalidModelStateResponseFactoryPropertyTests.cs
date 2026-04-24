using FsCheck.Xunit;
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

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for InvalidModelStateResponseFactory structured output.
/// Feature: architecture-hardening, Property 3: InvalidModelStateResponseFactory structured output
/// **Validates: Requirements 3.2, 3.3, 3.4**
/// </summary>
public class InvalidModelStateResponseFactoryPropertyTests
{
    private static readonly Func<ActionContext, IActionResult> Factory;

    static InvalidModelStateResponseFactoryPropertyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiControllers();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>();
        Factory = options.Value.InvalidModelStateResponseFactory;
    }

    /// <summary>
    /// For any ActionContext with a non-empty ModelState containing field errors
    /// and any correlationId string in HttpContext.Items, the factory produces a
    /// 422 ObjectResult with responseCode "96", errorCode "VALIDATION_ERROR",
    /// errorValue 1000, a data array with one { field, message } per error,
    /// and the correlationId from HttpContext.Items.
    /// **Validates: Requirements 3.2, 3.3, 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Factory_Returns422_WithCorrectStructure_ForAnyModelStateErrors(ushort seed)
    {
        var rng = new Random(seed);

        // Generate random correlation ID
        var correlationId = $"corr-{seed}-{rng.Next(100000)}";

        // Generate random field errors (1-5 fields, 1-3 errors each)
        var fieldCount = rng.Next(1, 6);
        var modelState = new ModelStateDictionary();

        for (var f = 0; f < fieldCount; f++)
        {
            var fieldName = $"Field{rng.Next(10000)}";
            var errorCount = rng.Next(1, 4);
            for (var e = 0; e < errorCount; e++)
            {
                var message = $"Error message {rng.Next(100000)}";
                modelState.AddModelError(fieldName, message);
            }
        }

        // Build expected errors from ModelState the same way the factory does
        var expectedErrors = modelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => new
            {
                Field = e.Key,
                Message = err.ErrorMessage
            }))
            .ToList();

        // Build ActionContext with populated ModelState and CorrelationId
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = correlationId;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);

        // Invoke the factory
        var result = Factory(actionContext);

        // Assert: must be ObjectResult with StatusCode 422
        if (result is not ObjectResult objectResult) return false;
        if (objectResult.StatusCode != 422) return false;

        // Assert: value must be ApiResponse<object>
        if (objectResult.Value is not ApiResponse<object> apiResponse) return false;

        // Assert: fixed fields
        if (apiResponse.Success != false) return false;
        if (apiResponse.ResponseCode != "96") return false;
        if (apiResponse.ErrorCode != "VALIDATION_ERROR") return false;
        if (apiResponse.ErrorValue != 1000) return false;
        if (apiResponse.CorrelationId != correlationId) return false;

        // Assert: data array has one { Field, Message } per ModelState error
        if (apiResponse.Data is null) return false;

        // Serialize and re-parse Data to inspect the anonymous objects
        var dataJson = JsonSerializer.Serialize(apiResponse.Data);
        using var doc = JsonDocument.Parse(dataJson);
        var dataArray = doc.RootElement;

        if (dataArray.ValueKind != JsonValueKind.Array) return false;
        if (dataArray.GetArrayLength() != expectedErrors.Count) return false;

        for (var i = 0; i < expectedErrors.Count; i++)
        {
            var item = dataArray[i];
            if (!item.TryGetProperty("Field", out var fieldProp)) return false;
            if (!item.TryGetProperty("Message", out var msgProp)) return false;
            if (fieldProp.GetString() != expectedErrors[i].Field) return false;
            if (msgProp.GetString() != expectedErrors[i].Message) return false;
        }

        return true;
    }
}
