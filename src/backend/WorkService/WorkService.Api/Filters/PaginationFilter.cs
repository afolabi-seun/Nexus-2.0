using Microsoft.AspNetCore.Mvc.Filters;

namespace WorkService.Api.Filters;

public class PaginationFilter : IActionFilter
{
    private const int MaxPageSize = 100;
    private const int MinPage = 1;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("pageSize", out var ps) && ps is int pageSize)
            context.ActionArguments["pageSize"] = Math.Clamp(pageSize, 1, MaxPageSize);

        if (context.ActionArguments.TryGetValue("page", out var p) && p is int page)
            context.ActionArguments["page"] = Math.Max(page, MinPage);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
