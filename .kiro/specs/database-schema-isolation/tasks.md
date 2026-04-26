# Implementation Plan: Database Schema Isolation

## Overview

Migrate Nexus 2.0 from 5 separate PostgreSQL databases to a single shared database (`nexusDb`) with per-service schema isolation. This is a configuration-level change — no business logic, entities, or API contracts change. Each task builds incrementally: AppSettings → DbContext → DI → DesignTimeFactory → env files → Docker → verification.

## Tasks

- [ ] 1. Add DatabaseSchema property to all AppSettings and standardize UtilityService env vars
  - [ ] 1.1 Add `DatabaseSchema` property to SecurityService AppSettings
    - Add `public string? DatabaseSchema { get; set; }` property
    - Add `DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA")` to `FromEnvironment()`
    - _Requirements: 4.1, 4.6_

  - [ ] 1.2 Add `DatabaseSchema` property to ProfileService AppSettings
    - Add `public string? DatabaseSchema { get; set; }` property
    - Add `DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA")` to `FromEnvironment()`
    - _Requirements: 4.2, 4.6_

  - [ ] 1.3 Add `DatabaseSchema` property to BillingService AppSettings
    - Add `public string? DatabaseSchema { get; set; }` property
    - Add `DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA")` to `FromEnvironment()`
    - _Requirements: 4.3, 4.6_

  - [ ] 1.4 Add `DatabaseSchema` property to WorkService AppSettings
    - Add `public string? DatabaseSchema { get; set; }` property
    - Add `DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA")` to `FromEnvironment()`
    - _Requirements: 4.4, 4.6_

  - [ ] 1.5 Add `DatabaseSchema` property and standardize env var names in UtilityService AppSettings
    - Add `public string? DatabaseSchema { get; set; }` property
    - Add `DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA")` to `FromEnvironment()`
    - Change `GetRequired("DATABASE_URL")` to `GetRequired("DATABASE_CONNECTION_STRING")`
    - Change `GetRequired("REDIS_URL")` to `GetRequired("REDIS_CONNECTION_STRING")`
    - Change `GetRequired("JWT_SECRET")` to `GetRequired("JWT_SECRET_KEY")`
    - _Requirements: 4.5, 4.6, 8.1, 8.2, 8.3_

- [ ] 2. Add conditional HasDefaultSchema and InMemory detection to all DbContexts
  - [ ] 2.1 Update SecurityDbContext with schema support
    - Add `private readonly string? _databaseSchema` field and constructor parameter `string? databaseSchema = null`
    - In `OnModelCreating`, add `if (!string.IsNullOrEmpty(_databaseSchema) && !Database.IsInMemory()) { modelBuilder.HasDefaultSchema(_databaseSchema); }` before existing entity configurations
    - _Requirements: 2.1, 2.6, 9.1, 9.3_

  - [ ] 2.2 Update ProfileDbContext with schema support
    - Add `private readonly string? _databaseSchema` field and constructor parameter `string? databaseSchema = null`
    - In `OnModelCreating`, add the `HasDefaultSchema` + `IsInMemory()` guard before `base.OnModelCreating(modelBuilder)`
    - _Requirements: 2.2, 2.6, 9.1, 9.3_

  - [ ] 2.3 Update BillingDbContext with schema support
    - Add `private readonly string? _databaseSchema` field and constructor parameter `string? databaseSchema = null`
    - In `OnModelCreating`, add the `HasDefaultSchema` + `IsInMemory()` guard before `base.OnModelCreating(modelBuilder)`
    - _Requirements: 2.3, 2.6, 9.1, 9.3_

  - [ ] 2.4 Update WorkDbContext with schema support
    - Add `private readonly string? _databaseSchema` field and constructor parameter `string? databaseSchema = null`
    - In `OnModelCreating`, add the `HasDefaultSchema` + `IsInMemory()` guard before `base.OnModelCreating(modelBuilder)`
    - _Requirements: 2.4, 2.6, 9.1, 9.3_

  - [ ] 2.5 Update UtilityDbContext with schema support
    - Add `private readonly string? _databaseSchema` field and constructor parameter `string? databaseSchema = null`
    - In `OnModelCreating`, add the `HasDefaultSchema` + `IsInMemory()` guard before existing entity configurations
    - _Requirements: 2.5, 2.6, 9.1, 9.3_

- [ ] 3. Update DI registration with MigrationsHistoryTable in all services
  - [ ] 3.1 Update SecurityService DependencyInjection.cs
    - Change `AddDbContext<SecurityDbContext>` to use the lambda overload with `npgsql =>` options
    - When `appSettings.DatabaseSchema` is set, call `npgsql.MigrationsHistoryTable("__EFMigrationsHistory", appSettings.DatabaseSchema)`
    - Register `appSettings.DatabaseSchema` so the DbContext receives it (e.g., via factory or DI registration of the schema string)
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 3.2 Update ProfileService DependencyInjection.cs
    - Same pattern: add `MigrationsHistoryTable` configuration and pass schema to DbContext
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 3.3 Update BillingService DependencyInjection.cs
    - Same pattern: add `MigrationsHistoryTable` configuration and pass schema to DbContext
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 3.4 Update WorkService DependencyInjection.cs
    - Same pattern: add `MigrationsHistoryTable` configuration and pass schema to DbContext
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 3.5 Update UtilityService DependencyInjection.cs
    - Same pattern: add `MigrationsHistoryTable` configuration and pass schema to DbContext
    - _Requirements: 5.1, 5.2, 5.3_

- [ ] 4. Create or update DesignTimeDbContextFactory for all services
  - [ ] 4.1 Create DesignTimeDbContextFactory for SecurityService
    - Create `SecurityService.Infrastructure/Data/DesignTimeDbContextFactory.cs`
    - Implement `IDesignTimeDbContextFactory<SecurityDbContext>`
    - Load `.env` via `DotNetEnv.Env.Load()`, read `DATABASE_CONNECTION_STRING` and `DATABASE_SCHEMA`
    - Configure `UseNpgsql` with `MigrationsHistoryTable` when schema is set
    - Pass schema to `SecurityDbContext` constructor
    - Fallback connection string: `Host=localhost;Port=5432;Database=nexusDb;Username=postgres;Password=pass.123`
    - _Requirements: 6.2, 6.3_

  - [ ] 4.2 Create DesignTimeDbContextFactory for ProfileService
    - Create `ProfileService.Infrastructure/Data/DesignTimeDbContextFactory.cs`
    - Same pattern as 4.1 but for `ProfileDbContext`
    - _Requirements: 6.2, 6.3_

  - [ ] 4.3 Create DesignTimeDbContextFactory for BillingService
    - Create `BillingService.Infrastructure/Data/DesignTimeDbContextFactory.cs`
    - Same pattern as 4.1 but for `BillingDbContext`
    - _Requirements: 6.2, 6.3_

  - [ ] 4.4 Create DesignTimeDbContextFactory for WorkService
    - Create `WorkService.Infrastructure/Data/DesignTimeDbContextFactory.cs`
    - Same pattern as 4.1 but for `WorkDbContext`
    - _Requirements: 6.2, 6.3_

  - [ ] 4.5 Update UtilityService DesignTimeDbContextFactory
    - Change `DATABASE_URL` to `DATABASE_CONNECTION_STRING`
    - Add `DATABASE_SCHEMA` reading and `MigrationsHistoryTable` configuration
    - Pass schema to `UtilityDbContext` constructor
    - Update fallback connection string to `Database=nexusDb`
    - _Requirements: 6.1, 6.3, 8.5_

- [ ] 5. Checkpoint — Verify code compiles
  - Ensure all projects build successfully with `dotnet build Nexus-2.0.sln`
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Update environment configuration files for all services
  - [ ] 6.1 Update SecurityService environment files
    - Add `DATABASE_SCHEMA=nexus_security` and update `Database=nexusDb` with `SearchPath=nexus_security` in: `.env`, `.env.example`, `config/development/security-service.env`, `config/staging/security-service.env`, `config/production/security-service.env`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 3.1, 3.3_

  - [ ] 6.2 Update ProfileService environment files
    - Add `DATABASE_SCHEMA=nexus_profile` and update `Database=nexusDb` with `SearchPath=nexus_profile` in: `.env`, `.env.example`, `config/development/profile-service.env`, `config/staging/profile-service.env`, `config/production/profile-service.env`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 3.1, 3.3_

  - [ ] 6.3 Update BillingService environment files
    - Add `DATABASE_SCHEMA=nexus_billing` and update `Database=nexusDb` with `SearchPath=nexus_billing` in: `.env`, `.env.example`, `config/development/billing-service.env`, `config/staging/billing-service.env`, `config/production/billing-service.env`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 3.1, 3.3_

  - [ ] 6.4 Update WorkService environment files
    - Add `DATABASE_SCHEMA=nexus_work` and update `Database=nexusDb` with `SearchPath=nexus_work` in: `.env`, `.env.example`, `config/development/work-service.env`, `config/staging/work-service.env`, `config/production/work-service.env`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 3.1, 3.3_

  - [ ] 6.5 Update UtilityService environment files (with env var rename)
    - Add `DATABASE_SCHEMA=nexus_utility` and update `Database=nexusDb` with `SearchPath=nexus_utility`
    - Rename `DATABASE_URL` → `DATABASE_CONNECTION_STRING`, `REDIS_URL` → `REDIS_CONNECTION_STRING`, `JWT_SECRET` → `JWT_SECRET_KEY`
    - Apply to: `.env`, `.env.example`, `config/development/utility-service.env`, `config/staging/utility-service.env`, `config/production/utility-service.env`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.4, 3.1, 3.3_

- [ ] 7. Update Docker init script and docker-compose files
  - [ ] 7.1 Replace `docker/init-databases.sql` with shared database + schemas
    - Replace 5 `CREATE DATABASE` statements with: `CREATE DATABASE "nexusDb"`, `\c "nexusDb"`, then 5 `CREATE SCHEMA IF NOT EXISTS` + `GRANT ALL ON SCHEMA` statements
    - _Requirements: 1.1, 1.2, 1.3_

  - [ ] 7.2 Update `docker/docker-compose.yml`
    - Update all 5 services: `Database=nexusDb` + `SearchPath={schema}` in connection strings, add `DATABASE_SCHEMA` env var
    - Rename UtilityService env vars: `DATABASE_URL` → `DATABASE_CONNECTION_STRING`, `REDIS_URL` → `REDIS_CONNECTION_STRING`, `JWT_SECRET` → `JWT_SECRET_KEY`
    - _Requirements: 10.1, 10.2, 10.3, 7.6_

  - [ ] 7.3 Update `docker/docker-compose.local.yml`
    - Same changes as 7.2 but with `host.docker.internal` host
    - _Requirements: 10.4, 7.6_

  - [ ] 7.4 Update `docker/docker-compose.server.yml`
    - Same changes as 7.2 but with `host.docker.internal` host
    - _Requirements: 10.4, 7.6_

- [ ] 8. Final checkpoint — Build and verify
  - Run `dotnet build Nexus-2.0.sln` to confirm everything compiles
  - Run existing test suite to confirm no regressions (InMemory provider tests should pass unchanged)
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Property-based tests for schema application logic
  - [ ]* 9.1 Write property test: Schema application with Npgsql provider
    - **Property 1: Schema application with Npgsql provider**
    - For any non-empty schema string, when a DbContext is configured with the Npgsql provider and that schema value, the EF Core model's default schema SHALL equal the provided schema string
    - Use FsCheck.Xunit with minimum 100 iterations
    - Tag: `Feature: database-schema-isolation, Property 1: Schema application with Npgsql provider`
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**

  - [ ]* 9.2 Write property test: InMemory provider skips schema
    - **Property 2: InMemory provider skips schema**
    - For any schema string (including non-empty), when a DbContext is configured with the InMemory provider, the EF Core model SHALL have no default schema set
    - Use FsCheck.Xunit with minimum 100 iterations
    - Tag: `Feature: database-schema-isolation, Property 2: InMemory provider skips schema`
    - **Validates: Requirements 9.1, 9.3**

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The design uses C# throughout — all code changes target the existing .NET/EF Core codebase
- UtilityService requires extra attention: env var rename (`DATABASE_URL` → `DATABASE_CONNECTION_STRING`, etc.) must be applied consistently across code and all config files
- Each DbContext change preserves backward compatibility — the `databaseSchema` parameter defaults to `null`
- The `Database.IsInMemory()` guard ensures existing InMemory-based tests continue working without modification
- Checkpoints at tasks 5 and 8 ensure incremental validation
