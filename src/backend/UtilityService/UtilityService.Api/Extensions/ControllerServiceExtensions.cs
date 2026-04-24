using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using UtilityService.Api.Filters;
using UtilityService.Application.DTOs;

namespace UtilityService.Api.Extensions;

public static class ControllerServiceExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
            options.Filters.Add<PaginationFilter>();
            options.Filters.Add<NullBodyFilter>();
        })
        .AddJsonOptions(json =>
        {
            json.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = false;
            options.InvalidModelStateResponseFactory = context =>
            {
                var fieldErrors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(err => new
                    {
                        Field = e.Key,
                        Message = err.ErrorMessage
                    }))
                    .ToList();

                var correlationId = context.HttpContext.Items["CorrelationId"] as string;

                var response = new ApiResponse<object>
                {
                    Success = false,
                    ErrorCode = "VALIDATION_ERROR",
                    ErrorValue = 1000,
                    ResponseCode = "96",
                    ResponseDescription = "Validation error",
                    Message = "Validation error",
                    Data = fieldErrors,
                    CorrelationId = correlationId
                };

                return new ObjectResult(response) { StatusCode = 422 };
            };
        });

        return services;
    }
}
