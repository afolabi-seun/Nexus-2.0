# UtilityService

Audit logging, notifications, and reference data microservice for the Nexus 2.0 platform.

- **Port:** 5200
- **Database:** `nexus_utility` (PostgreSQL)
- **Base path:** `/api/v1`

## Responsibilities

- **Audit logging** — Immutable audit trail (no update/delete); service-to-service creation, org-scoped queries, archive queries
- **Error logging** — Structured error logs with PII redaction
- **Notification dispatch** — Multi-channel delivery (Email/Push/InApp) with 16 notification templates
- **Error code registry** — CRUD for application error codes
- **Reference data** — Department types, priority levels, task types, workflow states
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
| GET | `/audit-logs` | Bearer | Query audit logs (paginated, filterable) |
| GET | `/audit-logs/archive` | Bearer | Query archived audit logs |
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
| GET | `/reference/department-types` | Public | Get department types |
| GET | `/reference/priority-levels` | Public | Get priority levels |
| GET | `/reference/task-types` | Public | Get task types |
| GET | `/reference/workflow-states` | Public | Get workflow states |
| POST | `/reference/department-types` | OrgAdmin | Create department type |
| POST | `/reference/priority-levels` | OrgAdmin | Create priority level |

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
| `SEQ_URL` | Seq logging server URL |
