using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProfileService.Application.DTOs;

namespace ProfileService.Api.Filters;

public class NullBodyFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var param in context.ActionDescriptor.Parameters)
        {
            if (param.BindingInfo?.BindingSource?.Id == "Body" &&
                context.ActionArguments.TryGetValue(param.Name, out var value) &&
                value == null)
            {
                var correlationId = context.HttpContext.Items["CorrelationId"] as string;
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false,
                    ErrorCode = "VALIDATION_ERROR",
                    ErrorValue = 1000,
                    ResponseCode = "99",
                    ResponseDescription = "Validation failed",
                    Message = "Request body is required.",
                    CorrelationId = correlationId
                })
                { StatusCode = 422 };
                return;
            }
        }
    }
}
