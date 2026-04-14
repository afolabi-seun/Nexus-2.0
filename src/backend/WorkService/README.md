# WorkService

Project and work item management microservice for the Nexus 2.0 platform.

- **Port:** 5003
- **Database:** `nexus_work` (PostgreSQL)
- **Base path:** `/api/v1`

## Responsibilities

- **Project management** — CRUD with unique project keys (2-10 uppercase alphanumeric), status lifecycle
- **Story management** — Professional IDs (e.g., MOB-42), workflow state machine, labels, links, assignment
- **Task management** — Department auto-routing, assignment (DeptLead), self-assign, time logging
- **Sprint management** — Planning → Active → Completed lifecycle, story assignment, metrics, velocity
- **Board views** — Kanban, Sprint, Department, Backlog (all with filtering)
- **Comments** — CRUD with @mentions support on stories and tasks
- **Labels** — Organization-scoped labels for story categorization
- **Story links** — Relate stories (blocks, is-blocked-by, relates-to, duplicates)
- **Search** — PostgreSQL full-text search across stories, tasks, and projects
- **Reports** — Velocity, department workload, capacity utilization, cycle time, task completion
- **Saved filters** — Per-user saved filter configurations
- **Activity logging** — Track all changes on stories and tasks
- **Activity feed** — Organization-wide paginated activity feed
- **CSV export** — Export stories and time entries as CSV
- **Bulk operations** — Bulk status update and bulk assign for stories
- **Workflow engine** — Configurable state machine with org-level and department-level overrides
- **Sprint notifications** — Background service checks for sprints due soon, overdue, and at risk
- **Health checks** — `/health` and `/ready` endpoints with DB + Redis checks

## Workflow State Machine

```
                    ┌──────────┐
                    │  Backlog │
                    └────┬─────┘
                         │
                    ┌────▼─────┐
              ┌─────│  Ready   │─────┐
              │     └────┬─────┘     │
              │          │           │
         ┌────▼───┐ ┌───▼────┐      │
         │ Blocked│ │In Prog │      │
         └────┬───┘ └───┬────┘      │
              │         │           │
              │    ┌────▼─────┐     │
              └───►│ Review   │◄────┘
                   └────┬─────┘
                        │
                   ┌────▼─────┐
                   │   Done   │
                   └──────────┘
```

States: `Backlog` → `Ready` → `InProgress` → `Review` → `Done` (with `Blocked` as a side state).

## API Endpoints

### Projects (`/api/v1/projects`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/projects` | DeptLead+ | Create project |
| GET | `/projects` | Bearer | List projects (paginated, filterable) |
| GET | `/projects/{id}` | Bearer | Get project by ID |
| PUT | `/projects/{id}` | DeptLead+ | Update project |
| PATCH | `/projects/{id}/status` | OrgAdmin | Update project status |
| GET | `/projects/{id}/cost-summary` | Bearer | Get project cost summary |
| GET | `/projects/{id}/utilization` | Bearer | Get resource utilization |
| GET | `/projects/{id}/cost-snapshots` | Bearer | Get cost snapshots |
| GET | `/projects/export/stories` | Bearer | Export stories as CSV |
| GET | `/projects/export/time-entries` | Bearer | Export time entries as CSV |

### Stories (`/api/v1/stories`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/stories` | Bearer | Create story |
| GET | `/stories` | Bearer | List stories (paginated, filterable) |
| GET | `/stories/{id}` | Bearer | Get story by ID |
| GET | `/stories/by-key/{storyKey}` | Bearer | Get story by key (e.g., MOB-42) |
| PUT | `/stories/{id}` | Bearer | Update story |
| DELETE | `/stories/{id}` | DeptLead+ | Delete story |
| PATCH | `/stories/{id}/status` | Bearer | Transition workflow state |
| PATCH | `/stories/{id}/assign` | DeptLead+ | Assign story |
| PATCH | `/stories/{id}/unassign` | DeptLead+ | Unassign story |
| POST | `/stories/{id}/links` | Bearer | Add story link |
| DELETE | `/stories/{id}/links/{linkId}` | Bearer | Remove story link |
| POST | `/stories/{id}/labels` | Bearer | Add label to story |
| DELETE | `/stories/{id}/labels/{labelId}` | Bearer | Remove label from story |
| GET | `/stories/{id}/comments` | Bearer | List story comments |
| GET | `/stories/{id}/activity` | Bearer | List story activity log |
| GET | `/stories/{storyId}/tasks` | Bearer | List tasks for story |
| GET | `/activity-feed` | Bearer | Organization-wide activity feed |
| POST | `/stories/bulk/status` | DeptLead+ | Bulk update story statuses |
| POST | `/stories/bulk/assign` | DeptLead+ | Bulk assign stories |

### Tasks (`/api/v1/tasks`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/tasks` | Bearer | Create task |
| GET | `/tasks/{id}` | Bearer | Get task by ID |
| PUT | `/tasks/{id}` | Bearer | Update task |
| DELETE | `/tasks/{id}` | DeptLead+ | Delete task |
| PATCH | `/tasks/{id}/status` | Bearer | Transition task status |
| PATCH | `/tasks/{id}/assign` | DeptLead+ | Assign task |
| PATCH | `/tasks/{id}/self-assign` | Bearer | Self-assign task |
| PATCH | `/tasks/{id}/unassign` | DeptLead+ | Unassign task |
| PATCH | `/tasks/{id}/log-hours` | Bearer | Log hours on task |
| GET | `/tasks/{id}/activity` | Bearer | List task activity |
| GET | `/tasks/{id}/comments` | Bearer | List task comments |
| GET | `/tasks/suggest-assignee` | Bearer | Suggest assignee by task type |

### Sprints (`/api/v1`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/projects/{projectId}/sprints` | Bearer | Create sprint |
| GET | `/sprints` | Bearer | List sprints |
| GET | `/sprints/{id}` | Bearer | Get sprint by ID |
| PUT | `/sprints/{id}` | Bearer | Update sprint |
| PATCH | `/sprints/{id}/start` | Bearer | Start sprint |
| PATCH | `/sprints/{id}/complete` | Bearer | Complete sprint |
| PATCH | `/sprints/{id}/cancel` | Bearer | Cancel sprint |
| POST | `/sprints/{sprintId}/stories` | Bearer | Add story to sprint |
| DELETE | `/sprints/{sprintId}/stories/{storyId}` | Bearer | Remove story from sprint |
| GET | `/sprints/{id}/metrics` | Bearer | Get sprint metrics |
| GET | `/sprints/velocity` | Bearer | Get velocity chart data |
| GET | `/sprints/active` | Bearer | Get active sprint |

### Boards, Search, Reports, Labels, Workflows, Saved Filters

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/boards/kanban` | Bearer | Kanban board view |
| GET | `/boards/sprint` | Bearer | Sprint board view |
| GET | `/boards/backlog` | Bearer | Backlog view |
| GET | `/boards/department` | Bearer | Department board view |
| GET | `/search` | Bearer | Full-text search |
| GET | `/reports/velocity` | Bearer | Velocity report |
| GET | `/reports/department-workload` | Bearer | Department workload report |
| GET | `/reports/capacity` | Bearer | Capacity utilization report |
| GET | `/reports/cycle-time` | Bearer | Cycle time report |
| GET | `/reports/task-completion` | Bearer | Task completion report |
| POST | `/labels` | DeptLead+ | Create label |
| GET | `/labels` | Bearer | List labels |
| PUT | `/labels/{id}` | DeptLead+ | Update label |
| DELETE | `/labels/{id}` | OrgAdmin | Delete label |
| GET | `/workflows` | Bearer | Get workflow configuration |
| PUT | `/workflows/organization` | OrgAdmin | Save org workflow override |
| PUT | `/workflows/department/{deptId}` | DeptLead+ | Save dept workflow override |
| POST | `/saved-filters` | Bearer | Create saved filter |
| GET | `/saved-filters` | Bearer | List saved filters |
| DELETE | `/saved-filters/{id}` | Bearer | Delete saved filter |
| POST | `/comments` | Bearer | Create comment |
| PUT | `/comments/{id}` | Bearer | Update comment |
| DELETE | `/comments/{id}` | Bearer | Delete comment |

## Project Structure

```
WorkService/
├── WorkService.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Helpers/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   └── Common/
├── WorkService.Application/
│   ├── DTOs/
│   ├── Contracts/
│   └── Validators/
├── WorkService.Infrastructure/
│   ├── Configuration/
│   ├── Data/
│   ├── Repositories/
│   │   ├── Projects/
│   │   ├── Stories/
│   │   ├── StorySequences/
│   │   ├── Tasks/
│   │   ├── Sprints/
│   │   ├── SprintStories/
│   │   ├── Comments/
│   │   ├── Labels/
│   │   ├── StoryLabels/
│   │   ├── StoryLinks/
│   │   ├── ActivityLogs/
│   │   └── SavedFilters/
│   └── Services/
│       ├── Projects/
│       ├── Stories/
│       ├── Tasks/
│       ├── Sprints/
│       ├── Boards/
│       ├── Comments/
│       ├── Labels/
│       ├── Search/
│       ├── Reports/
│       ├── Workflows/
│       ├── ActivityLog/
│       ├── Outbox/
│       ├── ErrorCodeResolver/
│       └── ServiceClients/
├── WorkService.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Attributes/
│   └── Extensions/
└── WorkService.Tests/
```

## Architecture Conventions

- **Clean Architecture** — 4 layers: Domain → Application → Infrastructure → Api. Dependencies flow inward only.
- **Entity-named subfolders** — Repositories and Services are organized into subfolders named after the entity they manage (e.g., `Repositories/Projects/ProjectRepository.cs`). Namespaces match folder paths.
- **ApiResponse envelope** — All API responses wrapped in `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, and `CorrelationId`.
- **DomainException pattern** — Business rule violations throw typed exceptions (e.g., `ProjectNotFoundException`) with error codes, HTTP status codes, and correlation IDs. Caught by `GlobalExceptionHandlerMiddleware`.
- **Middleware pipeline** — CORS → CorrelationId → GlobalExceptionHandler → Serilog → RateLimiter → Routing → Auth → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
- **Polly resilience** — Inter-service HTTP calls use retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s).
- **Redis outbox** — Audit events published via `LPUSH outbox:{service}` for async processing by UtilityService.

## How to Run

```bash
cd src/backend/WorkService/WorkService.Api
cp .env.example .env   # Edit with your values
dotnet run
```

Service starts at `http://localhost:5003`. Swagger UI at `/swagger`.

## Environment Variables

See [`.env.example`](WorkService.Api/.env.example) for all variables. Key settings:

| Variable | Description |
|----------|-------------|
| `DATABASE_CONNECTION_STRING` | PostgreSQL connection string |
| `REDIS_CONNECTION_STRING` | Redis host:port |
| `JWT_SECRET_KEY` | Shared JWT signing key |
| `PROFILE_SERVICE_BASE_URL` | ProfileService URL for member lookups |
| `SECURITY_SERVICE_BASE_URL` | SecurityService URL |
| `UTILITY_SERVICE_BASE_URL` | UtilityService URL for audit logging |
| `SERVICE_SECRET` | Shared secret for service-to-service auth |
