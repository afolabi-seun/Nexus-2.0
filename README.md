# Nexus 2.0 — Enterprise Agile Platform

A multi-tenant agile project management platform built with .NET 8 microservices and a React SPA frontend. Nexus 2.0 provides organizations with story tracking, sprint management, Kanban boards, department-based workflows, RBAC, billing with Stripe integration, and real-time notifications.

## Architecture

5 backend microservices + 1 React SPA frontend, each with its own PostgreSQL database. Services communicate via REST with service-to-service JWT authentication. Redis is used for caching, sessions, rate limiting, and feature gate enforcement. Serilog aggregates structured logs to Seq.

| Service | Port | Database | Description |
|---------|------|----------|-------------|
| SecurityService | 5001 | nexus_security | Auth, JWT, sessions, RBAC, OTP, rate limiting |
| ProfileService | 5002 | nexus_profile | Organizations, departments, team members, invites |
| WorkService | 5003 | nexus_work | Projects, stories, tasks, sprints, boards |
| UtilityService | 5200 | nexus_utility | Audit logs, notifications, reference data |
| BillingService | 5300 | nexus_billing | Subscriptions, plans, feature gates, usage |
| Frontend | 5173 | — | React 18 SPA (TypeScript, Vite, Tailwind) |

## Tech Stack

- **Backend:** .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL, Redis, FluentValidation
- **Frontend:** React 18, TypeScript, Vite, Tailwind CSS v3, Zustand, React Router v6, Recharts, dnd-kit
- **Testing:** xUnit, Moq, FsCheck (401 backend tests) · Vitest, fast-check (93 frontend tests)
- **Infrastructure:** Polly (resilience), Serilog + Seq (logging), Stripe SDK (payments), Docker Compose
- **CI/CD:** GitHub Actions (build, test, Docker image push)

## Architecture Conventions

Each backend service follows Clean Architecture with 4 layers:

```
{Service}/
├── {Service}.Domain/              # Entities, interfaces, exceptions (no external deps)
├── {Service}.Application/         # DTOs, validators (FluentValidation)
├── {Service}.Infrastructure/      # EF Core, Redis, HTTP clients, background services
│   ├── Repositories/
│   │   └── {Entity}/              # Entity-named subfolders (e.g., Organizations/)
│   └── Services/
│       └── {Feature}/             # Feature-named subfolders (e.g., Auth/, Stripe/)
├── {Service}.Api/                 # Controllers, middleware, Program.cs
└── {Service}.Tests/               # xUnit + Moq + FsCheck
```

Key patterns shared across all services:
- **Entity-named subfolders** — Repositories and Services organized by entity/feature. Namespaces match folder paths.
- **ApiResponse envelope** — All responses wrapped in `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`.
- **DomainException pattern** — Typed exceptions with error codes and HTTP status codes, caught by `GlobalExceptionHandlerMiddleware`.
- **Middleware pipeline** — CORS → CorrelationId → GlobalExceptionHandler → Serilog → RateLimiter → Routing → Auth → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
- **Polly resilience** — Inter-service calls use retry (3x exponential), circuit breaker (5/30s), timeout (10s).
- **Redis outbox** — Audit events published via `LPUSH outbox:{service}` for async processing by UtilityService.
- **Swagger + JWT** — All services expose `/swagger` with Bearer token auth support and XML doc comments.

## Prerequisites

- .NET 8 SDK (8.0.403+)
- Node.js 18+
- PostgreSQL 16+
- Redis 7+
- Docker (optional, for Docker Compose)
- EF Core tools: `dotnet tool install --global dotnet-ef`

## Quick Start

### Option A: Docker Compose (recommended)

Spin up the entire platform with one command:

```bash
docker compose -f docker/docker-compose.yml up --build
```

This starts all 5 backend services, the frontend, PostgreSQL (with all 5 databases), Redis, and Seq.

- Frontend: http://localhost:5173
- Swagger: http://localhost:5001/swagger (Security), :5002 (Profile), :5003 (Work), :5200 (Utility), :5300 (Billing)
- Seq Logs: http://localhost:5341

See [docker/README.md](docker/README.md) for details.
### Option B: Run locally

#### 1. Set up environment files

```bash
./config/setup-env.sh development
```

#### 2. Create databases

```bash
createdb nexus_security
createdb nexus_profile
createdb nexus_work
createdb nexus_utility
createdb nexus_billing
```

#### 3. Start backend services

```bash
# In separate terminals:
dotnet run --project src/backend/SecurityService/SecurityService.Api
dotnet run --project src/backend/ProfileService/ProfileService.Api
dotnet run --project src/backend/WorkService/WorkService.Api
dotnet run --project src/backend/UtilityService/UtilityService.Api
dotnet run --project src/backend/BillingService/BillingService.Api
```

Each service auto-applies database migrations on startup.

#### 4. Start frontend

```bash
cd src/frontend
npm install
npm run dev
```

Open http://localhost:5173

## Project Structure

```
Nexus-2.0/
├── src/
│   ├── backend/
│   │   ├── SecurityService/         # Auth, JWT, sessions, RBAC (port 5001)
│   │   ├── ProfileService/          # Orgs, departments, members (port 5002)
│   │   ├── WorkService/             # Projects, stories, sprints (port 5003)
│   │   ├── UtilityService/          # Audit, notifications, ref data (port 5200)
│   │   ├── BillingService/          # Subscriptions, billing (port 5300)
│   │   └── migrations-guide.md      # EF Core migration commands
│   └── frontend/                    # React 18 SPA (port 5173)
│       └── src/features/            # Feature-based module organization
├── .kiro/specs/                     # Spec-driven development artifacts
│   ├── security-service/            # Requirements, design, tasks per feature
│   ├── profile-service/
│   ├── work-service/
│   ├── utility-service/
│   ├── billing-service/
│   ├── frontend-app/
│   └── billing-frontend/
├── config/                          # Environment configs
│   ├── development/                 # Local dev (localhost, relaxed limits)
│   ├── staging/                     # Staging (internal hosts, test keys)
│   └── production/                  # Production (SSL, strict limits, live keys)
├── docker/                          # Docker Compose + init scripts
├── postman/                         # API collection + environment files
├── docs/                            # Platform specs and requirements
├── .github/workflows/               # CI/CD pipelines
│   ├── ci.yml                       # Build + test on push/PR
│   └── docker.yml                   # Docker image build + push
└── Nexus-2.0.sln                    # .NET solution (25 projects)
```
## Tests

494 tests total, all passing:

| Service | Tests | Framework |
|---------|-------|-----------|
| SecurityService | 54 | xUnit + Moq |
| ProfileService | 58 | xUnit + Moq |
| WorkService | 130 | xUnit + Moq |
| UtilityService | 80 | xUnit + Moq |
| BillingService | 79 | xUnit + Moq + FsCheck |
| Frontend | 93 | Vitest + fast-check |

```bash
# Run all backend tests
dotnet test Nexus-2.0.sln

# Run frontend tests
cd src/frontend && npx vitest --run
```

## CI/CD

GitHub Actions pipelines in `.github/workflows/`:

- **ci.yml** — Triggers on push to `main` and PRs. Builds and tests each backend service in parallel (5 matrix jobs), builds and tests the frontend, then runs a full solution build check.
- **docker.yml** — Triggers on push to `main`. Builds Docker images for all 6 containers and pushes to GitHub Container Registry (ghcr.io).

## Resources

| Resource | Path |
|----------|------|
| Environment Configuration | [config/README.md](config/README.md) |
| Docker Compose | [docker/README.md](docker/README.md) |
| Postman Collection | [postman/README.md](postman/README.md) |
| Database Migrations | [src/backend/migrations-guide.md](src/backend/mig
## Resources

| Resource | Path |
|----------|------|
| Environment Configuration | [config/README.md](config/README.md) |
| Docker Compose | [docker/README.md](docker/README.md) |
| Postman Collection | [postman/README.md](postman/README.md) |
| Database Migrations | [migrations-guide.md](src/backend/migrations-guide.md) |
| Platform Specification | [platform-specification.md](docs/platform-specification.md) |
| Backend Requirements | [backend-requirements.md](docs/nexus-2.0-backend-requirements.md) |
| Backend Specification | [backend-specification.md](docs/nexus-2.0-backend-specification.md) |
