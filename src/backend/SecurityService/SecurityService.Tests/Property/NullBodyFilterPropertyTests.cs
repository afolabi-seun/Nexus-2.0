using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using SecurityService.Api.Filters;
using SecurityService.Application.DTOs;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for NullBodyFilter null-body gating.
/// Feature: architecture-hardening, Property 2: NullBodyFilter null-body gating
/// **Validates: Requirements 2.2, 2.3**
/// </summary>
public class NullBodyFilterPropertyTests
{
    /// <summary>
    /// For any ActionExecutingContext with a [FromBody] parameter,
    /// the NullBodyFilter sets context.Result to a 422 ApiResponse with
    /// errorCode "VALIDATION_ERROR" if and only if the body parameter is null.
    /// When the body is non-null, context.Result remains unset.
    /// **Validates: Requirements 2.2, 2.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NullBody_Returns422_NonNullBody_PassesThrough(ushort seed)
    {
        var rng = new Random(seed);
        var bodyIsNull = rng.Next(2) == 0;
        var paramName = $"request{rng.Next(1000)}";

        var filter = new NullBodyFilter();
        var context = CreateActionExecutingContext(paramName, bodyIsNull);

        filter.OnActionExecuting(context);

        if (bodyIsNull)
        {
            // Body is null → must set Result to 422 with correct ApiResponse
            if (context.Result is not ObjectResult objectResult) return false;
            if (objectResult.StatusCode != 422) return false;
            if (objectResult.Value is not ApiResponse<object> apiResponse) return false;
            if (apiResponse.ErrorCode != "VALIDATION_ERROR") return false;
            if (apiResponse.ErrorValue != 1000) return false;
            if (apiResponse.ResponseCode != "99") return false;
            if (apiResponse.Success != false) return false;
            return true;
        }
        else
        {
            // Body is non-null → Result must remain null (pass-through)
            return context.Result is null;
        }
    }

    private static ActionExecutingContext CreateActionExecutingContext(string paramName, bool bodyIsNull)
    {
        var httpContext = new DefaultHttpContext();

        var paramDescriptor = new ParameterDescriptor
        {
            Name = paramName,
            ParameterType = typeof(object),
            BindingInfo = new BindingInfo
            {
                BindingSource = BindingSource.Body
            }
        };

        var actionDescriptor = new ActionDescriptor
        {
            Parameters = new List<ParameterDescriptor> { paramDescriptor }
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor, new ModelStateDictionary());

        var actionArguments = new Dictionary<string, object?>();
        if (bodyIsNull)
        {
            actionArguments[paramName] = null;
        }
        else
        {
            actionArguments[paramName] = new { Value = "non-null-body" };
        }

        var filters = new List<IFilterMetadata>();

        return new ActionExecutingContext(actionContext, filters, actionArguments, controller: null!);
    }
}
