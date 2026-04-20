using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WorkService.Domain.Exceptions;
using WorkService.Infrastructure.Configuration;

namespace WorkService.Api.Extensions;

/// <summary>
/// Configures JWT Bearer authentication with proper error responses.
/// </summary>
public static class AuthServiceExtensions
{
    public static IServiceCollection AddNexusAuthentication(this IServiceCollection services, AppSettings appSettings)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = appSettings.JwtIssuer,
                    ValidAudience = appSettings.JwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(appSettings.JwtSecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/problem+json";
                        var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "";
                        var message = "Authentication required.";
                        if (context.ErrorDescription?.Contains("expired") == true)
                            message = "Token has expired. Please refresh your session.";
                        else if (context.Error == "invalid_token")
                            message = "Invalid or malformed token.";
                        var errorCode = context.ErrorDescription?.Contains("expired") == true
                            ? ErrorCodes.TokenExpired : ErrorCodes.InvalidToken;
                        var errorValue = context.ErrorDescription?.Contains("expired") == true
                            ? ErrorCodes.TokenExpiredValue : ErrorCodes.InvalidTokenValue;
                        var body = JsonSerializer.Serialize(new
                        {
                            responseCode = "03",
                            responseDescription = message,
                            success = false,
                            data = (object?)null,
                            errorCode,
                            errorValue,
                            message,
                            correlationId,
                        });
                        await context.Response.WriteAsync(body);
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
