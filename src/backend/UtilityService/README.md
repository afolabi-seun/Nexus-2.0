# UtilityService

Audit logging, notifications, and reference data microservice for the Nexus 2.0 platform.

- **Port:** 5200
- **Database:** `nexus_utility` (PostgreSQL)
- **Base path:** `/api/v1`

## Responsibilities

- **Audit logging** — Immutable audit trail (no update/delete); service-to-service creation, org-scoped queries, archive queries
- **Error logging** — Structured error logs with PII redaction
- **Notification dispatch** — Multi-channel delivery (Email/Push/InApp) with SMTP email integration and 16 notification templates
- **Error code registry** — CRUD for application error codes with role restriction codes seeded
- **Reference data** — Department types, priority levels, task types, workflow states (requires authentication)
- **Retention archival** — Automatic archival of aged audit logs based on configurable retention period

### Background Hosted Services

1. **Outbox Processor** — Polls outbox table and dispatches pending messages (configurable interval)
2. **Notification Dispatcher** — Processes notification queue with retry logic (max 3 retries)
3. **Retention Archival** — Moves audit logs older than retention period to archive table (runs at configurable hour)
4. **Digest Scheduler** — Aggregates and dispatches notification digests

## API Endpoints

### Audit Logs (`/api/v1/audit-logs`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/audit-logs` | Service | Create audit log entry |
| GET | `/audit-logs` | OrgAdmin | Query audit logs (paginated, filterable) |
| GET | `/audit-logs/archive` | OrgAdmin | Query archived audit logs |
| PUT | `/audit-logs` | — | 405 — Audit logs are immutable |
| DELETE | `/audit-logs` | — | 405 — Audit logs are immutable |

### Error Logs (`/api/v1/error-logs`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/error-logs` | Service | Create error log (PII redacted) |
| GET | `/error-logs` | OrgAdmin | Query error logs (paginated, filterable) |

### Notifications (`/api/v1`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/notifications/dispatch` | Service | Dispatch notification |
| GET | `/notification-logs` | Bearer | Get user notification history |

### Error Codes (`/api/v1/error-codes`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/error-codes` | OrgAdmin | Create error code |
| GET | `/error-codes` | Bearer | List error codes |
| PUT | `/error-codes/{code}` | OrgAdmin | Update error code |
| DELETE | `/error-codes/{code}` | OrgAdmin | Delete error code |

### Reference Data (`/api/v1/reference`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/reference/department-types` | Bearer | Get department types |
| GET | `/reference/priority-levels` | Bearer | Get priority levels |
| GET | `/reference/task-types` | Bearer | Get task types |
| GET | `/reference/workflow-states` | Bearer | Get workflow states |
| POST | `/reference/department-types` | OrgAdmin | Create department type |
| POST | `/reference/priority-levels` | OrgAdmin | Create priority level |

## Project Structure

```
UtilityService/
├── UtilityService.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   └── Common/
├── UtilityService.Application/
│   ├── DTOs/
│   └── Validators/
├── UtilityService.Infrastructure/
│   ├── Configuration/
│   ├── Data/
│   ├── Templates/             # Email and Push notification templates
│   │   ├── Email/
│   │   └── Push/
│   ├── Repositories/
│   │   ├── AuditLogs/
│   │   ├── ArchivedAuditLogs/
│   │   ├── ErrorLogs/
│   │   ├── NotificationLogs/
│   │   ├── ErrorCodeEntries/
│   │   ├── DepartmentTypes/
│   │   ├── PriorityLevels/
│   │   ├── TaskTypeRefs/
│   │   └── WorkflowStates/
│   └── Services/
│       ├── AuditLogs/
│       ├── ErrorLogs/
│       ├── Notifications/
│       ├── ErrorCodes/
│       ├── ReferenceData/
│       ├── PiiRedaction/
│       ├── Outbox/
│       ├── ErrorCodeResolver/
│       └── BackgroundServices/
├── UtilityService.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Attributes/
│   └── Extensions/
└── UtilityService.Tests/
```

## Architecture Conventions

- **Clean Architecture** — 4 layers: Domain → Application → Infrastructure → Api. Dependencies flow inward only.
- **Entity-named subfolders** — Repositories and Services are organized into subfolders named after the entity they manage (e.g., `Repositories/AuditLogs/AuditLogRepository.cs`). Namespaces match folder paths.
- **ApiResponse envelope** — All API responses wrapped in `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, and `CorrelationId`.
- **DomainException pattern** — Business rule violations throw typed exceptions (e.g., `ErrorCodeNotFoundException`) with error codes, HTTP status codes, and correlation IDs. Caught by `GlobalExceptionHandlerMiddleware`.
- **Middleware pipeline** — CORS → CorrelationId → GlobalExceptionHandler → Serilog → RateLimiter → Routing → Auth → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
- **Polly resilience** — Inter-service HTTP calls use retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s).
- **Redis outbox** — Audit events published via `LPUSH outbox:{service}` for async processing by UtilityService.

## How to Run

```bash
cd src/backend/UtilityService/UtilityService.Api
cp .env.example .env   # Edit with your values
dotnet run
```

Service starts at `http://localhost:5200`. Swagger UI at `/swagger`.

## Environment Variables

See [`.env.example`](UtilityService.Api/.env.example) for all variables. Key settings:

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | PostgreSQL connection string |
| `REDIS_URL` | Redis host:port |
| `JWT_SECRET` | Shared JWT signing key |
| `OUTBOX_POLL_INTERVAL_SECONDS` | Outbox processor poll interval (default: 30) |
| `RETENTION_PERIOD_DAYS` | Audit log retention before archival (default: 90) |
| `RETENTION_SCHEDULE_HOUR` | Hour of day to run archival (default: 2) |
| `NOTIFICATION_RETRY_MAX` | Max notification delivery retries (default: 3) |
| `SMTP_HOST` | SMTP server host (default: localhost) |
| `SMTP_PORT` | SMTP server port (default: 1025) |
| `SMTP_FROM_ADDRESS` | Sender email address |
| `SMTP_FROM_NAME` | Sender display name |
| `SMTP_USERNAME` | SMTP auth username (optional) |
| `SMTP_PASSWORD` | SMTP auth password (optional) |
| `SMTP_USE_SSL` | Enable SSL/TLS (default: false) |
| `SEQ_URL` | Seq logging server URL |
