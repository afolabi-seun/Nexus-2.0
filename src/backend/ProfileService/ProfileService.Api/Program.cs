using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProfileService.Api.Extensions;
using ProfileService.Application.Validators;
using ProfileService.Infrastructure.Configuration;
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
    .Enrich.WithProperty("ServiceName", "ProfileService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ProfileService | {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(appSettings.SeqUrl ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Infrastructure services (EF Core, Redis, domain services, HTTP clients)
builder.Services.AddInfrastructureServices(appSettings);

// Controllers
builder.Services.AddApiControllers();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrganizationRequestValidator>();
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

// Seed reference data (roles, notification types, navigation, platform admin)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProfileService.Infrastructure.Data.ProfileDbContext>();
    await ProfileService.Infrastructure.Data.SeedData.SeedAllAsync(context);
}

// Swagger UI (Development only)
app.UseSwaggerInDevelopment();

// Profile middleware pipeline
app.UseProfilePipeline();

// Map controllers
app.MapControllers();

// Map health check endpoints
app.MapHealthCheckEndpoints();

app.Run();
