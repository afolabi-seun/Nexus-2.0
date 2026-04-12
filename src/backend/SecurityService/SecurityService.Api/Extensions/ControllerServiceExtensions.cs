using SecurityService.Api.Filters;

namespace SecurityService.Api.Extensions;

public static class ControllerServiceExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
            options.Filters.Add<PaginationFilter>();
        });

        return services;
    }
}
