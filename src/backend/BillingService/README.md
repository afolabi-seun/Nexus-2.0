# BillingService

Subscription management and billing microservice for the Nexus 2.0 platform.

- **Port:** 5300
- **Database:** `nexus_billing` (PostgreSQL)
- **Base path:** `/api/v1`

## Responsibilities

- **Plan tier management** — 4 tiers: Free, Starter, Professional, Enterprise
- **Subscription lifecycle** — Create, upgrade, downgrade, cancel
- **14-day trial management** — Paid plans start with a trial; upgrade during trial ends it immediately
- **Feature gating** — Cross-service limit enforcement via Redis-cached plan limits
- **Usage tracking** — Active members, stories created, storage consumed
- **Stripe integration** — Payment processing via Stripe SDK, webhook handling with signature verification, idempotent event processing
- **Outbox-based audit events** — Subscription changes published via outbox pattern

### Background Hosted Services

1. **Trial Expiry** — Monitors and expires trials that exceed the 14-day window
2. **Usage Persistence** — Periodically flushes in-memory usage counters to the database

## Plan Tiers

| Plan | Price | Members | Projects | Stories | Storage |
|------|-------|---------|----------|---------|---------|
| Free | $0/mo | 5 | 2 | 100 | 500 MB |
| Starter | $29/mo | 25 | 10 | 1,000 | 5 GB |
| Professional | $79/mo | 100 | 50 | 10,000 | 50 GB |
| Enterprise | $199/mo | Unlimited | Unlimited | Unlimited | 500 GB |

## API Endpoints

### Plans (`/api/v1/plans`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/plans` | Bearer | List all active plans |

### Subscriptions (`/api/v1/subscriptions`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/subscriptions/current` | OrgAdmin | Get current subscription |
| POST | `/subscriptions` | OrgAdmin | Create subscription |
| PATCH | `/subscriptions/upgrade` | OrgAdmin | Upgrade to higher tier |
| PATCH | `/subscriptions/downgrade` | OrgAdmin | Schedule downgrade at period end |
| POST | `/subscriptions/cancel` | OrgAdmin | Cancel subscription |

### Usage (`/api/v1/usage`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/usage` | OrgAdmin | Get current usage metrics |
| POST | `/usage/increment` | Service | Increment usage counter |

### Feature Gates (`/api/v1/feature-gates`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/feature-gates/{feature}` | Service | Check feature availability for org |

### Stripe Webhooks (`/api/v1/webhooks/stripe`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/webhooks/stripe` | Public | Handle Stripe webhook events |

### Admin Billing (`/api/v1/admin/billing`) — PlatformAdmin only

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/admin/billing/subscriptions` | PlatformAdmin | List all subscriptions |
| GET | `/admin/billing/organizations/{orgId}` | PlatformAdmin | Get org billing details |
| POST | `/admin/billing/organizations/{orgId}/override` | PlatformAdmin | Override subscription |
| POST | `/admin/billing/organizations/{orgId}/cancel` | PlatformAdmin | Cancel org subscription |
| GET | `/admin/billing/usage/summary` | PlatformAdmin | Platform-wide usage summary |
| GET | `/admin/billing/usage/organizations` | PlatformAdmin | Per-org usage list |

### Admin Plans (`/api/v1/admin/billing/plans`) — PlatformAdmin only

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/admin/billing/plans` | PlatformAdmin | List all plans (including inactive) |
| POST | `/admin/billing/plans` | PlatformAdmin | Create plan |
| PUT | `/admin/billing/plans/{planId}` | PlatformAdmin | Update plan |
| PATCH | `/admin/billing/plans/{planId}/deactivate` | PlatformAdmin | Deactivate plan |

## Project Structure

```
BillingService/
├── BillingService.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   └── Common/
├── BillingService.Application/
│   ├── DTOs/
│   ├── Contracts/
│   └── Validators/
├── BillingService.Infrastructure/
│   ├── Configuration/
│   ├── Data/
│   ├── Repositories/
│   │   ├── Subscriptions/
│   │   ├── Plans/
│   │   ├── UsageRecords/
│   │   └── StripeEvents/
│   └── Services/
│       ├── Subscriptions/
│       ├── Plans/
│       ├── FeatureGates/
│       ├── Usage/
│       ├── Stripe/
│       ├── Outbox/
│       ├── ErrorCodeResolver/
│       ├── ServiceClients/
│       └── BackgroundServices/
├── BillingService.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Attributes/
│   └── Extensions/
└── BillingService.Tests/
    ├── Unit/
    └── Property/
```

## Architecture Conventions

- **Clean Architecture** — 4 layers: Domain → Application → Infrastructure → Api. Dependencies flow inward only.
- **Entity-named subfolders** — Repositories and Services are organized into subfolders named after the entity they manage (e.g., `Repositories/Subscriptions/SubscriptionRepository.cs`). Namespaces match folder paths.
- **ApiResponse envelope** — All API responses wrapped in `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, and `CorrelationId`.
- **DomainException pattern** — Business rule violations throw typed exceptions (e.g., `PlanNotFoundException`) with error codes, HTTP status codes, and correlation IDs. Caught by `GlobalExceptionHandlerMiddleware`.
- **Middleware pipeline** — CORS → CorrelationId → GlobalExceptionHandler → Serilog → RateLimiter → Routing → Auth → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
- **Polly resilience** — Inter-service HTTP calls use retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s).
- **Redis outbox** — Audit events published via `LPUSH outbox:{service}` for async processing by UtilityService.

## How to Run

```bash
# Automated (from project root)
make setup    # Creates DBs, starts all services, seeds test data
make start    # Start services only
make status   # Check health

# Manual
cd src/backend/BillingService/BillingService.Api
dotnet run
```

Service starts at `http://localhost:5300`. Swagger UI at `/swagger`.
Health checks at `/health` and `/ready` (checks PostgreSQL + Redis).

## Seed Data

The seed scripts (`make seed`) create subscriptions (Professional for Acme, Starter for Globex).
See [docs/setup-guide.md](../../docs/setup-guide.md) for full seed data details.

## Environment Variables

See [`.env.example`](BillingService.Api/.env.example) for all variables. Key settings:

| Variable | Description |
|----------|-------------|
| `DATABASE_CONNECTION_STRING` | PostgreSQL connection string |
| `REDIS_CONNECTION_STRING` | Redis host:port |
| `JWT_SECRET_KEY` | Shared JWT signing key |
| `STRIPE_SECRET_KEY` | Stripe API secret key |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret |
| `SECURITY_SERVICE_BASE_URL` | SecurityService URL |
| `PROFILE_SERVICE_BASE_URL` | ProfileService URL |
| `UTILITY_SERVICE_BASE_URL` | UtilityService URL (error code resolution) |
| `SERVICE_SECRET` | Shared secret for service-to-service auth |
