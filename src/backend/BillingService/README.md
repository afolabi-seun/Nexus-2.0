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

## How to Run

```bash
cd src/backend/BillingService/BillingService.Api
cp .env.example .env   # Edit with your values
dotnet run
```

Service starts at `http://localhost:5300`. Swagger UI at `/swagger`.

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
| `SERVICE_SECRET` | Shared secret for service-to-service auth |
