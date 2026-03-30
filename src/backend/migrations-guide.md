# Database Migrations Guide

## Prerequisites
- .NET 8 SDK
- PostgreSQL running with databases: `nexus_security`, `nexus_profile`, `nexus_work`, `nexus_utility`, `nexus_billing`
- EF Core tools: `dotnet tool install --global dotnet-ef`

## Generate Initial Migration (per service)

```bash
cd src/backend/{ServiceName}/{ServiceName}.Api
dotnet ef migrations add InitialCreate --project ../{ServiceName}.Infrastructure --context {DbContextName}
```

## Apply Migrations

```bash
dotnet ef database update --project ../{ServiceName}.Infrastructure --context {DbContextName}
```

## Service-specific commands

### SecurityService
```bash
cd src/backend/SecurityService/SecurityService.Api
dotnet ef migrations add InitialCreate --project ../SecurityService.Infrastructure --context SecurityDbContext
dotnet ef database update --project ../SecurityService.Infrastructure --context SecurityDbContext
```

### ProfileService
```bash
cd src/backend/ProfileService/ProfileService.Api
dotnet ef migrations add InitialCreate --project ../ProfileService.Infrastructure --context ProfileDbContext
dotnet ef database update --project ../ProfileService.Infrastructure --context ProfileDbContext
```

### WorkService
```bash
cd src/backend/WorkService/WorkService.Api
dotnet ef migrations add InitialCreate --project ../WorkService.Infrastructure --context WorkDbContext
dotnet ef database update --project ../WorkService.Infrastructure --context WorkDbContext
```

### UtilityService
```bash
cd src/backend/UtilityService/UtilityService.Api
dotnet ef migrations add InitialCreate --project ../UtilityService.Infrastructure --context UtilityDbContext
dotnet ef database update --project ../UtilityService.Infrastructure --context UtilityDbContext
```

### BillingService
```bash
cd src/backend/BillingService/BillingService.Api
dotnet ef migrations add InitialCreate --project ../BillingService.Infrastructure --context BillingDbContext
dotnet ef database update --project ../BillingService.Infrastructure --context BillingDbContext
```

## Rollback a Migration

```bash
dotnet ef database update {PreviousMigrationName} --project ../{ServiceName}.Infrastructure --context {DbContextName}
```

## Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove --project ../{ServiceName}.Infrastructure --context {DbContextName}
```

## Notes

- Each service uses `DatabaseMigrationHelper.ApplyMigrations()` at startup to automatically apply pending migrations.
- In-memory databases (used in tests) skip migrations and call `EnsureCreated()` instead.
- The `Microsoft.EntityFrameworkCore.Design` package is included in each Api project to enable `dotnet ef` CLI commands.
