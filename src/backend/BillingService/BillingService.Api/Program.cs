using System.Text;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BillingService.Api.Extensions;
using BillingService.Application.Validators;
using BillingService.Infrastructure.Configuration;
using Serilog;
using Serilog.Events;

// Load environment variables
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Build configuration from environment
var appSettings = AppSettings.FromEnvironment();

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("ServiceName", "BillingService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] BillingService | {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(appSettings.SeqUrl ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure services (EF Core, Redis, Stripe, domain services, HTTP clients)
builder.Services.AddInfrastructureServices(appSettings);

// Controllers
builder.Services.AddApiControllers();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateSubscriptionRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// JWT Bearer Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                var errorCode = context.ErrorDescription?.Contains("expired") == true ? "TOKEN_EXPIRED" : "INVALID_TOKEN";
                var body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    responseCode = "03",
                    responseDescription = message,
                    success = false,
                    data = (object?)null,
                    errorCode,
                    errorValue = 2024,
                    message,
                    correlationId,
                });
                await context.Response.WriteAsync(body);
            }
        };
    });

builder.Services.AddAuthorization();

// CORS
var corsOrigins = new List<string>();
if (!string.IsNullOrWhiteSpace(appSettings.FrontendUrl))
    corsOrigins.Add(appSettings.FrontendUrl);
corsOrigins.AddRange(appSettings.AllowedOrigins);
var distinctOrigins = corsOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("NexusPolicy", policy =>
    {
        if (distinctOrigins.Length > 0)
        {
            policy.WithOrigins(distinctOrigins)
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-Correlation-Id")
                  .WithExposedHeaders("X-Correlation-Id")
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Health checks
builder.Services.AddHealthCheckServices();

// Swagger
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Apply database migrations and seed plans
DatabaseMigrationHelper.ApplyMigrations(app);

// Swagger UI (Development only)
app.UseSwaggerInDevelopment();

// Billing middleware pipeline
app.UseBillingPipeline();

// Map controllers
app.MapControllers();

// Map health check endpoints
app.MapHealthCheckEndpoints();

app.Run();
