# Requirements Document

## Introduction

Migrate the Nexus 2.0 backend from 5 separate PostgreSQL databases (one per service) to a single shared database with per-service PostgreSQL schema isolation. Each service will own a dedicated schema (e.g., `nexus_security`, `nexus_profile`) within one database, configured via environment variables and EF Core's `HasDefaultSchema()`. This consolidation simplifies infrastructure, reduces connection overhead, and enables future cross-service query capabilities while maintaining strict data isolation between services.

## Glossary

- **Shared_Database**: The single PostgreSQL database instance (e.g., `nexusDb`) that replaces the current 5 separate databases
- **Service_Schema**: A PostgreSQL schema within the Shared_Database that isolates all tables, indexes, and migration history for a single backend service (e.g., `nexus_security`, `nexus_profile`, `nexus_billing`, `nexus_work`, `nexus_utility`)
- **DbContext**: The EF Core database context class in each service that manages entity mappings and database operations (e.g., `SecurityDbContext`, `ProfileDbContext`, `BillingDbContext`, `WorkDbContext`, `UtilityDbContext`)
- **HasDefaultSchema**: The EF Core `ModelBuilder.HasDefaultSchema(string)` method called in `OnModelCreating` that qualifies all generated SQL with the specified schema name
- **SearchPath**: The Npgsql connection string parameter that sets the PostgreSQL `search_path` for the connection, directing unqualified table references to the specified schema
- **DATABASE_SCHEMA**: The environment variable added to each service that holds the schema name (e.g., `nexus_profile`)
- **Migrations_History_Table**: The EF Core `__EFMigrationsHistory` table that tracks applied migrations; must be scoped to each Service_Schema
- **DesignTimeDbContextFactory**: The `IDesignTimeDbContextFactory<T>` implementation used by EF Core CLI tooling to create a DbContext at design time for migration generation
- **Init_Script**: The `docker/init-databases.sql` file executed on first PostgreSQL container startup to initialize the database structure
- **InMemory_Provider**: The EF Core `UseInMemoryDatabase` provider used in unit and property tests, which does not support PostgreSQL schemas
- **Connection_String**: The Npgsql connection string containing Host, Port, Database, Username, Password, and optionally SearchPath and other parameters
- **AppSettings**: The configuration class in each service's Infrastructure layer that reads environment variables and exposes them as typed properties

## Requirements

### Requirement 1: Shared Database Initialization

**User Story:** As a developer, I want the Docker initialization script to create a single shared database with per-service schemas, so that all services can operate within one database with proper isolation.

#### Acceptance Criteria

1. WHEN the PostgreSQL container starts for the first time, THE Init_Script SHALL create a single Shared_Database instead of 5 separate databases
2. WHEN the Shared_Database is created, THE Init_Script SHALL create 5 Service_Schemas: `nexus_security`, `nexus_profile`, `nexus_billing`, `nexus_work`, and `nexus_utility`
3. WHEN a Service_Schema is created, THE Init_Script SHALL grant full usage and create privileges on the schema to the database user

### Requirement 2: DbContext Schema Configuration

**User Story:** As a developer, I want each service's DbContext to use `HasDefaultSchema()` in `OnModelCreating`, so that all tables, indexes, and constraints are created within the service's dedicated schema.

#### Acceptance Criteria

1. THE SecurityDbContext SHALL call `HasDefaultSchema` with the value from the DATABASE_SCHEMA environment variable in its `OnModelCreating` method
2. THE ProfileDbContext SHALL call `HasDefaultSchema` with the value from the DATABASE_SCHEMA environment variable in its `OnModelCreating` method
3. THE BillingDbContext SHALL call `HasDefaultSchema` with the value from the DATABASE_SCHEMA environment variable in its `OnModelCreating` method
4. THE WorkDbContext SHALL call `HasDefaultSchema` with the value from the DATABASE_SCHEMA environment variable in its `OnModelCreating` method
5. THE UtilityDbContext SHALL call `HasDefaultSchema` with the value from the DATABASE_SCHEMA environment variable in its `OnModelCreating` method
6. WHEN the DATABASE_SCHEMA environment variable is not set, THE DbContext SHALL omit the `HasDefaultSchema` call to preserve backward compatibility with multi-database deployments

### Requirement 3: Connection String Schema Configuration

**User Story:** As a developer, I want each service's connection string to include the `SearchPath` parameter pointing to its schema, so that PostgreSQL resolves unqualified table names to the correct schema.

#### Acceptance Criteria

1. WHEN the DATABASE_SCHEMA environment variable is set, THE Connection_String for each service SHALL include `SearchPath={schema_value}` as a parameter
2. WHEN the DATABASE_SCHEMA environment variable is not set, THE Connection_String SHALL remain unchanged to support multi-database deployments
3. THE Connection_String for all services SHALL point to the same Shared_Database name

### Requirement 4: DATABASE_SCHEMA Environment Variable

**User Story:** As a developer, I want a `DATABASE_SCHEMA` environment variable added to each service, so that the schema name is configurable per environment without code changes.

#### Acceptance Criteria

1. THE AppSettings class for SecurityService SHALL read the DATABASE_SCHEMA environment variable and expose it as a typed property
2. THE AppSettings class for ProfileService SHALL read the DATABASE_SCHEMA environment variable and expose it as a typed property
3. THE AppSettings class for BillingService SHALL read the DATABASE_SCHEMA environment variable and expose it as a typed property
4. THE AppSettings class for WorkService SHALL read the DATABASE_SCHEMA environment variable and expose it as a typed property
5. THE AppSettings class for UtilityService SHALL read the DATABASE_SCHEMA environment variable and expose it as a typed property
6. THE DATABASE_SCHEMA environment variable SHALL be optional to support both single-database (with schema) and multi-database (without schema) deployments

### Requirement 5: EF Core Migrations History Table Scoping

**User Story:** As a developer, I want the EF Core migrations history table to be scoped to each service's schema, so that each service tracks its own migration history independently within the shared database.

#### Acceptance Criteria

1. WHEN the DATABASE_SCHEMA environment variable is set, THE DbContext registration SHALL configure the Migrations_History_Table to reside in the Service_Schema using `MigrationsHistoryTable("__EFMigrationsHistory", schema)`
2. WHEN migrations are applied for a service, THE Migrations_History_Table SHALL be created in that service's Service_Schema rather than in the `public` schema
3. WHEN multiple services apply migrations to the Shared_Database, each service's Migrations_History_Table SHALL be independent and not conflict with other services

### Requirement 6: DesignTimeDbContextFactory Updates

**User Story:** As a developer, I want the DesignTimeDbContextFactory to support schema configuration, so that I can generate EF Core migrations that target the correct schema.

#### Acceptance Criteria

1. WHEN the DATABASE_SCHEMA environment variable is set, THE DesignTimeDbContextFactory for UtilityService SHALL configure the DbContext with the schema-scoped Migrations_History_Table
2. THE DesignTimeDbContextFactory SHALL be created for each service that does not already have one (SecurityService, ProfileService, BillingService, WorkService)
3. WHEN the DATABASE_SCHEMA environment variable is set, each DesignTimeDbContextFactory SHALL pass the schema value to the DbContext so that `HasDefaultSchema` is applied during migration generation

### Requirement 7: Environment Configuration File Updates

**User Story:** As a developer, I want all environment configuration files updated to use the shared database and schema settings, so that local development, staging, and production environments all use the new schema-based isolation.

#### Acceptance Criteria

1. THE development environment files for all 5 services SHALL include the DATABASE_SCHEMA variable set to the appropriate schema name (`nexus_security`, `nexus_profile`, `nexus_billing`, `nexus_work`, `nexus_utility`)
2. THE development environment files for all 5 services SHALL update the database name in the Connection_String to the Shared_Database name
3. THE staging environment files for all 5 services SHALL include the DATABASE_SCHEMA variable set to the appropriate schema name
4. THE production environment files for all 5 services SHALL include the DATABASE_SCHEMA variable set to the appropriate schema name
5. THE `.env` and `.env.example` files in each service's Api directory SHALL include the DATABASE_SCHEMA variable
6. THE docker-compose environment sections for all 5 services SHALL include the DATABASE_SCHEMA variable and update the database name to the Shared_Database name

### Requirement 8: UtilityService Environment Variable Standardization

**User Story:** As a developer, I want the UtilityService to use the same environment variable naming convention as the other 4 services, so that configuration is consistent across all services.

#### Acceptance Criteria

1. THE UtilityService AppSettings SHALL read `DATABASE_CONNECTION_STRING` instead of `DATABASE_URL` for the database connection string
2. THE UtilityService AppSettings SHALL read `REDIS_CONNECTION_STRING` instead of `REDIS_URL` for the Redis connection string
3. THE UtilityService AppSettings SHALL read `JWT_SECRET_KEY` instead of `JWT_SECRET` for the JWT secret
4. WHEN the UtilityService environment variable names are changed, all environment configuration files (development, staging, production, `.env`, `.env.example`, docker-compose) SHALL be updated to use the new variable names
5. THE UtilityService DesignTimeDbContextFactory SHALL read `DATABASE_CONNECTION_STRING` instead of `DATABASE_URL`

### Requirement 9: Test Compatibility

**User Story:** As a developer, I want existing tests using the InMemory database provider to continue working without modification, so that the schema migration does not break the test suite.

#### Acceptance Criteria

1. WHEN a DbContext is created with the InMemory_Provider, THE DbContext SHALL not apply `HasDefaultSchema` since the InMemory_Provider does not support PostgreSQL schemas
2. THE existing unit tests and property tests across all services that use `UseInMemoryDatabase` SHALL continue to pass without modification
3. WHEN the DbContext detects it is using the InMemory_Provider, THE DbContext SHALL skip the `HasDefaultSchema` call

### Requirement 10: Docker Compose Configuration

**User Story:** As a developer, I want the Docker Compose files updated to use the shared database configuration, so that the local development environment matches the new schema-based architecture.

#### Acceptance Criteria

1. THE docker-compose.yml SHALL configure all 5 services to connect to the same Shared_Database
2. THE docker-compose.yml SHALL include the DATABASE_SCHEMA environment variable for each service with the correct schema name
3. THE docker-compose.yml SHALL update the UtilityService environment variables to use the standardized naming convention (`DATABASE_CONNECTION_STRING`, `REDIS_CONNECTION_STRING`, `JWT_SECRET_KEY`)
4. WHEN the docker-compose.local.yml or docker-compose.server.yml files contain database configuration, they SHALL be updated to match the new shared database pattern
