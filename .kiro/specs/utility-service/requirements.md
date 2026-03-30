# Requirements Document — UtilityService

## Introduction

This document defines the complete requirements for the Nexus-2.0 UtilityService — the cross-cutting operational microservice of the Enterprise Agile Platform. UtilityService runs on port 5200 with database `nexus_utility` and follows Clean Architecture (.NET 8) with Domain / Application / Infrastructure / Api layers.

UtilityService provides operational capabilities consumed by all other services: immutable audit logging, error logging with PII redaction, notification dispatch (Email, Push, InApp), error code registry, reference data management, retention archival, and outbox processing. It has no direct user-facing features — it is primarily an internal/service-to-service platform.

UtilityService polls Redis outbox queues from SecurityService (`outbox:security`), ProfileService (`outbox:profile`), and WorkService (`outbox:work`) via `OutboxProcessorHostedService`, routing messages to audit log creation or notification dispatch handlers.

All requirements are derived from the platform documentation:
- `docs/nexus-2.0-backend-requirements.md` (REQ-071 – REQ-085, REQ-086 – REQ-108)
- `docs/nexus-2.0-backend-specification.md` (UtilityService specification, sections 7.1–7.10)
- `docs/platform-specification.md` (predecessor WEP UtilityService patterns)
- `.kiro/specs/security-service/requirements.md` (SecurityService integration — outbox:security)
- `.kiro/specs/profile-service/requirements.md` (ProfileService integration — outbox:profile)
- `.kiro/specs/work-service/requirements.md` (WorkService integration — outbox:work)

## Glossary

- **UtilityService**: Microservice (port 5200, database `nexus_utility`) responsible for audit logging, error logging with PII redaction, notification dispatch, error code registry, reference data management, retention archival, and outbox processing.
- **SecurityService**: Microservice (port 5001) responsible for authentication, JWT issuance, and RBAC. Publishes audit events and notifications to `outbox:security`.
- **ProfileService**: Microservice (port 5002) responsible for organizations, departments, and team members. Publishes audit events and notifications to `outbox:profile`.
- **WorkService**: Microservice (port 5003) responsible for stories, tasks, sprints, and boards. Publishes audit events and notifications to `outbox:work`.
- **AuditLog**: Immutable record of a significant platform event, scoped to an organization. Contains `ServiceName`, `Action`, `EntityType`, `EntityId`, `UserId`, `OldValue` (JSON), `NewValue` (JSON), `IpAddress`, `CorrelationId`, and `DateCreated`.
- **ArchivedAuditLog**: An audit log record moved to the archive table by the retention archival process. Contains the same fields as AuditLog plus `ArchivedDate`.
- **ErrorLog**: Record of an error event with PII-redacted message and stack trace. Contains `ServiceName`, `ErrorCode`, `Message`, `StackTrace`, `CorrelationId`, `Severity`, and `DateCreated`.
- **ErrorCodeEntry**: A centralized registry entry for an error code used across all services. Contains `Code`, `Value`, `HttpStatusCode`, `ResponseCode`, `Description`, and `ServiceName`.
- **NotificationLog**: Record of a dispatched notification. Contains `UserId`, `NotificationType`, `Channel`, `Recipient`, `Subject`, `Status`, `RetryCount`, `LastRetryDate`, and `DateCreated`.
- **DepartmentType**: Reference data entity representing a department classification with `TypeName` and `TypeCode`.
- **PriorityLevel**: Reference data entity representing a priority classification with `Name`, `SortOrder`, and `Color`.
- **TaskTypeRef**: Reference data entity representing a task type with `TypeName` and `DefaultDepartmentCode` for department routing.
- **WorkflowState**: Reference data entity representing a workflow state for stories or tasks, with `EntityType`, `StateName`, and `SortOrder`.
- **NotificationType**: A predefined notification category. Eight types: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching.
- **Notification_Channel**: A delivery mechanism for notifications — Email, Push, or InApp.
- **Outbox**: Redis-based async messaging pattern. Each upstream service publishes events to `outbox:{service}`. UtilityService polls all queues and processes messages.
- **OutboxProcessorHostedService**: Background service that polls Redis outbox queues (`outbox:security`, `outbox:profile`, `outbox:work`) on a configurable interval (default 30s) and routes messages to audit log or notification dispatch handlers.
- **RetentionArchivalHostedService**: Background service that runs daily at a configured hour, moving audit logs older than the retention period to the `archived_audit_log` table.
- **NotificationRetryHostedService**: Background service that runs every 60 seconds, retrying failed notification dispatches with exponential backoff (2^retryCount minutes), max 3 retries.
- **DueDateNotificationHostedService**: Background service that runs every 6 hours, scanning for stories/tasks with due dates within 24 hours and publishing `DueDateApproaching` notifications.
- **Dead_Letter_Queue**: Redis queue (`dlq:{service}`) where outbox messages are moved after 3 failed processing attempts.
- **PII_Redaction**: Process of detecting and replacing personally identifiable information (emails, names, IP addresses) with `[REDACTED]` before persistence.
- **ErrorCodeResolverService**: Per-service component that resolves error codes via a multi-tier cache: (1) in-memory `ConcurrentDictionary`, (2) Redis hash `error_codes_registry` (24h TTL), (3) HTTP call to UtilityService `GET /api/v1/error-codes`, (4) local static fallback map.
- **Organization**: Top-level tenant entity. All UtilityService data (audit logs, error logs, notification logs) is scoped to an organization via `OrganizationId`.
- **FlgStatus**: Soft-delete lifecycle field — `A` (Active), `S` (Suspended), `D` (Deactivated).
- **ApiResponse**: Standardized JSON envelope `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, `Errors` fields.
- **DomainException**: Base exception class for business rule violations, containing `ErrorValue`, `ErrorCode`, `StatusCode`, and `CorrelationId`.
- **CorrelationId**: End-to-end trace identifier (`X-Correlation-Id` header) propagated across all service calls and included in all API responses.
- **IOrganizationEntity**: Marker interface for entities scoped to an organization, enabling EF Core global query filters by `OrganizationId`.
- **Clean_Architecture**: Four-layer architecture — Domain (entities, interfaces), Application (DTOs, validators), Infrastructure (EF Core, Redis, HTTP clients), Api (controllers, middleware).
- **Outbox_Message**: Standardized JSON message published to Redis outbox queues with fields: `Type` (notification | audit), `Payload` (event-specific data), `Timestamp`, and `Id`.
- **Template_Variables**: Key-value pairs injected into notification templates for rendering (e.g., `StoryKey`, `StoryTitle`, `AssignerName`).

## Requirements

### Requirement 1: Audit Logging (REQ-071)

**User Story:** As an OrgAdmin, I want an immutable audit trail of all significant platform events so that I can review activity for compliance and troubleshooting.

#### Acceptance Criteria

1. WHEN `POST /api/v1/audit-logs` is called by a service via service JWT, THE UtilityService SHALL create an immutable audit log entry with: `OrganizationId`, `ServiceName`, `Action`, `EntityType`, `EntityId`, `UserId`, `OldValue` (JSON), `NewValue` (JSON), `IpAddress`, `CorrelationId`, and `DateCreated`.
2. WHEN `GET /api/v1/audit-logs` is called with a valid Bearer token, THE UtilityService SHALL return paginated audit logs filterable by `ServiceName`, `Action`, `EntityType`, `UserId`, and date range, scoped to the authenticated user's organization.
3. WHEN any attempt is made to UPDATE or DELETE an audit log record, THE UtilityService SHALL return HTTP 405 with `AUDIT_LOG_IMMUTABLE` (6001).
4. WHEN `GET /api/v1/audit-logs/archive` is called with a valid Bearer token, THE UtilityService SHALL return archived audit logs with the same pagination and filtering as active logs.
5. WHEN `POST /api/v1/audit-logs` is called without a valid service JWT, THE UtilityService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED`.
6. THE UtilityService SHALL index the `OrganizationId`, `ServiceName`, `Action`, `EntityType`, and `DateCreated` columns on the `audit_log` table for query performance.

### Requirement 2: Error Logging with PII Redaction (REQ-072)

**User Story:** As a developer, I want error logs with PII automatically redacted so that debugging is possible without exposing sensitive data.

#### Acceptance Criteria

1. WHEN `POST /api/v1/error-logs` is called by a service via service JWT, THE UtilityService SHALL create an error log entry with PII fields (emails, names, IP addresses) in `Message` and `StackTrace` replaced with `[REDACTED]` before persistence.
2. WHEN `GET /api/v1/error-logs` is called by an OrgAdmin, THE UtilityService SHALL return paginated error logs filterable by `ServiceName`, `ErrorCode`, `Severity`, and date range, scoped to the authenticated user's organization.
3. THE UtilityService SHALL store error log entries with: `ErrorLogId` (Guid PK), `OrganizationId`, `ServiceName`, `ErrorCode`, `Message` (redacted), `StackTrace` (redacted), `CorrelationId`, `Severity` (Info/Warning/Error/Critical), and `DateCreated`.
4. WHEN `POST /api/v1/error-logs` is called without a valid service JWT, THE UtilityService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED`.
5. WHEN the `Severity` field is not one of Info, Warning, Error, or Critical, THE UtilityService SHALL return HTTP 422 with `VALIDATION_ERROR` (1000).
6. THE UtilityService SHALL detect PII patterns including email addresses (regex pattern for `*@*.*`), names in known PII fields, and IPv4/IPv6 addresses, replacing each occurrence with `[REDACTED]`.

### Requirement 3: Error Code Registry (REQ-073)

**User Story:** As a developer, I want a centralized error code registry so that all services resolve error codes consistently.

#### Acceptance Criteria

1. WHEN `POST /api/v1/error-codes` is called by an OrgAdmin, THE UtilityService SHALL create an error code entry with: `Code` (string, unique), `Value` (int), `HttpStatusCode` (int), `ResponseCode` (string, max 10), `Description` (string), and `ServiceName` (string).
2. WHEN the error code `Code` already exists, THE UtilityService SHALL return HTTP 409 with `ERROR_CODE_DUPLICATE` (6002).
3. WHEN `GET /api/v1/error-codes` is called with a valid Bearer token, THE UtilityService SHALL return all error code entries, used by `ErrorCodeResolverService` in each service for cache refresh.
4. WHEN `PUT /api/v1/error-codes/{code}` is called by an OrgAdmin, THE UtilityService SHALL update the error code entry and invalidate the Redis cache at `error_codes_registry`.
5. WHEN `DELETE /api/v1/error-codes/{code}` is called by an OrgAdmin, THE UtilityService SHALL remove the error code entry and invalidate the Redis cache at `error_codes_registry`.
6. WHEN an error code is not found for update or delete, THE UtilityService SHALL return HTTP 404 with `ERROR_CODE_NOT_FOUND` (6003).
7. THE UtilityService SHALL cache the full error code registry in a Redis hash at `error_codes_registry` with 24-hour TTL.
8. WHEN error codes are created, updated, or deleted, THE UtilityService SHALL invalidate the `error_codes_registry` Redis hash so that downstream services pick up changes on their next cache miss.

### Requirement 4: Notification Dispatch (REQ-074)

**User Story:** As the platform, I want to dispatch notifications via email, push, and in-app channels so that team members are informed of relevant events.

#### Acceptance Criteria

1. WHEN `POST /api/v1/notifications/dispatch` is called by a service via service JWT, THE UtilityService SHALL dispatch the notification via the specified channels (Email, Push, InApp).
2. WHEN a notification is dispatched, THE UtilityService SHALL create a `NotificationLog` entry with: `OrganizationId`, `UserId`, `NotificationType`, `Channel`, `Recipient`, `Subject`, `Status` (Pending initially), `RetryCount` (0), and `DateCreated`.
3. WHEN all specified channels fail for a notification, THE UtilityService SHALL set the `NotificationLog` status to `PermanentlyFailed` and return HTTP 500 with `NOTIFICATION_DISPATCH_FAILED` (6004).
4. WHEN `GET /api/v1/notification-logs` is called with a valid Bearer token, THE UtilityService SHALL return the authenticated user's notification history, paginated and filterable by `NotificationType`, `Channel`, `Status`, and date range.
5. WHEN a notification is dispatched, THE UtilityService SHALL check the recipient's notification preferences — if the user has disabled a channel for the given notification type, that channel is skipped.
6. WHEN the notification preferences are checked, THE UtilityService SHALL use the cached preferences at `notif_pref:{userId}:{typeId}` (5-minute TTL), falling back to ProfileService on cache miss.
7. WHEN the `NotificationType` is not one of the 8 defined types, THE UtilityService SHALL return HTTP 400 with `INVALID_NOTIFICATION_TYPE` (6011).
8. WHEN the `Channel` is not one of Email, Push, or InApp, THE UtilityService SHALL return HTTP 400 with `INVALID_CHANNEL` (6012).

### Requirement 5: Agile-Specific Notification Types (REQ-075)

**User Story:** As a team member, I want to receive notifications for Agile workflow events so that I stay informed about relevant changes.

#### Acceptance Criteria

1. THE UtilityService SHALL support the following 8 notification types with their corresponding trigger events and template variables:

| Notification Type | Trigger | Template Variables |
|-------------------|---------|-------------------|
| `StoryAssigned` | Story assigned to member | `StoryKey`, `StoryTitle`, `AssignerName` |
| `TaskAssigned` | Task assigned to member | `StoryKey`, `TaskTitle`, `TaskType`, `AssignerName` |
| `SprintStarted` | Sprint activated | `SprintName`, `StartDate`, `EndDate`, `StoryCount` |
| `SprintEnded` | Sprint completed | `SprintName`, `Velocity`, `CompletionRate` |
| `MentionedInComment` | @mention in comment | `MentionerName`, `StoryKey`, `CommentPreview` |
| `StoryStatusChanged` | Story state transition | `StoryKey`, `StoryTitle`, `OldStatus`, `NewStatus` |
| `TaskStatusChanged` | Task state transition | `StoryKey`, `TaskTitle`, `OldStatus`, `NewStatus` |
| `DueDateApproaching` | Due date within 24 hours | `EntityType`, `StoryKey`, `Title`, `DueDate` |

2. WHEN a notification event is received via the outbox, THE UtilityService SHALL use the `NotificationType` to select the correct template and inject the `TemplateVariables` for rendering.
3. WHEN a notification is dispatched, THE UtilityService SHALL use the outbox pattern — the originating service publishes to `outbox:{service}` and UtilityService processes the message.

### Requirement 6: Notification Templates (REQ-076)

**User Story:** As the platform, I want pre-built notification templates so that notifications are professional and consistent.

#### Acceptance Criteria

1. WHEN a notification is dispatched, THE UtilityService SHALL render the appropriate template from the `Templates/` folder using the `NotificationType` to select the template file.
2. WHEN email templates are rendered, THE UtilityService SHALL use HTML templates with Razor-style placeholders for template variables (e.g., `{{StoryKey}}`, `{{StoryTitle}}`).
3. WHEN push or in-app templates are rendered, THE UtilityService SHALL use plain-text versions of the corresponding email templates.
4. WHEN a template file is not found for the given `NotificationType` and `Channel`, THE UtilityService SHALL return HTTP 404 with `TEMPLATE_NOT_FOUND` (6007).
5. WHEN the UtilityService is deployed, THE UtilityService SHALL include 8 email templates and 8 push/in-app templates (one per notification type): `story-assigned`, `task-assigned`, `sprint-started`, `sprint-ended`, `mentioned-in-comment`, `story-status-changed`, `task-status-changed`, `due-date-approaching`.

### Requirement 7: Reference Data Management (REQ-077)

**User Story:** As the platform, I want centralized reference data so that all services use consistent values for department types, priority levels, task types, and workflow states.

#### Acceptance Criteria

1. WHEN `GET /api/v1/reference/department-types` is called (no auth required), THE UtilityService SHALL return all active department types with `TypeName` and `TypeCode`.
2. WHEN `GET /api/v1/reference/priority-levels` is called (no auth required), THE UtilityService SHALL return all active priority levels with `Name`, `SortOrder`, and `Color`.
3. WHEN `GET /api/v1/reference/task-types` is called (no auth required), THE UtilityService SHALL return all active task types with `TypeName` and `DefaultDepartmentCode`.
4. WHEN `GET /api/v1/reference/workflow-states` is called (no auth required), THE UtilityService SHALL return all active workflow states with `EntityType` (Story/Task), `StateName`, and `SortOrder`.
5. WHEN reference data is requested, THE UtilityService SHALL serve results from Redis cache with 24-hour TTL using keys: `ref:department_types`, `ref:priority_levels`, `ref:task_types`, `ref:workflow_states`. IF the cache misses, THEN THE UtilityService SHALL query the database and populate the cache.
6. WHEN `POST /api/v1/reference/department-types` is called by an OrgAdmin, THE UtilityService SHALL create a new department type entry and invalidate the `ref:department_types` cache.
7. WHEN `POST /api/v1/reference/priority-levels` is called by an OrgAdmin, THE UtilityService SHALL create a new priority level entry and invalidate the `ref:priority_levels` cache.
8. WHEN a duplicate reference data entry is created (same `TypeName` or `TypeCode` for department types, same `Name` for priority levels), THE UtilityService SHALL return HTTP 409 with `REFERENCE_DATA_DUPLICATE` (6014).
9. WHEN a reference data entry is not found, THE UtilityService SHALL return HTTP 404 with `REFERENCE_DATA_NOT_FOUND` (6005).

### Requirement 8: Retention Archival (REQ-078)

**User Story:** As an OrgAdmin, I want audit logs to be automatically archived after a configurable period so that the active database stays performant.

#### Acceptance Criteria

1. WHEN the `RetentionArchivalHostedService` runs (daily at the hour configured via `RETENTION_SCHEDULE_HOUR`), THE UtilityService SHALL move audit logs with `DateCreated` older than `RETENTION_PERIOD_DAYS` (default 90) to the `archived_audit_log` table.
2. WHEN audit logs are archived, THE UtilityService SHALL copy each record to the `archived_audit_log` table with an `ArchivedDate` set to `DateTime.UtcNow`, then delete the original record from the `audit_log` table.
3. WHEN archived logs are queried via `GET /api/v1/audit-logs/archive`, THE UtilityService SHALL return them with the same pagination and filtering capabilities as active audit logs.
4. WHEN `RETENTION_PERIOD_DAYS` is configured as 0 or a negative value, THE UtilityService SHALL reject the configuration with `RETENTION_PERIOD_INVALID` (6013).
5. WHEN the retention archival process runs, THE UtilityService SHALL process records in batches to avoid long-running transactions and excessive database locks.

### Requirement 9: Outbox Processing (REQ-080, REQ-085, REQ-093)

**User Story:** As the platform, I want background outbox processing so that audit events and notifications from all services are handled asynchronously and reliably.

#### Acceptance Criteria

1. WHEN UtilityService starts, THE UtilityService SHALL start the `OutboxProcessorHostedService` which polls Redis outbox queues (`outbox:security`, `outbox:profile`, `outbox:work`) on a configurable interval (default 30 seconds, configurable via `OUTBOX_POLL_INTERVAL_SECONDS`).
2. WHEN the `OutboxProcessorHostedService` reads a message from an outbox queue, THE UtilityService SHALL deserialize the message and route it based on the `Type` field: `"audit"` messages are routed to the audit log creation handler, `"notification"` messages are routed to the notification dispatch handler.
3. WHEN outbox processing fails for a message, THE UtilityService SHALL re-queue the message with an incremented retry counter. IF the retry counter reaches 3, THEN THE UtilityService SHALL move the message to the dead-letter queue (`dlq:{service}`) and log the failure.
4. WHEN a service publishes to its outbox queue, THE UtilityService SHALL expect messages in the standardized format: `{ "Type": "notification | audit", "Payload": { ... }, "Timestamp": "ISO8601", "Id": "guid" }`.
5. WHEN an audit-type outbox message is processed, THE UtilityService SHALL extract the audit log fields from the `Payload` and create an `AuditLog` record.
6. WHEN a notification-type outbox message is processed, THE UtilityService SHALL extract the notification fields from the `Payload` and dispatch the notification via the specified channels.
7. WHEN an outbox message cannot be deserialized or has an unknown `Type`, THE UtilityService SHALL move the message to the dead-letter queue and log the error with `OUTBOX_PROCESSING_FAILED` (6015).

### Requirement 10: Background Services (REQ-080)

**User Story:** As the platform, I want multiple background services to handle async processing so that the system operates reliably without blocking API responses.

#### Acceptance Criteria

1. WHEN UtilityService starts, THE UtilityService SHALL start the following background services:

| Service | Description | Interval |
|---------|-------------|----------|
| `OutboxProcessorHostedService` | Polls Redis outbox queues (`outbox:profile`, `outbox:security`, `outbox:work`), dispatches notifications and creates audit logs | Configurable (default 30s) |
| `RetentionArchivalHostedService` | Moves audit logs older than retention period to archive table | Daily at configured hour |
| `NotificationRetryHostedService` | Retries failed notification dispatches with exponential backoff (2^retryCount minutes), max 3 retries | Every 60 seconds |
| `DueDateNotificationHostedService` | Scans for stories/tasks with due dates within 24 hours and publishes `DueDateApproaching` notifications | Every 6 hours |

2. WHEN the `NotificationRetryHostedService` runs, THE UtilityService SHALL query `NotificationLog` entries with `Status = 'Failed'` and `RetryCount < 3`, and retry dispatch with exponential backoff delay of `2^RetryCount` minutes.
3. WHEN a notification retry succeeds, THE UtilityService SHALL update the `NotificationLog` status to `Sent`.
4. WHEN a notification retry fails and `RetryCount` reaches 3, THE UtilityService SHALL update the `NotificationLog` status to `PermanentlyFailed`.
5. WHEN the `DueDateNotificationHostedService` runs, THE UtilityService SHALL scan for entities with due dates within 24 hours and publish `DueDateApproaching` notification events, avoiding duplicate notifications for the same entity within a 24-hour window.

### Requirement 11: Activity Feed Aggregation (REQ-079)

**User Story:** As a team member, I want an aggregated activity feed so that I can see recent changes across all stories and tasks.

#### Acceptance Criteria

1. WHEN UtilityService processes outbox messages from all services, THE UtilityService SHALL create audit log entries that serve as the data source for activity feeds.
2. WHEN `GET /api/v1/audit-logs` is called with `action` filter (e.g., `action=StoryCreated,TaskAssigned,StatusChanged`), THE UtilityService SHALL return a filtered activity feed showing recent Agile events.
3. WHEN the activity feed is queried, THE UtilityService SHALL scope results to the authenticated user's organization via the `OrganizationId` from JWT claims.

### Requirement 12: UtilityService API Endpoints (REQ-081)

**User Story:** As a developer, I want a complete set of utility endpoints so that all operational concerns are addressed.

#### Acceptance Criteria

1. THE UtilityService SHALL expose the following endpoints:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/audit-logs` | Service | Create audit log entry |
| GET | `/api/v1/audit-logs` | Bearer | Query audit logs (paginated, filtered) |
| GET | `/api/v1/audit-logs/archive` | Bearer | Query archived audit logs |
| POST | `/api/v1/error-logs` | Service | Create error log (PII redacted) |
| GET | `/api/v1/error-logs` | OrgAdmin | Query error logs |
| POST | `/api/v1/error-codes` | OrgAdmin | Create error code entry |
| GET | `/api/v1/error-codes` | Bearer | List all error codes |
| PUT | `/api/v1/error-codes/{code}` | OrgAdmin | Update error code |
| DELETE | `/api/v1/error-codes/{code}` | OrgAdmin | Delete error code |
| POST | `/api/v1/notifications/dispatch` | Service | Dispatch notification event |
| GET | `/api/v1/notification-logs` | Bearer | User notification history |
| GET | `/api/v1/reference/department-types` | None | List department types |
| GET | `/api/v1/reference/priority-levels` | None | List priority levels |
| GET | `/api/v1/reference/task-types` | None | List task types with department mapping |
| GET | `/api/v1/reference/workflow-states` | None | List workflow states |
| POST | `/api/v1/reference/department-types` | OrgAdmin | Create department type |
| POST | `/api/v1/reference/priority-levels` | OrgAdmin | Create priority level |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

2. THE UtilityService SHALL use URL path versioning with prefix `/api/v1/`.
3. THE UtilityService SHALL return all responses in the `ApiResponse<T>` envelope format with `CorrelationId`.
4. WHEN a request fails FluentValidation, THE UtilityService SHALL return HTTP 422 with error code `VALIDATION_ERROR` (1000) and the list of validation errors.

### Requirement 13: UtilityService Redis Key Patterns (REQ-082)

**User Story:** As a developer, I want well-defined Redis key patterns for UtilityService caching so that caching is consistent and predictable.

#### Acceptance Criteria

1. THE UtilityService SHALL use the following Redis key patterns with their specified TTLs:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `ref:department_types` | Cached department type list | 24 hours |
| `ref:priority_levels` | Cached priority level list | 24 hours |
| `ref:task_types` | Cached task type list | 24 hours |
| `ref:workflow_states` | Cached workflow state list | 24 hours |
| `notif_pref:{userId}:{typeId}` | Cached notification preferences | 5 minutes |
| `outbox:profile` | Inbound outbox from ProfileService | Until processed |
| `outbox:security` | Inbound outbox from SecurityService | Until processed |
| `outbox:work` | Inbound outbox from WorkService | Until processed |
| `dlq:profile` | Dead-letter queue for ProfileService outbox | Until manually processed |
| `dlq:security` | Dead-letter queue for SecurityService outbox | Until manually processed |
| `dlq:work` | Dead-letter queue for WorkService outbox | Until manually processed |
| `error_codes_registry` | Cached error code registry (hash) | 24 hours |

2. THE UtilityService SHALL use consistent key naming with colon-separated segments.
3. THE UtilityService SHALL set appropriate TTLs on all Redis keys to prevent unbounded memory growth.

### Requirement 14: UtilityService Data Models (REQ-083)

**User Story:** As a developer, I want well-defined data models so that the UtilityService database schema is clear and supports all operational concerns.

#### Acceptance Criteria

1. THE UtilityService SHALL maintain an `audit_log` table with columns: `AuditLogId` (Guid, PK), `OrganizationId` (Guid, required, indexed), `ServiceName` (string, required), `Action` (string, required), `EntityType` (string, required), `EntityId` (string, required), `UserId` (string, required), `OldValue` (string, nullable), `NewValue` (string, nullable), `IpAddress` (string, nullable), `CorrelationId` (string, required), `DateCreated` (DateTime, required).
2. THE UtilityService SHALL maintain an `archived_audit_log` table with the same columns as `audit_log` plus `ArchivedAuditLogId` (Guid, PK) and `ArchivedDate` (DateTime, required).
3. THE UtilityService SHALL maintain an `error_log` table with columns: `ErrorLogId` (Guid, PK), `OrganizationId` (Guid, required, indexed), `ServiceName` (string, required), `ErrorCode` (string, required), `Message` (string, required), `StackTrace` (string, nullable), `CorrelationId` (string, required), `Severity` (string, required), `DateCreated` (DateTime, required).
4. THE UtilityService SHALL maintain an `error_code_entry` table with columns: `ErrorCodeEntryId` (Guid, PK), `Code` (string, required, unique index), `Value` (int, required), `HttpStatusCode` (int, required), `ResponseCode` (string, required, max 10), `Description` (string, required), `ServiceName` (string, required), `DateCreated` (DateTime), `DateUpdated` (DateTime).
5. THE UtilityService SHALL maintain a `notification_log` table with columns: `NotificationLogId` (Guid, PK), `OrganizationId` (Guid, required, indexed), `UserId` (Guid, required, indexed), `NotificationType` (string, required), `Channel` (string, required), `Recipient` (string, required), `Subject` (string, nullable), `Status` (string, required), `RetryCount` (int, default 0), `LastRetryDate` (DateTime, nullable), `DateCreated` (DateTime, required).
6. THE UtilityService SHALL maintain a `department_type` table with columns: `DepartmentTypeId` (Guid, PK), `TypeName` (string, required), `TypeCode` (string, required), `FlgStatus` (string, default `A`).
7. THE UtilityService SHALL maintain a `priority_level` table with columns: `PriorityLevelId` (Guid, PK), `Name` (string, required), `SortOrder` (int, required), `Color` (string, required), `FlgStatus` (string, default `A`).
8. THE UtilityService SHALL maintain a `task_type_ref` table with columns: `TaskTypeRefId` (Guid, PK), `TypeName` (string, required), `DefaultDepartmentCode` (string, required), `FlgStatus` (string, default `A`).
9. THE UtilityService SHALL maintain a `workflow_state` table with columns: `WorkflowStateId` (Guid, PK), `EntityType` (string, required), `StateName` (string, required), `SortOrder` (int, required), `FlgStatus` (string, default `A`).
10. THE UtilityService SHALL use EF Core with PostgreSQL (Npgsql) and apply auto-migrations via `DatabaseMigrationHelper` on startup.

### Requirement 15: Seed Data (REQ-084)

**User Story:** As a developer, I want predefined reference data so that the system has consistent defaults on first deployment.

#### Acceptance Criteria

1. WHEN the UtilityService database is initialized, THE UtilityService SHALL seed the following department types: Engineering (ENG), QA (QA), DevOps (DEVOPS), Product (PROD), Design (DESIGN).
2. WHEN the UtilityService database is initialized, THE UtilityService SHALL seed the following priority levels: Critical (SortOrder 1, Color #DC2626), High (SortOrder 2, Color #EA580C), Medium (SortOrder 3, Color #CA8A04), Low (SortOrder 4, Color #16A34A).
3. WHEN the UtilityService database is initialized, THE UtilityService SHALL seed the following task types with department mappings: Development (ENG), Testing (QA), DevOps (DEVOPS), Design (DESIGN), Documentation (PROD), Bug (ENG).
4. WHEN the UtilityService database is initialized, THE UtilityService SHALL seed the following story workflow states: Backlog (SortOrder 1), Ready (SortOrder 2), InProgress (SortOrder 3), InReview (SortOrder 4), QA (SortOrder 5), Done (SortOrder 6), Closed (SortOrder 7).
5. WHEN the UtilityService database is initialized, THE UtilityService SHALL seed the following task workflow states: ToDo (SortOrder 1), InProgress (SortOrder 2), InReview (SortOrder 3), Done (SortOrder 4).
6. WHEN seed data already exists in the database, THE UtilityService SHALL skip seeding to avoid duplicate entries.

### Requirement 16: UtilityService Error Codes (REQ-071–REQ-085)

**User Story:** As a developer, I want well-defined error codes for UtilityService so that error handling is consistent and predictable.

#### Acceptance Criteria

1. THE UtilityService SHALL use the following error codes:

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| VALIDATION_ERROR | 1000 | 422 | FluentValidation pipeline failure |
| AUDIT_LOG_IMMUTABLE | 6001 | 405 | Cannot modify or delete audit logs |
| ERROR_CODE_DUPLICATE | 6002 | 409 | Duplicate error code entry |
| ERROR_CODE_NOT_FOUND | 6003 | 404 | Unknown error code |
| NOTIFICATION_DISPATCH_FAILED | 6004 | 500 | All notification channels failed |
| REFERENCE_DATA_NOT_FOUND | 6005 | 404 | Unknown reference data ID |
| ORGANIZATION_MISMATCH | 6006 | 403 | Cross-organization access attempt |
| TEMPLATE_NOT_FOUND | 6007 | 404 | Notification template not found |
| NOT_FOUND | 6008 | 404 | Entity not found |
| CONFLICT | 6009 | 409 | Duplicate or state conflict |
| SERVICE_UNAVAILABLE | 6010 | 503 | Downstream timeout or circuit open |
| INVALID_NOTIFICATION_TYPE | 6011 | 400 | Unknown notification type |
| INVALID_CHANNEL | 6012 | 400 | Unknown notification channel |
| RETENTION_PERIOD_INVALID | 6013 | 400 | Retention period must be greater than 0 days |
| REFERENCE_DATA_DUPLICATE | 6014 | 409 | Duplicate reference data entry |
| OUTBOX_PROCESSING_FAILED | 6015 | 500 | Outbox message could not be processed |

2. THE UtilityService SHALL define all error codes as constants in a static `ErrorCodes` class in the Domain layer.
3. THE UtilityService SHALL define a `DomainException` subclass for each error code that requires specific handling (e.g., `AuditLogImmutableException`, `ErrorCodeDuplicateException`, `NotificationDispatchFailedException`).

### Requirement 17: Clean Architecture Layer Structure (REQ-086)

**User Story:** As a developer, I want UtilityService to follow Clean Architecture so that the codebase is maintainable and testable.

#### Acceptance Criteria

1. THE UtilityService SHALL be structured as four projects: `UtilityService.Domain`, `UtilityService.Application`, `UtilityService.Infrastructure`, `UtilityService.Api`.
2. WHEN the Domain layer is built, THE UtilityService.Domain SHALL have zero `ProjectReference` entries and zero ASP.NET Core or EF Core package references.
3. WHEN the Application layer is built, THE UtilityService.Application SHALL reference only `UtilityService.Domain` and contain no infrastructure packages.
4. WHEN the Infrastructure layer is built, THE UtilityService.Infrastructure SHALL reference `UtilityService.Domain` and `UtilityService.Application`.
5. WHEN the Api layer is built, THE UtilityService.Api SHALL reference `UtilityService.Application` and `UtilityService.Infrastructure` and serve as the composition root for all DI registrations.

### Requirement 18: Organization Isolation (REQ-087)

**User Story:** As the platform, I want all UtilityService database queries to be automatically scoped to the current organization so that data isolation is enforced at the database level.

#### Acceptance Criteria

1. WHEN EF Core queries are executed against organization-scoped entities (AuditLog, ErrorLog, NotificationLog), THE UtilityService SHALL apply global query filters that automatically scope all queries by `OrganizationId`.
2. WHEN an entity implements `IOrganizationEntity`, THE UtilityService SHALL apply the global query filter for that entity.
3. WHEN `OrganizationScopeMiddleware` processes a request, THE UtilityService SHALL extract `organizationId` from JWT claims and store it in `HttpContext.Items["OrganizationId"]`.
4. WHEN inter-service calls are made, THE UtilityService SHALL propagate the `X-Organization-Id` header.
5. WHEN a request attempts to access data from a different organization, THE UtilityService SHALL return HTTP 403 with `ORGANIZATION_MISMATCH` (6006).

### Requirement 19: Standardized Error Handling (REQ-088)

**User Story:** As a developer, I want all UtilityService errors handled consistently so that clients receive predictable error responses.

#### Acceptance Criteria

1. WHEN a `DomainException` is thrown, THE UtilityService SHALL catch it via `GlobalExceptionHandlerMiddleware` and return an `ApiResponse<object>` with `application/problem+json` content type, including the error's `ErrorCode`, `ErrorValue`, `Message`, and `CorrelationId`.
2. WHEN an unhandled exception is thrown, THE UtilityService SHALL return HTTP 500 with `ErrorCode = "INTERNAL_ERROR"`, `Message = "An unexpected error occurred."`, and `CorrelationId`. THE UtilityService SHALL not leak stack traces or internal details.
3. WHEN any error response is returned, THE UtilityService SHALL include the `CorrelationId` from `HttpContext.Items["CorrelationId"]`.

### Requirement 20: FluentValidation Pipeline (REQ-090)

**User Story:** As a developer, I want automatic request validation so that invalid data is rejected before reaching UtilityService business logic.

#### Acceptance Criteria

1. WHEN a request DTO has a corresponding FluentValidation validator, THE UtilityService SHALL auto-discover and execute the validator before the controller action.
2. WHEN validation fails, THE UtilityService SHALL return HTTP 422 with `ErrorCode = "VALIDATION_ERROR"`, `ErrorValue = 1000`, and per-field errors in the `Errors` array as `{ field, message }` objects.
3. WHEN ASP.NET Core's built-in `ModelStateInvalidFilter` is configured, THE UtilityService SHALL disable it via `SuppressModelStateInvalidFilter = true` to let FluentValidation handle all validation.

### Requirement 21: CorrelationId Propagation (REQ-092)

**User Story:** As a developer, I want end-to-end request tracing in UtilityService so that I can debug issues across services.

#### Acceptance Criteria

1. WHEN a request enters UtilityService, THE UtilityService SHALL extract `X-Correlation-Id` from the request header via `CorrelationIdMiddleware` or generate a new GUID if the header is absent.
2. WHEN the correlation ID is established, THE UtilityService SHALL store it in `HttpContext.Items["CorrelationId"]` and add it to the response header `X-Correlation-Id`.
3. WHEN any API response is returned, THE UtilityService SHALL include the `CorrelationId` in the `ApiResponse` body.

### Requirement 22: Database Migrations (REQ-094)

**User Story:** As a developer, I want database migrations to auto-apply on UtilityService startup so that deployment is simplified.

#### Acceptance Criteria

1. WHEN UtilityService starts, THE UtilityService SHALL call `DatabaseMigrationHelper.ApplyMigrations(app)` to check for pending EF Core migrations and apply them.
2. WHEN the database is InMemory (test environment), THE UtilityService SHALL call `EnsureCreated()` instead of `Migrate()`.
3. WHEN no pending migrations exist, THE UtilityService SHALL proceed with startup without database changes.

### Requirement 23: Health Checks (REQ-095)

**User Story:** As a DevOps engineer, I want health check endpoints so that I can monitor UtilityService availability.

#### Acceptance Criteria

1. WHEN `GET /health` is called, THE UtilityService SHALL return HTTP 200 if the process is running (liveness probe).
2. WHEN `GET /ready` is called, THE UtilityService SHALL check database connectivity (PostgreSQL) and Redis connection and return HTTP 200 if both are healthy (readiness probe).
3. WHEN either the database or Redis is unreachable, THE UtilityService SHALL return HTTP 503 from the readiness endpoint.

### Requirement 24: Pagination (REQ-096)

**User Story:** As a developer, I want consistent pagination across all UtilityService list endpoints so that large datasets are handled efficiently.

#### Acceptance Criteria

1. WHEN any list endpoint is called (audit logs, error logs, notification logs, error codes, reference data), THE UtilityService SHALL support `page` (default 1) and `pageSize` (default 20, max 100) query parameters.
2. WHEN the response is paginated, THE UtilityService SHALL include: `TotalCount`, `Page`, `PageSize`, `TotalPages`, and the `Data` array.
3. WHEN `pageSize` exceeds 100, THE UtilityService SHALL cap it at 100.

### Requirement 25: Soft Delete Pattern (REQ-097)

**User Story:** As the platform, I want soft deletes for reference data so that data is never permanently lost.

#### Acceptance Criteria

1. WHEN a reference data entity (DepartmentType, PriorityLevel, TaskTypeRef, WorkflowState) is "deleted", THE UtilityService SHALL set its `FlgStatus` to `D` instead of physically removing the record.
2. WHEN reference data entities are queried, THE UtilityService SHALL apply EF Core global query filters to exclude entities with `FlgStatus = 'D'` by default.
3. WHEN an admin needs to see deleted reference data, THE UtilityService SHALL support bypassing the query filter with `.IgnoreQueryFilters()`.

### Requirement 26: Structured Logging (REQ-098)

**User Story:** As a developer, I want structured logging in UtilityService so that logs are searchable and correlatable.

#### Acceptance Criteria

1. WHEN a `DomainException` is logged, THE UtilityService SHALL include structured properties: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`.
2. WHEN an unhandled exception is logged, THE UtilityService SHALL include structured properties: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`.
3. WHEN outbox processing fails, THE UtilityService SHALL log at Warning level with structured properties: `CorrelationId`, `OutboxQueue`, `MessageId`, `RetryCount`, `ErrorMessage`.
4. WHEN notification dispatch fails, THE UtilityService SHALL log at Warning level with structured properties: `CorrelationId`, `NotificationType`, `Channel`, `Recipient`, `ErrorMessage`.

### Requirement 27: Configuration via Environment Variables (REQ-101)

**User Story:** As a DevOps engineer, I want all UtilityService configuration via environment variables so that the service is 12-factor compliant.

#### Acceptance Criteria

1. WHEN UtilityService starts, THE UtilityService SHALL load configuration from a `.env` file via `DotNetEnv` and populate an `AppSettings` singleton.
2. WHEN a required environment variable is missing, THE UtilityService SHALL throw `InvalidOperationException` at startup with a clear message identifying the missing variable.
3. THE UtilityService SHALL support the following configuration variables:

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DATABASE_URL` | Yes | — | PostgreSQL connection string |
| `REDIS_URL` | Yes | — | Redis connection string |
| `JWT_SECRET` | Yes | — | JWT signing key |
| `OUTBOX_POLL_INTERVAL_SECONDS` | No | 30 | Outbox polling interval |
| `RETENTION_PERIOD_DAYS` | No | 90 | Audit log retention period |
| `RETENTION_SCHEDULE_HOUR` | No | 2 | Hour (UTC) for retention archival |
| `NOTIFICATION_RETRY_MAX` | No | 3 | Max notification retry attempts |
| `ALLOWED_ORIGINS` | No | * | CORS allowed origins |
| `ASPNETCORE_ENVIRONMENT` | No | Production | Runtime environment |

### Requirement 28: CORS Configuration (REQ-102)

**User Story:** As a developer, I want CORS configured on UtilityService so that the frontend can communicate with the service.

#### Acceptance Criteria

1. WHEN UtilityService starts, THE UtilityService SHALL configure CORS with allowed origins from the `ALLOWED_ORIGINS` environment variable (comma-separated).
2. WHEN a preflight request is received, THE UtilityService SHALL respond with appropriate CORS headers.

### Requirement 29: Swagger Documentation (REQ-103)

**User Story:** As a developer, I want Swagger UI so that I can explore and test UtilityService API endpoints.

#### Acceptance Criteria

1. WHEN UtilityService is running in Development mode, THE UtilityService SHALL serve Swagger UI at `http://localhost:5200/swagger`.
2. WHEN Swagger is configured, THE UtilityService SHALL include JWT Bearer authentication support for testing authenticated endpoints.

### Requirement 30: Middleware Pipeline Order

**User Story:** As the platform, I want a well-defined middleware pipeline for UtilityService so that cross-cutting concerns are enforced in the correct order.

#### Acceptance Criteria

1. WHEN a request enters UtilityService, THE UtilityService SHALL execute middleware in this exact order: CORS → CorrelationId → GlobalExceptionHandler → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → OrganizationScope → Controllers.
2. WHEN `GlobalExceptionHandlerMiddleware` catches a `DomainException`, THE UtilityService SHALL return the appropriate HTTP status code with `application/problem+json` content type and the `ApiResponse<T>` envelope.
3. WHEN `GlobalExceptionHandlerMiddleware` catches an unhandled exception, THE UtilityService SHALL return HTTP 500 with a generic error message and log the exception with structured properties.
4. THE UtilityService SHALL generate or propagate a `CorrelationId` (`X-Correlation-Id` header) on every request via `CorrelationIdMiddleware`.

### Requirement 31: API Versioning (REQ-100)

**User Story:** As a developer, I want API versioning on UtilityService so that breaking changes can be introduced without affecting existing clients.

#### Acceptance Criteria

1. WHEN any UtilityService endpoint is defined, THE UtilityService SHALL use URL path versioning: `/api/v1/...`.
2. WHEN a new version is needed, THE UtilityService SHALL add it as `/api/v2/...` without removing the v1 endpoints.
