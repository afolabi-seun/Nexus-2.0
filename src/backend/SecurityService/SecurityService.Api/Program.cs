using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using SecurityService.Api.Extensions;
using SecurityService.Application.Validators;
using SecurityService.Infrastructure.Configuration;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var appSettings = AppSettings.FromEnvironment();
SerilogHelper.ConfigureLogging("SecurityService", appSettings.SeqUrl);
builder.Host.UseSerilog();

// Services
builder.Services.AddInfrastructureServices(appSettings);
builder.Services.AddApiControllers();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddNexusAuthentication(appSettings);
builder.Services.AddNexusCors(appSettings);
builder.Services.AddHealthCheckServices();
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Startup
DatabaseMigrationHelper.ApplyMigrations(app);
app.UseSwaggerInDevelopment();
app.UseSecurityPipeline();
app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();
