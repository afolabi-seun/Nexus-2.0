using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using BillingService.Api.Extensions;
using BillingService.Application.Validators;
using BillingService.Infrastructure.Configuration;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var appSettings = AppSettings.FromEnvironment();
SerilogHelper.ConfigureLogging("BillingService", appSettings.SeqUrl);
builder.Host.UseSerilog();

// Services
builder.Services.AddInfrastructureServices(appSettings);
builder.Services.AddApiControllers();
builder.Services.AddValidatorsFromAssemblyContaining<CreateSubscriptionRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddNexusAuthentication(appSettings);
builder.Services.AddNexusCors(appSettings);
builder.Services.AddHealthCheckServices();
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Startup
DatabaseMigrationHelper.ApplyMigrations(app);
app.UseSwaggerInDevelopment();
app.UseBillingPipeline();
app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();
