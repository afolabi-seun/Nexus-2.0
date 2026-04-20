using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using SecurityService.Api.Extensions;
using SecurityService.Application.Validators;
using SecurityService.Infrastructure.Configuration;
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
    .Enrich.WithProperty("ServiceName", "SecurityService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] SecurityService | {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(appSettings.SeqUrl ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure services (EF Core, Redis, domain services, HTTP clients)
builder.Services.AddInfrastructureServices(appSettings);

// Controllers
builder.Services.AddApiControllers();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Authentication & Authorization
builder.Services.AddNexusAuthentication(appSettings);

// CORS
builder.Services.AddNexusCors(appSettings);

// Health checks
builder.Services.AddHealthCheckServices();

// Swagger (registered always, but UI only in Development)
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Apply database migrations
DatabaseMigrationHelper.ApplyMigrations(app);

// Swagger UI (Development only)
app.UseSwaggerInDevelopment();

// Security middleware pipeline
app.UseSecurityPipeline();

// Map controllers
app.MapControllers();

// Map health check endpoints
app.MapHealthCheckEndpoints();

app.Run();
