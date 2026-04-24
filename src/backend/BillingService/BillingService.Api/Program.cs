using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
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
// Authentication & Authorization
builder.Services.AddNexusAuthentication(appSettings);

// CORS
builder.Services.AddNexusCors(appSettings);

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
