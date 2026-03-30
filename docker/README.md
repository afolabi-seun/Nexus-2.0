# Docker Compose — Nexus 2.0

Spin up the entire platform with one command.

## Services

| Container | Image | Port | Description |
|-----------|-------|------|-------------|
| nexus-postgres | postgres:16-alpine | 5432 | PostgreSQL (5 databases) |
| nexus-redis | redis:7-alpine | 6379 | Redis (sessions, cache, rate limiting) |
| nexus-seq | datalust/seq | 5341 | Log aggregation UI |
| nexus-security | .NET 8 | 5001 | SecurityService |
| nexus-profile | .NET 8 | 5002 | ProfileService |
| nexus-work | .NET 8 | 5003 | WorkService |
| nexus-utility | .NET 8 | 5200 | UtilityService |
| nexus-billing | .NET 8 | 5300 | BillingService |
| nexus-frontend | nginx | 5173 | React SPA |

## Quick Start

```bash
# From the project root:
docker compose -f docker/docker-compose.yml up --build
```

Then open:
- Frontend: http://localhost:5173
- Swagger (Security): http://localhost:5001/swagger
- Swagger (Profile): http://localhost:5002/swagger
- Swagger (Work): http://localhost:5003/swagger
- Swagger (Utility): http://localhost:5200/swagger
- Swagger (Billing): http://localhost:5300/swagger
- Seq Logs: http://localhost:5341

## Stop

```bash
docker compose -f docker/docker-compose.yml down
```

## Reset (wipe data)

```bash
docker compose -f docker/docker-compose.yml down -v
```

## Notes

- PostgreSQL creates all 5 databases on first startup via `init-databases.sql`
- Each service auto-applies EF Core migrations on startup
- Seq accepts logs from all services (no API key needed in dev)
- Frontend is built as a static SPA served by nginx
- All services use Docker-internal hostnames (e.g., `postgres`, `redis`, `security-service`)
