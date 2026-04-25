using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProfileService.Api.Extensions;
using ProfileService.Application.Validators;
using ProfileService.Infrastructure.Configuration;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var appSettings = AppSettings.FromEnvironment();
SerilogHelper.ConfigureLogging("ProfileService", appSettings.SeqUrl);
builder.Host.UseSerilog();

// Services
builder.Services.AddInfrastructureServices(appSettings);
builder.Services.AddApiControllers();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrganizationRequestValidator>();
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
app.UseProfilePipeline();
app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();
