namespace SecurityService.Api.Extensions;

public static class ControllerServiceExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            // Suppress default model state invalid filter — FluentValidation handles validation
            options.SuppressAsyncSuffixInActionNames = false;
        });

        return services;
    }
}
