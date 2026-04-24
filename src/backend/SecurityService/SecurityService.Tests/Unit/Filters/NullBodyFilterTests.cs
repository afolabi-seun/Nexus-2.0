using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using SecurityService.Api.Filters;
using SecurityService.Application.DTOs;

namespace SecurityService.Tests.Unit.Filters;

/// <summary>
/// Unit tests for NullBodyFilter edge cases.
/// Requirements: 2.2, 2.3
/// </summary>
public class NullBodyFilterTests
{
    // ── 1. No [FromBody] parameters → pass-through ──

    [Fact]
    public void OnActionExecuting_NoBodyParameters_PassesThrough()
    {
        var filter = new NullBodyFilter();

        // Parameter bound from Query, not Body
        var paramDescriptor = new ParameterDescriptor
        {
            Name = "id",
            ParameterType = typeof(int),
            BindingInfo = new BindingInfo
            {
                BindingSource = BindingSource.Query
            }
        };

        var context = CreateContext(
            parameters: new List<ParameterDescriptor> { paramDescriptor },
            arguments: new Dictionary<string, object?> { ["id"] = 42 }
        );

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    // ── 2. Multiple body parameters — first null is caught ──

    [Fact]
    public void OnActionExecuting_MultipleBodyParams_FirstNullCaught()
    {
        var filter = new NullBodyFilter();

        var firstParam = new ParameterDescriptor
        {
            Name = "request",
            ParameterType = typeof(object),
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Body }
        };

        var secondParam = new ParameterDescriptor
        {
            Name = "payload",
            ParameterType = typeof(object),
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Body }
        };

        var context = CreateContext(
            parameters: new List<ParameterDescriptor> { firstParam, secondParam },
            arguments: new Dictionary<string, object?>
            {
                ["request"] = null,       // first body param is null
                ["payload"] = new { X = 1 } // second body param is non-null
            }
        );

        filter.OnActionExecuting(context);

        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(422, objectResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("VALIDATION_ERROR", apiResponse.ErrorCode);
        Assert.Equal(1000, apiResponse.ErrorValue);
        Assert.Equal("99", apiResponse.ResponseCode);
        Assert.Equal("Request body is required.", apiResponse.Message);
    }

    // ── 3. CorrelationId included in response from HttpContext.Items ──

    [Fact]
    public void OnActionExecuting_NullBody_IncludesCorrelationIdFromHttpContext()
    {
        var filter = new NullBodyFilter();

        var paramDescriptor = new ParameterDescriptor
        {
            Name = "body",
            ParameterType = typeof(object),
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Body }
        };

        var context = CreateContext(
            parameters: new List<ParameterDescriptor> { paramDescriptor },
            arguments: new Dictionary<string, object?> { ["body"] = null },
            correlationId: "test-correlation-id"
        );

        filter.OnActionExecuting(context);

        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.Equal("test-correlation-id", apiResponse.CorrelationId);
    }

    // ── Helper ──

    private static ActionExecutingContext CreateContext(
        List<ParameterDescriptor> parameters,
        Dictionary<string, object?> arguments,
        string? correlationId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (correlationId is not null)
            httpContext.Items["CorrelationId"] = correlationId;

        var actionDescriptor = new ActionDescriptor
        {
            Parameters = parameters
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            actionDescriptor,
            new ModelStateDictionary()
        );

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            arguments,
            controller: null!
        );
    }
}
