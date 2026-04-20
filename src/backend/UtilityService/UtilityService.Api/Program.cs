using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using UtilityService.Api.Extensions;
using UtilityService.Application.Validators;
using UtilityService.Infrastructure.Configuration;
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
    .Enrich.WithProperty("ServiceName", "UtilityService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] UtilityService | {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(appSettings.SeqUrl ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure services (EF Core, Redis, domain services, background services)
builder.Services.AddInfrastructureServices(appSettings);

// Controllers
builder.Services.AddApiControllers();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateAuditLogRequestValidator>();
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

// Swagger
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Apply database migrations and seed data
DatabaseMigrationHelper.ApplyMigrations(app);
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UtilityService.Infrastructure.Data.UtilityDbContext>();
    await SeedDataHelper.SeedAsync(context);
}

// Swagger UI (Development only)
app.UseSwaggerInDevelopment();

// Utility middleware pipeline (no FirstTimeUser, no RateLimiter)
app.UseUtilityPipeline();

// Map controllers
app.MapControllers();

// Map health check endpoints
app.MapHealthCheckEndpoints();

app.Run();
