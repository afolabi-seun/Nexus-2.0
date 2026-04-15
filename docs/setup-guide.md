# Nexus 2.0 — Setup Guide

Step-by-step guide to get the platform running locally from a fresh clone.

---

## Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| .NET 8 SDK | 8.0.403+ | `dotnet --version` |
| Node.js | 18+ | `node --version` |
| PostgreSQL | 16+ | `psql --version` |
| Redis | 7+ | `redis-cli ping` |
| EF Core tools | latest | `dotnet ef --version` |
| python3 | 3.8+ | `python3 --version` (used by seed scripts) |

Install EF Core tools if missing:
```bash
dotnet tool install --global dotnet-ef
```

---

## Option A: Automated Setup (recommended)

```bash
# Clone the repo
git clone https://github.com/afolabi-seun/Nexus-2.0.git
cd Nexus-2.0

# Set up environment files
./config/setup-env.sh development

# Full setup: create DBs + start services + wait for health + seed data
make setup
```

This will:
1. Create 5 PostgreSQL databases (`nexus_security`, `nexus_profile`, `nexus_work`, `nexus_utility`, `nexus_billing`)
2. Start all 5 backend services in the background
3. Wait for all `/health` endpoints to respond (up to 120 seconds)
4. Run the seed scripts to populate test data

Then start the frontend:
```bash
make frontend
```

Open http://localhost:5173

### Verify Everything is Running

```bash
make status
```

Expected output:
```
  ● SecurityService (port 5001) — healthy
  ● ProfileService (port 5002) — healthy
  ● WorkService (port 5003) — healthy
  ● UtilityService (port 5200) — healthy
  ● BillingService (port 5300) — healthy
```

---

## Option B: Docker Compose

```bash
# Full stack (includes PostgreSQL, Redis, Seq, Mailpit)
docker compose -f docker/docker-compose.yml up --build

# Local dev (you have PostgreSQL + Redis installed)
docker compose -f docker/docker-compose.local.yml up --build
```

Then seed the data:
```bash
make seed
```

---

## Option C: Manual Setup

### 1. Set up environment files

```bash
./config/setup-env.sh development
```

### 2. Create databases

```bash
createdb nexus_security
createdb nexus_profile
createdb nexus_work
createdb nexus_utility
createdb nexus_billing
```

### 3. Start backend services (5 separate terminals)

```bash
dotnet run --project src/backend/SecurityService/SecurityService.Api
dotnet run --project src/backend/ProfileService/ProfileService.Api
dotnet run --project src/backend/WorkService/WorkService.Api
dotnet run --project src/backend/UtilityService/UtilityService.Api
dotnet run --project src/backend/BillingService/BillingService.Api
```

Each service auto-applies database migrations on startup.

### 4. Seed test data

```bash
./scripts/seed-all.sh
```

Or run batches individually:
```bash
./scripts/batch-1-orgs-admins.sh
./scripts/batch-2-subscriptions-members.sh
./scripts/batch-3-accept-projects.sh
./scripts/batch-4-stories-sprints.sh
./scripts/batch-5-tasks-extras.sh
```

### 5. Start frontend

```bash
cd src/frontend
npm install
npm run dev
```

Open http://localhost:5173

---

## Seed Data Overview

The seed scripts create a fully populated test environment:

### Organizations

| Organization | Prefix | Subscription | OrgAdmin |
|-------------|--------|-------------|----------|
| Acme Corp | ACME | Professional | jane.admin@acme.com |
| Globex Inc | GLX | Starter | bob.admin@globex.com |

### Team Members

**Acme Corp (6 members):**

| Email | Name | Role | Department |
|-------|------|------|------------|
| jane.admin@acme.com | Jane Admin | OrgAdmin | — |
| sarah.lead@acme.com | Sarah Lead | DeptLead | Engineering |
| mike.dev@acme.com | Mike Developer | Member | Engineering |
| lisa.qa@acme.com | Lisa Tester | Member | QA |
| tom.viewer@acme.com | Tom Viewer | Viewer | Product |
| anna.devops@acme.com | Anna DevOps | DeptLead | DevOps |
| chris.design@acme.com | Chris Designer | Member | Design |

**Globex Inc (4 members):**

| Email | Name | Role | Department |
|-------|------|------|------------|
| bob.admin@globex.com | Bob Admin | OrgAdmin | — |
| emma.lead@globex.com | Emma Lead | DeptLead | Engineering |
| james.dev@globex.com | James Developer | Member | Engineering |
| nina.qa@globex.com | Nina Tester | Member | QA |
| alex.viewer@globex.com | Alex Viewer | Viewer | Product |

### Test Credentials

| Account | Email/Username | Password |
|---------|---------------|----------|
| PlatformAdmin | admin | Platform@2025 |
| Acme OrgAdmin | jane.admin@acme.com | AcmeAdmin@2025 |
| Acme DeptLead | sarah.lead@acme.com | Sarah@2025 |
| Globex OrgAdmin | bob.admin@globex.com | GlobexAdmin@2025 |
| Globex DeptLead | emma.lead@globex.com | Emma@2025 |
| All other members | (see above) | Welcome@123 (forced change on first login) |

### Projects & Stories

**Acme Corp:**

| Project | Key | Stories | Sprint |
|---------|-----|---------|--------|
| Mobile App | MOB | 6 stories | MOB Sprint 1 (Planning, 3 stories) |
| Web Platform | WEB | 5 stories | WEB Sprint 1 (Planning, 2 stories) |
| API Gateway | API | 4 stories | No sprint |

**Globex Inc:**

| Project | Key | Stories | Sprint |
|---------|-----|---------|--------|
| Payment System | PAY | 5 stories | PAY Sprint 1 (Planning, 3 stories) |
| Admin Dashboard | ADM | 4 stories | No sprint |

### What's Left for Manual Testing

- **Stories are NOT assigned** — test assignment flows
- **Sprints are in Planning** — test start/complete flows
- **No time entries logged** — test timer and manual logging
- **Labels created but not applied** — test label management

---

## Make Commands Reference

| Command | Description |
|---------|-------------|
| `make setup` | Create DBs + start services + wait + seed (fresh machine) |
| `make start` | Start all backend services |
| `make stop` | Stop all backend services |
| `make seed` | Run seed scripts (services must be running) |
| `make reset` | Full reset: stop + drop DBs + recreate + start + seed |
| `make status` | Check service health |
| `make create-dbs` | Create databases only |
| `make test` | Run all tests (backend + frontend) |
| `make test-backend` | Run backend tests only |
| `make test-frontend` | Run frontend tests only |
| `make build` | Build entire .NET solution |
| `make frontend` | Start frontend dev server |
| `make frontend-install` | Install frontend npm dependencies |
| `make docker-up` | Start full stack via Docker Compose |
| `make docker-local` | Start local dev Docker stack |
| `make docker-down` | Stop Docker Compose |
| `make docker-reset` | Stop + wipe Docker volumes |
| `make help` | Show all commands |

---

## URLs

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| SecurityService Swagger | http://localhost:5001/swagger |
| ProfileService Swagger | http://localhost:5002/swagger |
| WorkService Swagger | http://localhost:5003/swagger |
| UtilityService Swagger | http://localhost:5200/swagger |
| BillingService Swagger | http://localhost:5300/swagger |
| Seq Logs | http://localhost:5341 |
| Mailpit (email) | http://localhost:8025 |

---

## Troubleshooting

### Services won't start
- Check PostgreSQL is running: `pg_isready`
- Check Redis is running: `redis-cli ping`
- Check ports aren't in use: `lsof -i :5001`
- Check logs: `cat scripts/.pids/SecurityService.log`

### Seed script fails
- Ensure all 5 services are healthy: `make status`
- Check if data already exists (scripts are idempotent — re-run is safe)
- Check python3 is available: `python3 --version`

### Database issues
- Full reset: `make reset` (drops and recreates all databases)
- Manual reset: `dropdb nexus_security && createdb nexus_security`
- Migrations auto-apply on service startup

### Port conflicts
- Kill process on port: `kill $(lsof -t -i :5001)`
- Or change ports in environment files: `config/development/*.env`
