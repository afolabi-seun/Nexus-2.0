# Implementation Plan: UtilityService

## Overview

Incremental implementation of the UtilityService microservice following Clean Architecture (.NET 8) with four projects: Domain, Application, Infrastructure, and Api, plus a co-located test project. All projects live under `src/backend/UtilityService/` in the Nexus-2.0 monorepo. Tasks build from the innermost layer (Domain) outward, wiring everything together in Program.cs at the end. All code is C# targeting `net8.0`.

UtilityService is the cross-cutting operational microservice — it provides audit logging, error logging with PII redaction, notification dispatch, error code registry, reference data management, retention archival, and outbox processing. It is an internal/service-to-service platform with no `FirstTimeUserMiddleware` or `RateLimiterMiddleware` in its pipeline. It includes 4 background hosted services and 16 notification template files.

## Tasks

- [-] 1. Solution and project scaffolding
  - [ ] 1.1 Create monorepo folder structure and .NET 8 projects with project references
    - Create `src/backend/UtilityService/` directory
    - Create `src/backend/UtilityService/UtilityService.Domain` (class library, net8.0, zero project references)
    - Create `src/backend/UtilityService/UtilityService.Application` (class library, net8.0, references Domain)
    - Create `src/backend/UtilityService/UtilityService.Infrastructure` (class library, net8.0, references Domain + Application)
    - Create `src/backend/UtilityService/UtilityService.Api` (web project, net8.0, references Application + Infrastructure)
    - Create `src/backend/UtilityService/UtilityService.Tests` (xUnit test project, net8.0, references Domain + Application + Infrastructure)
    - Add all projects to `Nexus-2.0.sln`
    - _Requirements: REQ-086 (Requirement 17)_

  - [ ] 1.2 Add NuGet package references to each project
    - Domain: no external packages
    - Application: `FluentValidation` only
    - Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Microsoft.Extensions.Http.Polly`, `Polly`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Redis`, `DotNetEnv`
    - Api: `FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore`
    - Tests: `FsCheck.Xunit`, `Moq`, `Microsoft.EntityFrameworkCore.InMemory`
    - _Requirements: REQ-086 (Requirement 17)_

- [ ] 2. Domain layer — Entities, exceptions, helpers, interfaces
  - [ ] 2.1 Create domain entities (9 entities)
    - Implement `AuditLog` entity with `AuditLogId`, `OrganizationId`, `ServiceName`, `Action`, `EntityType`, `EntityId`, `UserId`, `OldValue`, `NewValue`, `IpAddress`, `CorrelationId`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `ArchivedAuditLog` entity with same fields as AuditLog plus `ArchivedAuditLogId` (PK) and `ArchivedDate`
    - Implement `ErrorLog` entity with `ErrorLogId`, `OrganizationId`, `ServiceName`, `ErrorCode`, `Message`, `StackTrace`, `CorrelationId`, `Severity`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `ErrorCodeEntry` entity with `ErrorCodeEntryId`, `Code`, `Value`, `HttpStatusCode`, `ResponseCode`, `Description`, `ServiceName`, `DateCreated`, `DateUpdated`
    - Implement `NotificationLog` entity with `NotificationLogId`, `OrganizationId`, `UserId`, `NotificationType`, `Channel`, `Recipient`, `Subject`, `Status`, `RetryCount`, `LastRetryDate`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `DepartmentType` entity with `DepartmentTypeId`, `TypeName`, `TypeCode`, `FlgStatus`
    - Implement `PriorityLevel` entity with `PriorityLevelId`, `Name`, `SortOrder`, `Color`, `FlgStatus`
    - Implement `TaskTypeRef` entity with `TaskTypeRefId`, `TypeName`, `DefaultDepartmentCode`, `FlgStatus`
    - Implement `WorkflowState` entity with `WorkflowStateId`, `EntityType`, `StateName`, `SortOrder`, `FlgStatus`
    - Create `IOrganizationEntity` marker interface in `Common/`
    - _Requirements: REQ-083 (Requirement 14), REQ-084 (Requirement 15)_

  - [ ] 2.2 Create `ErrorCodes` static class and `DomainException` base class
    - Implement `ErrorCodes` with all constants: VALIDATION_ERROR (1000), AUDIT_LOG_IMMUTABLE (6001), ERROR_CODE_DUPLICATE (6002), ERROR_CODE_NOT_FOUND (6003), NOTIFICATION_DISPATCH_FAILED (6004), REFERENCE_DATA_NOT_FOUND (6005), ORGANIZATION_MISMATCH (6006), TEMPLATE_NOT_FOUND (6007), NOT_FOUND (6008), CONFLICT (6009), SERVICE_UNAVAILABLE (6010), INVALID_NOTIFICATION_TYPE (6011), INVALID_CHANNEL (6012), RETENTION_PERIOD_INVALID (6013), REFERENCE_DATA_DUPLICATE (6014), OUTBOX_PROCESSING_FAILED (6015), INTERNAL_ERROR (9999)
    - Implement `DomainException` base class with `ErrorValue`, `ErrorCode`, `StatusCode`, `CorrelationId`
    - _Requirements: REQ-071–REQ-085 (Requirement 16)_

  - [ ] 2.3 Create all concrete domain exception classes (15 exceptions)
    - `AuditLogImmutableException` (6001, 405), `ErrorCodeDuplicateException` (6002, 409), `ErrorCodeNotFoundException` (6003, 404), `NotificationDispatchFailedException` (6004, 500), `ReferenceDataNotFoundException` (6005, 404), `OrganizationMismatchException` (6006, 403), `TemplateNotFoundException` (6007, 404), `NotFoundException` (6008, 404), `ConflictException` (6009, 409), `ServiceUnavailableException` (6010, 503), `InvalidNotificationTypeException` (6011, 400), `InvalidChannelException` (6012, 400), `RetentionPeriodInvalidException` (6013, 400), `ReferenceDataDuplicateException` (6014, 409), `OutboxProcessingFailedException` (6015, 500)
    - _Requirements: REQ-071–REQ-085 (Requirement 16)_

  - [ ] 2.4 Create helper constants and enums
    - `NotificationTypes` with 8 types: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching
    - `NotificationChannels` with Email, Push, InApp
    - `NotificationStatuses` with Pending, Sent, Failed, PermanentlyFailed
    - `SeverityLevels` with Info, Warning, Error, Critical
    - `EntityStatuses` with Active = "A", Suspended = "S", Deactivated = "D"
    - _Requirements: REQ-074 (Requirement 4), REQ-075 (Requirement 5), REQ-072 (Requirement 2)_

  - [ ] 2.5 Create domain service interfaces
    - `IAuditLogService` (CreateAsync, QueryAsync, QueryArchiveAsync)
    - `IErrorLogService` (CreateAsync, QueryAsync)
    - `IErrorCodeService` (CreateAsync, ListAsync, UpdateAsync, DeleteAsync)
    - `INotificationService` (DispatchAsync, GetUserHistoryAsync, RetryFailedAsync)
    - `INotificationDispatcher` (SendEmailAsync, SendPushAsync, SendInAppAsync)
    - `IReferenceDataService` (GetDepartmentTypesAsync, GetPriorityLevelsAsync, GetTaskTypesAsync, GetWorkflowStatesAsync, CreateDepartmentTypeAsync, CreatePriorityLevelAsync)
    - `IPiiRedactionService` (Redact)
    - `ITemplateRenderer` (Render)
    - `IOutboxMessageRouter` (RouteAsync)
    - `IErrorCodeResolverService` (ResolveAsync)
    - _Requirements: REQ-071 (Requirement 1), REQ-072 (Requirement 2), REQ-073 (Requirement 3), REQ-074 (Requirement 4), REQ-076 (Requirement 6), REQ-077 (Requirement 7), REQ-080 (Requirement 9)_

  - [ ] 2.6 Create repository interfaces (9 repositories)
    - `IAuditLogRepository` (AddAsync, QueryAsync)
    - `IArchivedAuditLogRepository` (AddRangeAsync, QueryAsync)
    - `IErrorLogRepository` (AddAsync, QueryAsync)
    - `IErrorCodeEntryRepository` (GetByCodeAsync, AddAsync, UpdateAsync, RemoveAsync, ListAsync)
    - `INotificationLogRepository` (AddAsync, UpdateAsync, QueryByUserAsync, GetFailedForRetryAsync)
    - `IDepartmentTypeRepository` (GetByNameAsync, GetByCodeAsync, AddAsync, ListAsync, AddRangeAsync, ExistsAsync)
    - `IPriorityLevelRepository` (GetByNameAsync, AddAsync, ListAsync, AddRangeAsync, ExistsAsync)
    - `ITaskTypeRefRepository` (ListAsync, AddRangeAsync, ExistsAsync)
    - `IWorkflowStateRepository` (ListAsync, AddRangeAsync, ExistsAsync)
    - _Requirements: REQ-083 (Requirement 14)_

- [ ] 3. Application layer — DTOs, validators, OutboxMessage
  - [ ] 3.1 Create `ApiResponse<T>` envelope, `ErrorDetail`, and `PaginatedResponse<T>` classes
    - `ApiResponse<T>` with `ResponseCode`, `ResponseDescription`, `Success`, `Data`, `ErrorCode`, `ErrorValue`, `Message`, `CorrelationId`, `Errors`
    - `ErrorDetail` with `Field`, `Message`
    - `PaginatedResponse<T>` with `TotalCount`, `Page`, `PageSize`, `TotalPages`, `Data`
    - _Requirements: REQ-088 (Requirement 19), REQ-096 (Requirement 24)_

  - [ ] 3.2 Create request DTOs
    - `CreateAuditLogRequest` (OrganizationId, ServiceName, Action, EntityType, EntityId, UserId, OldValue, NewValue, IpAddress, CorrelationId)
    - `AuditLogFilterRequest` (ServiceName, Action, EntityType, UserId, DateFrom, DateTo)
    - `CreateErrorLogRequest` (OrganizationId, ServiceName, ErrorCode, Message, StackTrace, CorrelationId, Severity)
    - `ErrorLogFilterRequest` (ServiceName, ErrorCode, Severity, DateFrom, DateTo)
    - `CreateErrorCodeRequest` (Code, Value, HttpStatusCode, ResponseCode, Description, ServiceName)
    - `UpdateErrorCodeRequest` (Value, HttpStatusCode, ResponseCode, Description, ServiceName — all nullable)
    - `DispatchNotificationRequest` (OrganizationId, UserId, NotificationType, Channels, Recipient, Subject, TemplateVariables)
    - `NotificationLogFilterRequest` (NotificationType, Channel, Status, DateFrom, DateTo)
    - `CreateDepartmentTypeRequest` (TypeName, TypeCode)
    - `CreatePriorityLevelRequest` (Name, SortOrder, Color)
    - _Requirements: REQ-071 (Requirement 1), REQ-072 (Requirement 2), REQ-073 (Requirement 3), REQ-074 (Requirement 4), REQ-077 (Requirement 7)_

  - [ ] 3.3 Create response DTOs
    - `AuditLogResponse` (AuditLogId, OrganizationId, ServiceName, Action, EntityType, EntityId, UserId, OldValue, NewValue, IpAddress, CorrelationId, DateCreated, ArchivedDate)
    - `ErrorLogResponse` (ErrorLogId, OrganizationId, ServiceName, ErrorCode, Message, StackTrace, CorrelationId, Severity, DateCreated)
    - `ErrorCodeResponse` (ErrorCodeEntryId, Code, Value, HttpStatusCode, ResponseCode, Description, ServiceName, DateCreated, DateUpdated)
    - `NotificationLogResponse` (NotificationLogId, OrganizationId, UserId, NotificationType, Channel, Recipient, Subject, Status, RetryCount, LastRetryDate, DateCreated)
    - `DepartmentTypeResponse`, `PriorityLevelResponse`, `TaskTypeRefResponse`, `WorkflowStateResponse`
    - _Requirements: REQ-071 (Requirement 1), REQ-072 (Requirement 2), REQ-073 (Requirement 3), REQ-074 (Requirement 4), REQ-077 (Requirement 7)_

  - [ ] 3.4 Create `OutboxMessage` DTO and `ErrorCodeResolverResponse` contract
    - `OutboxMessage` with `Id`, `Type` (audit/notification), `Payload` (JSON), `Timestamp`, `RetryCount`
    - `ErrorCodeResolverResponse` with `ResponseCode`, `Description`
    - _Requirements: REQ-080 (Requirement 9), REQ-093_

  - [ ] 3.5 Create FluentValidation validators for all request DTOs (7 validators)
    - `CreateAuditLogRequestValidator` (OrganizationId, ServiceName, Action, EntityType, EntityId, UserId, CorrelationId all required)
    - `CreateErrorLogRequestValidator` (OrganizationId, ServiceName, ErrorCode, Message, CorrelationId required; Severity must be Info/Warning/Error/Critical)
    - `CreateErrorCodeRequestValidator` (Code required, Value > 0, HttpStatusCode 100–599, ResponseCode required max 10, Description required, ServiceName required)
    - `UpdateErrorCodeRequestValidator` (HttpStatusCode 100–599 when present, ResponseCode max 10 when present)
    - `DispatchNotificationRequestValidator` (OrganizationId, UserId, NotificationType must be one of 8 types, Channels must be comma-separated Email/Push/InApp, Recipient required)
    - `CreateDepartmentTypeRequestValidator` (TypeName required, TypeCode required uppercase alphanumeric max 10)
    - `CreatePriorityLevelRequestValidator` (Name required, SortOrder > 0, Color valid hex #RRGGBB)
    - _Requirements: REQ-090 (Requirement 20)_

- [ ] 4. Checkpoint — Verify Domain and Application layers compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Infrastructure layer — Data access (EF Core + PostgreSQL)
  - [x] 5.1 Create `UtilityDbContext` with entity configurations
    - Configure organization-scoped global query filters for `AuditLog`, `ErrorLog`, `NotificationLog` (filter by `OrganizationId`)
    - Configure soft-delete global query filters for `DepartmentType`, `PriorityLevel`, `TaskTypeRef`, `WorkflowState` (filter `FlgStatus != 'D'`)
    - Configure `AuditLog` (PK, indexes on OrganizationId, ServiceName, Action, EntityType, DateCreated)
    - Configure `ArchivedAuditLog` (PK, indexes on OrganizationId, DateCreated, ArchivedDate required)
    - Configure `ErrorLog` (PK, index on OrganizationId)
    - Configure `ErrorCodeEntry` (PK, unique index on Code, ResponseCode max 10)
    - Configure `NotificationLog` (PK, indexes on OrganizationId, UserId)
    - Configure reference data entities (PKs, required fields)
    - _Requirements: REQ-083 (Requirement 14), REQ-087 (Requirement 18), REQ-097 (Requirement 25)_

  - [x] 5.2 Create `DatabaseMigrationHelper` for auto-migration on startup
    - Apply pending EF Core migrations automatically
    - Use `EnsureCreated()` for InMemory test environments
    - _Requirements: REQ-094 (Requirement 22)_

  - [x] 5.3 Create `SeedDataHelper` for reference table seed data
    - Seed department types: Engineering (ENG), QA (QA), DevOps (DEVOPS), Product (PROD), Design (DESIGN)
    - Seed priority levels: Critical (1, #DC2626), High (2, #EA580C), Medium (3, #CA8A04), Low (4, #16A34A)
    - Seed task types: Development (ENG), Testing (QA), DevOps (DEVOPS), Design (DESIGN), Documentation (PROD), Bug (ENG)
    - Seed story workflow states: Backlog (1), Ready (2), InProgress (3), InReview (4), QA (5), Done (6), Closed (7)
    - Seed task workflow states: ToDo (1), InProgress (2), InReview (3), Done (4)
    - Skip seeding if data already exists (idempotent)
    - _Requirements: REQ-084 (Requirement 15)_

  - [x] 5.4 Implement all 9 repository classes
    - `AuditLogRepository` — AddAsync, QueryAsync with org-scoped pagination and filtering by ServiceName, Action, EntityType, UserId, date range
    - `ArchivedAuditLogRepository` — AddRangeAsync, QueryAsync with org-scoped pagination and filtering
    - `ErrorLogRepository` — AddAsync, QueryAsync with org-scoped pagination and filtering by ServiceName, ErrorCode, Severity, date range
    - `ErrorCodeEntryRepository` — GetByCodeAsync, AddAsync, UpdateAsync, RemoveAsync, ListAsync
    - `NotificationLogRepository` — AddAsync, UpdateAsync, QueryByUserAsync with pagination and filtering, GetFailedForRetryAsync (Status=Failed, RetryCount < max)
    - `DepartmentTypeRepository` — GetByNameAsync, GetByCodeAsync, AddAsync, ListAsync, AddRangeAsync, ExistsAsync
    - `PriorityLevelRepository` — GetByNameAsync, AddAsync, ListAsync, AddRangeAsync, ExistsAsync
    - `TaskTypeRefRepository` — ListAsync, AddRangeAsync, ExistsAsync
    - `WorkflowStateRepository` — ListAsync, AddRangeAsync, ExistsAsync
    - _Requirements: REQ-083 (Requirement 14), REQ-071 (Requirement 1), REQ-072 (Requirement 2), REQ-073 (Requirement 3), REQ-074 (Requirement 4), REQ-077 (Requirement 7)_

- [x] 6. Infrastructure layer — Configuration
  - [x] 6.1 Create `AppSettings` configuration class
    - `AppSettings.FromEnvironment()` loading from env vars via DotNetEnv
    - All configurable values: `DATABASE_URL`, `REDIS_URL`, `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `ALLOWED_ORIGINS`, `OUTBOX_POLL_INTERVAL_SECONDS` (default 30), `RETENTION_PERIOD_DAYS` (default 90), `RETENTION_SCHEDULE_HOUR` (default 2), `NOTIFICATION_RETRY_MAX` (default 3)
    - Throw `InvalidOperationException` for missing required variables
    - _Requirements: REQ-101 (Requirement 27)_

  - [x] 6.2 Create `.env.example` with all required environment variables
    - Document all env vars with sensible defaults
    - _Requirements: REQ-101 (Requirement 27)_

- [x] 7. Infrastructure layer — Services
  - [x] 7.1 Implement `AuditLogService`
    - `CreateAsync` — create immutable audit log entry from request DTO
    - `QueryAsync` — paginated, filtered query scoped to organization
    - `QueryArchiveAsync` — paginated, filtered query on archived audit logs scoped to organization
    - Reject any update/delete attempts with `AuditLogImmutableException` (6001, 405)
    - _Requirements: REQ-071 (Requirement 1)_

  - [x] 7.2 Implement `ErrorLogService`
    - `CreateAsync` — create error log entry with PII redaction applied to `Message` and `StackTrace` via `IPiiRedactionService` before persistence
    - `QueryAsync` — paginated, filtered query scoped to organization
    - _Requirements: REQ-072 (Requirement 2)_

  - [x] 7.3 Implement `ErrorCodeService`
    - `CreateAsync` — create error code entry, check for duplicate Code → throw `ErrorCodeDuplicateException` (6002), cache in Redis hash `error_codes_registry` (24h TTL)
    - `ListAsync` — return all error codes, serve from Redis cache on hit
    - `UpdateAsync` — update entry, throw `ErrorCodeNotFoundException` (6003) if not found, invalidate Redis cache
    - `DeleteAsync` — remove entry, throw `ErrorCodeNotFoundException` (6003) if not found, invalidate Redis cache
    - _Requirements: REQ-073 (Requirement 3), REQ-082 (Requirement 13)_

  - [x] 7.4 Implement `NotificationService`
    - `DispatchAsync` — validate notification type (8 types) and channels (Email/Push/InApp), check recipient preferences from Redis cache `notif_pref:{userId}:{typeId}` (5-min TTL), render template via `ITemplateRenderer`, dispatch via `INotificationDispatcher` per enabled channel, create `NotificationLog` per channel
    - `GetUserHistoryAsync` — paginated, filtered query scoped to user and organization
    - `RetryFailedAsync` — query failed notifications with RetryCount < max, retry with exponential backoff (2^RetryCount minutes), update status to Sent on success or PermanentlyFailed when RetryCount reaches max
    - _Requirements: REQ-074 (Requirement 4), REQ-075 (Requirement 5), REQ-082 (Requirement 13)_

  - [x] 7.5 Implement `ReferenceDataService`
    - `GetDepartmentTypesAsync`, `GetPriorityLevelsAsync`, `GetTaskTypesAsync`, `GetWorkflowStatesAsync` — serve from Redis cache (24h TTL) at `ref:department_types`, `ref:priority_levels`, `ref:task_types`, `ref:workflow_states`; query DB on cache miss and populate cache
    - `CreateDepartmentTypeAsync` — check for duplicate TypeName/TypeCode → throw `ReferenceDataDuplicateException` (6014), invalidate `ref:department_types` cache
    - `CreatePriorityLevelAsync` — check for duplicate Name → throw `ReferenceDataDuplicateException` (6014), invalidate `ref:priority_levels` cache
    - _Requirements: REQ-077 (Requirement 7), REQ-082 (Requirement 13)_

  - [x] 7.6 Implement `PiiRedactionService`
    - `Redact` — detect and replace email addresses (regex `[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}`), IPv4 addresses, IPv6 addresses with `[REDACTED]`
    - Return input unchanged if no PII patterns found
    - _Requirements: REQ-072 (Requirement 2)_

  - [x] 7.7 Implement `TemplateRenderer`
    - `Render` — load template file from `Templates/Email/{type}.html` or `Templates/Push/{type}.txt` based on channel, replace `{{key}}` placeholders with template variable values
    - Throw `TemplateNotFoundException` (6007) if template file not found
    - Email channel → HTML output; Push/InApp channels → plain-text output
    - _Requirements: REQ-076 (Requirement 6)_

  - [x] 7.8 Implement `OutboxMessageRouter`
    - `RouteAsync` — deserialize raw message to `OutboxMessage`, route by `Type` field: `"audit"` → extract payload and create AuditLog via `IAuditLogService`, `"notification"` → extract payload and dispatch via `INotificationService`
    - Move to dead-letter queue (`dlq:{service}`) for unknown types or deserialization failures, log with `OUTBOX_PROCESSING_FAILED` (6015)
    - _Requirements: REQ-080 (Requirement 9), REQ-085 (Requirement 9)_

  - [x] 7.9 Implement `ErrorCodeResolverService`
    - `ResolveAsync` — multi-tier resolution: (1) check Redis hash `error_codes_registry`, (2) query DB on miss, (3) fall back to static mapping
    - Return `(ResponseCode, ResponseDescription)` tuple
    - _Requirements: REQ-088 (Requirement 19)_

- [x] 8. Infrastructure layer — Background services (4 hosted services)
  - [x] 8.1 Implement `OutboxProcessorHostedService`
    - Poll Redis outbox queues (`outbox:security`, `outbox:profile`, `outbox:work`) on configurable interval (default 30s via `OUTBOX_POLL_INTERVAL_SECONDS`)
    - RPOP messages from each queue, route via `IOutboxMessageRouter`
    - On failure: increment RetryCount, re-queue; after 3 failures move to `dlq:{service}`
    - Use `IServiceScopeFactory` for scoped service resolution
    - _Requirements: REQ-080 (Requirement 9), REQ-085 (Requirement 10)_

  - [x] 8.2 Implement `RetentionArchivalHostedService`
    - Run daily at `RETENTION_SCHEDULE_HOUR` (default 2 AM UTC)
    - Query audit logs with `DateCreated` older than `RETENTION_PERIOD_DAYS` (default 90)
    - Process in batches: copy to `archived_audit_log` with `ArchivedDate = DateTime.UtcNow`, delete originals
    - Validate retention period > 0, throw `RetentionPeriodInvalidException` (6013) if invalid
    - _Requirements: REQ-078 (Requirement 8)_

  - [x] 8.3 Implement `NotificationRetryHostedService`
    - Run every 60 seconds
    - Query `NotificationLog` entries with `Status = 'Failed'` and `RetryCount < 3`
    - Retry dispatch with exponential backoff delay of `2^RetryCount` minutes
    - On success: update status to `Sent`; on final failure (RetryCount reaches 3): update status to `PermanentlyFailed`
    - _Requirements: REQ-080 (Requirement 10)_

  - [x] 8.4 Implement `DueDateNotificationHostedService`
    - Run every 6 hours
    - Scan for stories/tasks with due dates within 24 hours (via WorkService or Redis)
    - Publish `DueDateApproaching` notification events
    - Avoid duplicate notifications for the same entity within a 24-hour window
    - _Requirements: REQ-080 (Requirement 10)_

- [x] 9. Infrastructure layer — DI registration
  - [x] 9.1 Create `DependencyInjection` extension class for Infrastructure service registration
    - Register `UtilityDbContext` with PostgreSQL
    - Register Redis `IConnectionMultiplexer`
    - Register all 9 repository implementations
    - Register all service implementations: AuditLogService, ErrorLogService, ErrorCodeService, NotificationService, NotificationDispatcher, ReferenceDataService, PiiRedactionService, TemplateRenderer, OutboxMessageRouter, ErrorCodeResolverService
    - Register all 4 background hosted services: OutboxProcessorHostedService, RetentionArchivalHostedService, NotificationRetryHostedService, DueDateNotificationHostedService
    - Register `CorrelationIdDelegatingHandler` for outgoing HTTP calls
    - _Requirements: REQ-086 (Requirement 17)_

- [x] 10. Checkpoint — Verify Infrastructure layer compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Api layer — Middleware, controllers, and Program.cs
  - [x] 11.1 Implement middleware classes
    - `CorrelationIdMiddleware` — generate or propagate `X-Correlation-Id` header, store in `HttpContext.Items["CorrelationId"]`, include in response headers
    - `GlobalExceptionHandlerMiddleware` — catch `DomainException` → resolve via `IErrorCodeResolverService` → return `ApiResponse<object>` with `application/problem+json`; catch unhandled → return 500 `INTERNAL_ERROR` with no stack trace leakage
    - `JwtClaimsMiddleware` — extract JWT claims (userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti) and store in `HttpContext.Items`
    - `TokenBlacklistMiddleware` — check `blacklist:{jti}` in Redis, return 401 `TOKEN_REVOKED` if blacklisted
    - `OrganizationScopeMiddleware` — extract `organizationId` from JWT claims, validate against route/query params, skip for service-auth tokens, return 403 `ORGANIZATION_MISMATCH` on cross-org access
    - `CorrelationIdDelegatingHandler` — propagate `X-Correlation-Id` on all outgoing HTTP calls
    - _Requirements: REQ-092 (Requirement 21), REQ-088 (Requirement 19), REQ-087 (Requirement 18), Requirement 30_

  - [x] 11.2 Create `ServiceAuthAttribute` and `OrgAdminAttribute`
    - `ServiceAuthAttribute` — validate service JWT on endpoints marked with `[ServiceAuth]`, return 403 `SERVICE_NOT_AUTHORIZED` if invalid
    - `OrgAdminAttribute` — restrict endpoint to OrgAdmin role
    - _Requirements: REQ-081 (Requirement 12)_

  - [x] 11.3 Create `MiddlewarePipelineExtensions`
    - Register middleware in correct order: CORS → CorrelationId → GlobalExceptionHandler → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → OrganizationScope → Controllers
    - Note: NO FirstTimeUserMiddleware, NO RateLimiterMiddleware in UtilityService pipeline
    - _Requirements: Requirement 30_

  - [x] 11.4 Implement `AuditLogController`
    - `POST /api/v1/audit-logs` — `[ServiceAuth]`, accepts `CreateAuditLogRequest`, returns 201 `AuditLogResponse`
    - `GET /api/v1/audit-logs` — Bearer auth, paginated + filtered audit logs scoped to org
    - `GET /api/v1/audit-logs/archive` — Bearer auth, paginated + filtered archived audit logs scoped to org
    - Any PUT/DELETE → return 405 `AUDIT_LOG_IMMUTABLE`
    - _Requirements: REQ-071 (Requirement 1), REQ-081 (Requirement 12)_

  - [x] 11.5 Implement `ErrorLogController`
    - `POST /api/v1/error-logs` — `[ServiceAuth]`, accepts `CreateErrorLogRequest`, returns 201 `ErrorLogResponse`
    - `GET /api/v1/error-logs` — `[OrgAdmin]`, paginated + filtered error logs scoped to org
    - _Requirements: REQ-072 (Requirement 2), REQ-081 (Requirement 12)_

  - [x] 11.6 Implement `ErrorCodeController`
    - `POST /api/v1/error-codes` — `[OrgAdmin]`, accepts `CreateErrorCodeRequest`, returns 201 `ErrorCodeResponse`
    - `GET /api/v1/error-codes` — Bearer auth, list all error codes
    - `PUT /api/v1/error-codes/{code}` — `[OrgAdmin]`, accepts `UpdateErrorCodeRequest`, returns `ErrorCodeResponse`
    - `DELETE /api/v1/error-codes/{code}` — `[OrgAdmin]`, returns 204
    - _Requirements: REQ-073 (Requirement 3), REQ-081 (Requirement 12)_

  - [x] 11.7 Implement `NotificationController`
    - `POST /api/v1/notifications/dispatch` — `[ServiceAuth]`, accepts `DispatchNotificationRequest`, returns 200
    - `GET /api/v1/notification-logs` — Bearer auth, paginated user notification history
    - _Requirements: REQ-074 (Requirement 4), REQ-081 (Requirement 12)_

  - [x] 11.8 Implement `ReferenceDataController`
    - `GET /api/v1/reference/department-types` — no auth, list department types
    - `GET /api/v1/reference/priority-levels` — no auth, list priority levels
    - `GET /api/v1/reference/task-types` — no auth, list task types
    - `GET /api/v1/reference/workflow-states` — no auth, list workflow states
    - `POST /api/v1/reference/department-types` — `[OrgAdmin]`, accepts `CreateDepartmentTypeRequest`, returns 201
    - `POST /api/v1/reference/priority-levels` — `[OrgAdmin]`, accepts `CreatePriorityLevelRequest`, returns 201
    - _Requirements: REQ-077 (Requirement 7), REQ-081 (Requirement 12)_

  - [x] 11.9 Create `Program.cs` with full DI registration and middleware pipeline
    - Load `.env` via DotNetEnv, build `AppSettings`
    - Register Infrastructure services via `DependencyInjection` extension
    - Register FluentValidation validators (auto-discovery), suppress `ModelStateInvalidFilter`
    - Register JWT Bearer authentication
    - Register CORS with `AllowedOrigins`
    - Register health checks (PostgreSQL + Redis)
    - Register Swagger (Development mode only)
    - Apply `DatabaseMigrationHelper` and `SeedDataHelper` on startup
    - Build middleware pipeline via `MiddlewarePipelineExtensions`
    - Map controllers, health check endpoints (`/health`, `/ready`)
    - _Requirements: REQ-086 (Requirement 17), REQ-090 (Requirement 20), REQ-095 (Requirement 23), REQ-101 (Requirement 27), REQ-102 (Requirement 28), REQ-103 (Requirement 29)_

  - [x] 11.10 Create Api layer extensions
    - `ControllerServiceExtensions` — register controllers with `ApiResponse<T>` envelope conventions
    - `SwaggerServiceExtensions` — configure Swagger with JWT Bearer auth support (Development only)
    - `HealthCheckExtensions` — register PostgreSQL and Redis health checks, map `/health` and `/ready` endpoints
    - _Requirements: REQ-095 (Requirement 23), REQ-103 (Requirement 29)_

  - [x] 11.11 Create `Dockerfile` and `.env` / `.env.example`
    - Multi-stage Dockerfile for UtilityService.Api (port 5200)
    - `.env.example` documenting all environment variables
    - _Requirements: REQ-101 (Requirement 27)_

  - [x] 11.12 Configure structured logging conventions
    - DomainException logs: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`
    - Unhandled exception logs: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`
    - Outbox processing failure logs: `CorrelationId`, `OutboxQueue`, `MessageId`, `RetryCount`, `ErrorMessage`
    - Notification dispatch failure logs: `CorrelationId`, `NotificationType`, `Channel`, `Recipient`, `ErrorMessage`
    - _Requirements: REQ-098 (Requirement 26)_

- [x] 12. Notification templates (16 files)
  - [x] 12.1 Create 8 email HTML templates in `Infrastructure/Templates/Email/`
    - `story-assigned.html` — variables: `StoryKey`, `StoryTitle`, `AssignerName`
    - `task-assigned.html` — variables: `StoryKey`, `TaskTitle`, `TaskType`, `AssignerName`
    - `sprint-started.html` — variables: `SprintName`, `StartDate`, `EndDate`, `StoryCount`
    - `sprint-ended.html` — variables: `SprintName`, `Velocity`, `CompletionRate`
    - `mentioned-in-comment.html` — variables: `MentionerName`, `StoryKey`, `CommentPreview`
    - `story-status-changed.html` — variables: `StoryKey`, `StoryTitle`, `OldStatus`, `NewStatus`
    - `task-status-changed.html` — variables: `StoryKey`, `TaskTitle`, `OldStatus`, `NewStatus`
    - `due-date-approaching.html` — variables: `EntityType`, `StoryKey`, `Title`, `DueDate`
    - _Requirements: REQ-076 (Requirement 6), REQ-075 (Requirement 5)_

  - [x] 12.2 Create 8 push/in-app plain-text templates in `Infrastructure/Templates/Push/`
    - `story-assigned.txt`, `task-assigned.txt`, `sprint-started.txt`, `sprint-ended.txt`, `mentioned-in-comment.txt`, `story-status-changed.txt`, `task-status-changed.txt`, `due-date-approaching.txt`
    - Each with the same template variables as the corresponding email template, plain-text format
    - _Requirements: REQ-076 (Requirement 6), REQ-075 (Requirement 5)_

- [x] 13. Checkpoint — Full build verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

- [ ] 14. Testing
  - [ ] 14.1 Write property tests for PII redaction
    - **Property 1: PII redaction replaces all PII patterns** — Generate random strings with embedded emails, IPv4, IPv6 addresses. Verify all are replaced with `[REDACTED]`.
    - **Property 2: PII redaction preserves non-PII content** — Generate random strings without PII patterns. Verify output equals input.
    - **Validates: Requirements 2.1, 2.6**

  - [ ] 14.2 Write property tests for audit log immutability
    - **Property 3: Audit log immutability** — Generate random audit logs, attempt update/delete, verify rejection with AUDIT_LOG_IMMUTABLE (6001).
    - **Validates: Requirements 1.3**

  - [ ] 14.3 Write property tests for error code uniqueness and CRUD
    - **Property 5: Error code uniqueness enforcement** — Generate random error codes, attempt duplicate creation, verify rejection with ERROR_CODE_DUPLICATE (6002).
    - **Property 6: Error code CRUD round-trip with cache invalidation** — Create, read, update, delete error codes and verify consistency and Redis cache invalidation.
    - **Property 7: Error code not-found handling** — Generate random non-existent codes, verify 404 with ERROR_CODE_NOT_FOUND (6003).
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**

  - [ ] 14.4 Write property tests for notification dispatch and preferences
    - **Property 8: Notification dispatch creates log entries per channel** — Generate dispatch requests with various channels, verify N log entries created.
    - **Property 9: Notification preference filtering** — Generate preferences with disabled channels, verify skipped channels.
    - **Property 10: Invalid notification type or channel rejection** — Generate invalid types/channels, verify rejection with INVALID_NOTIFICATION_TYPE (6011) or INVALID_CHANNEL (6012).
    - **Validates: Requirements 4.1, 4.2, 4.5, 4.7, 4.8**

  - [ ] 14.5 Write property tests for template rendering
    - **Property 11: Template rendering injects all variables** — Generate random template variables, verify all appear in rendered output. Email → HTML, Push/InApp → plain-text.
    - **Validates: Requirements 5.2, 6.1, 6.2, 6.3**

  - [ ] 14.6 Write property tests for reference data
    - **Property 12: Reference data queries return only active entries** — Generate reference data with mixed FlgStatus, verify only active returned.
    - **Property 13: Reference data creation with cache invalidation** — Create reference data, verify in list and cache invalidated.
    - **Property 14: Reference data duplicate rejection** — Generate duplicate names/codes, verify rejection with REFERENCE_DATA_DUPLICATE (6014).
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.6, 7.7, 7.8, 25.2**

  - [ ] 14.7 Write property tests for retention archival
    - **Property 15: Retention archival moves only expired logs** — Generate audit logs with various dates, run archival, verify only expired moved to archive with ArchivedDate set.
    - **Validates: Requirements 8.1, 8.2**

  - [ ] 14.8 Write property tests for outbox processing
    - **Property 16: Outbox message routing by type** — Generate messages with audit/notification/unknown types, verify correct routing or DLQ.
    - **Property 17: Outbox retry and dead-letter queue escalation** — Generate failing messages, verify retry count increment and DLQ after 3 failures.
    - **Validates: Requirements 9.2, 9.3, 9.5, 9.6, 9.7**

  - [ ] 14.9 Write property tests for notification retry
    - **Property 18: Notification retry with exponential backoff** — Generate failed notifications, verify retry timing (2^RetryCount minutes), status transitions (Sent on success, PermanentlyFailed at max).
    - **Validates: Requirements 10.2, 10.3, 10.4**

  - [ ] 14.10 Write property tests for seed data, org isolation, soft delete, pagination, validation
    - **Property 19: Seed data idempotence** — Run seed multiple times, verify no duplicates.
    - **Property 20: Organization isolation via global query filters** — Generate data across orgs, verify query isolation.
    - **Property 21: Soft delete retains records physically** — Soft-delete reference data, verify physical existence but query exclusion.
    - **Property 22: Pagination metadata consistency** — Generate various page/pageSize values, verify TotalPages = ceil(TotalCount/PageSize), PageSize capped at 100.
    - **Property 23: Validation error format** — Generate invalid requests, verify 422 response with VALIDATION_ERROR (1000) and per-field errors.
    - **Property 24: Severity validation** — Generate invalid severity strings, verify rejection.
    - **Property 25: DueDateNotification deduplication** — Generate entities with approaching due dates, run service multiple times, verify no duplicate notifications.
    - **Validates: Requirements 15.6, 18.1, 18.5, 25.1, 25.2, 24.1, 24.2, 24.3, 12.4, 20.2, 2.5, 10.5**

  - [ ] 14.11 Write unit tests for middleware pipeline and validators
    - Test middleware registration order matches Requirement 30 specification
    - Test GlobalExceptionHandlerMiddleware returns correct ApiResponse for DomainException and unhandled exceptions
    - Test TokenBlacklistMiddleware rejects blacklisted tokens
    - Test each FluentValidation validator with valid and invalid inputs
    - Test seed data exact values (department types, priority levels, task types, workflow states)
    - Test health check endpoint responses
    - **Validates: Requirement 30, REQ-088 (Requirement 19), REQ-090 (Requirement 20), REQ-084 (Requirement 15), REQ-095 (Requirement 23)**

- [x] 15. Final checkpoint — Full integration verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

## Notes

- All UtilityService projects live under `src/backend/UtilityService/` in the monorepo
- Tests are co-located at `src/backend/UtilityService/UtilityService.Tests/`
- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The implementation language is C# (.NET 8) as specified in the design document
- Checkpoints ensure incremental validation at layer boundaries
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
- UtilityService middleware pipeline does NOT include `FirstTimeUserMiddleware` or `RateLimiterMiddleware`
- UtilityService includes 4 background hosted services: OutboxProcessor, RetentionArchival, NotificationRetry, DueDateNotification
- UtilityService includes 16 notification template files (8 email HTML + 8 push/in-app text)
- Organization-scoped entities (AuditLog, ErrorLog, NotificationLog) have global query filters by OrganizationId
- Reference data entities have soft-delete query filters (FlgStatus != 'D')
- ErrorCodeEntry and ArchivedAuditLog are NOT organization-scoped
- Seed data is applied on startup via SeedDataHelper for department types, priority levels, task types, and workflow states
