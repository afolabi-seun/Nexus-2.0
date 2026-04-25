using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using WorkService.Api.Extensions;
using WorkService.Application.Validators;
using WorkService.Infrastructure.Configuration;
using Serilog;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var appSettings = AppSettings.FromEnvironment();
SerilogHelper.ConfigureLogging("WorkService", appSettings.SeqUrl);
builder.Host.UseSerilog();

// Services
builder.Services.AddInfrastructureServices(appSettings);
builder.Services.AddApiControllers();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddNexusAuthentication(appSettings);
builder.Services.AddNexusCors(appSettings);
builder.Services.AddHealthCheckServices();
builder.Services.AddSwaggerServices();

var app = builder.Build();

// Startup
DatabaseMigrationHelper.ApplyMigrations(app);
app.UseSwaggerInDevelopment();
app.UseWorkServicePipeline();
app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();
