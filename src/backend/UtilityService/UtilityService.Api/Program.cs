using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using UtilityService.Api.Extensions;
using UtilityService.Application.Validators;
using UtilityService.Infrastructure.Configuration;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var appSettings = AppSettings.FromEnvironment();
SerilogHelper.ConfigureLogging("UtilityService", appSettings.SeqUrl);
builder.Host.UseSerilog();

// Services
builder.Services.AddInfrastructureServices(appSettings);
builder.Services.AddApiControllers();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAuditLogRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddNexusAuthentication(appSettings);
builder.Services.AddNexusCors(appSettings);
builder.Services.AddHealthCheckServices();
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Startup
DatabaseMigrationHelper.ApplyMigrations(app);
await SeedDataHelper.SeedAsync(app);
app.UseSwaggerInDevelopment();
app.UseUtilityPipeline();
app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();
