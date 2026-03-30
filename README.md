# Nexus 2.0 — Enterprise Agile Platform

A multi-tenant agile project management platform built with .NET 8 microservices and a React SPA frontend. Nexus 2.0 provides organizations with story tracking, sprint management, Kanban boards, department-based workflows, RBAC, billing with Stripe integration, and real-time notifications.

## Architecture

5 backend microservices + 1 React SPA frontend, each with its own PostgreSQL database. Services communicate via REST with service-to-service JWT authentication. Redis is used for caching, sessions, rate limiting, and feature gate enforcement.

| Service | Port | Database | Description |
|---------|------|----------|-------------|
| SecurityService | 5001 | nexus_security | Auth, JWT, sessions, RBAC, OTP, rate limiting |
| ProfileService | 5002 | nexus_profile | Organizations, departments, team members, invites |
| WorkService | 5003 | nexus_work | Projects, stories, tasks, sprints, boards |
| UtilityService | 5200 | nexus_utility | Audit logs, notifications, reference data |
| BillingService | 5300 | nexus_billing | Subscriptions, plans, feature gates, usage |
| Frontend | 5173 | — | React 18 SPA (TypeScript, Vite, Tailwind) |

## Tech Stack

- **Backend:** .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL, Redis
- **Frontend:** React 18, TypeScript, Vite, Tailwind CSS v3, Zustand, React Router v6
- **Testing:** xUnit, Moq (401 backend tests), Vitest, fast-check (93 frontend tests)
- **Infrastructure:** Polly (resilience), Serilog + Seq (logging), Stripe SDK (payments)

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- PostgreSQL (with databases: `nexus_security`, `nexus_profile`, `nexus_work`, `nexus_utility`, `nexus_billing`)
- Redis
- EF Core tools: `dotnet tool install --global dotnet-ef`

## Quick Start

### 1. Set up environment files

```bash
# Copy development env files to each service
./config/setup-env.sh development
```

Or manually copy from `config/development/` — see [config/README.md](config/README.md).

### 2. Create databases

```bash
createdb nexus_security
createdb nexus_profile
createdb nexus_work
createdb nexus_utility
createdb nexus_billing
```

### 3. Run migrations

Each service auto-applies pending migrations on startup. To run manually:

```bash
cd src/backend/SecurityService/SecurityService.Api
dotnet ef database update --project ../SecurityService.Infrastructure --context SecurityDbContext
```

See [migrations guide](src/backend/migrations-guide.md) for all services.

### 4. Start backend services

```bash
# In separate terminals:
dotnet run --project src/backend/SecurityService/SecurityService.Api
dotnet run --project src/backend/ProfileService/ProfileService.Api
dotnet run --project src/backend/WorkService/WorkService.Api
dotnet run --project src/backend/UtilityService/UtilityService.Api
dotnet run --project src/backend/BillingService/BillingService.Api
```

### 5. Start frontend

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
│   │   ├── SecurityService/     # Auth & security (port 5001)
│   │   ├── ProfileService/      # Org & member management (port 5002)
│   │   ├── WorkService/         # Projects, stories, sprints (port 5003)
│   │   ├── UtilityService/      # Audit, notifications, reference data (port 5200)
│   │   ├── BillingService/      # Subscriptions & billing (port 5300)
│   │   └── migrations-guide.md
│   └── frontend/                # React SPA (port 5173)
├── config/                      # Environment configs (dev/staging/prod)
├── postman/                     # API collection + environments
├── docs/                        # Platform specification & requirements
└── Nexus-2.0.sln               # .NET solution file
```

Each backend service follows a 4-layer architecture: `Domain` → `Application` → `Infrastructure` → `Api`.

## Resources

- [Environment Configuration](config/README.md)
- [Postman Collection](postman/README.md)
- [Database Migrations Guide](src/backend/migrations-guide.md)
- [Platform Specification](docs/platform-specification.md)
- [Backend Requirements](docs/nexus-2.0-backend-requirements.md)
- [Backend Specification](docs/nexus-2.0-backend-specification.md)

## Tests

- **Backend:** 401 tests across 5 services (xUnit + Moq)
- **Frontend:** 93 tests across 14 test files (Vitest + fast-check)

```bash
# Run all backend tests
dotnet test Nexus-2.0.sln

# Run frontend tests
cd src/frontend
npx vitest --run
```
