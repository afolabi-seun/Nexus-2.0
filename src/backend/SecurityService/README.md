# SecurityService

Authentication and authorization microservice for the Nexus 2.0 platform.

- **Port:** 5001
- **Database:** `nexus_security` (PostgreSQL)
- **Base path:** `/api/v1`

## Responsibilities

- **Authentication** — Email/password login with credential validation
- **JWT management** — Access tokens (15 min) + refresh tokens (7 days) with rotation; reuse detection triggers full session revocation
- **Session management** — Per-device sessions stored in Redis; list, revoke individual, or revoke all
- **RBAC** — Role-based access: PlatformAdmin, OrgAdmin, DeptLead, Member, Viewer
- **OTP** — 6-digit one-time passwords with configurable expiry and max attempts
- **Password management** — Forced change for first-time users, reset via OTP flow
- **Rate limiting** — Sliding window via Redis (login: 5/15min, OTP: 3/5min)
- **Account lockout** — Auto-lock after 10 failed attempts within 24 hours
- **Token blacklisting** — Revoked JWTs tracked in Redis until natural expiry
- **Anomaly detection** — IP-based tracking on login attempts
- **Service-to-service auth** — Issue and validate service tokens for inter-service calls

## API Endpoints

### Auth (`/api/v1/auth`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/login` | Public | Login with email/password |
| POST | `/auth/logout` | Bearer | Logout current session |
| POST | `/auth/refresh` | Public | Refresh access token (rotates refresh token) |
| POST | `/auth/otp/request` | Public | Request OTP for identity |
| POST | `/auth/otp/verify` | Public | Verify OTP code |
| POST | `/auth/credentials/generate` | Service | Generate credentials for new member |

### Password (`/api/v1/password`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/password/forced-change` | Bearer | Change password (first-time users) |
| POST | `/password/reset/request` | Public | Request password reset OTP |
| POST | `/password/reset/confirm` | Public | Confirm reset with OTP + new password |

### Sessions (`/api/v1/sessions`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/sessions` | Bearer | List active sessions (paginated) |
| DELETE | `/sessions/{sessionId}` | Bearer | Revoke a specific session |
| DELETE | `/sessions/all` | Bearer | Revoke all sessions except current |

### Service Tokens (`/api/v1/service-tokens`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/service-tokens/issue` | Service | Issue a service-to-service token |

## Project Structure

```
SecurityService/
├── SecurityService.Domain/
│   ├── Entities/              # Domain entities
│   ├── Enums/                 # Domain enums
│   ├── Exceptions/            # Domain exceptions (ErrorCodes.cs + specific exceptions)
│   ├── Helpers/               # Constants and helpers
│   ├── Interfaces/
│   │   ├── Repositories/      # Repository interfaces
│   │   └── Services/          # Service interfaces
│   └── Common/                # Shared interfaces (IOrganizationEntity)
├── SecurityService.Application/
│   ├── DTOs/                  # Request/response DTOs organized by feature
│   ├── Contracts/             # Inter-service contracts
│   └── Validators/            # FluentValidation validators
├── SecurityService.Infrastructure/
│   ├── Configuration/         # AppSettings, DI, DatabaseMigrationHelper
│   ├── Data/                  # EF Core DbContext
│   ├── Repositories/
│   │   └── PasswordHistory/   # Entity-named subfolders
│   └── Services/
│       ├── Auth/              # Each service in its own subfolder
│       ├── Jwt/
│       ├── Session/
│       ├── Otp/
│       ├── Password/
│       ├── AnomalyDetection/
│       ├── RateLimiter/
│       ├── ServiceToken/
│       ├── Outbox/
│       ├── ErrorCodeResolver/
│       └── ServiceClients/
├── SecurityService.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Attributes/
│   ├── Extensions/
│   └── Program.cs
└── SecurityService.Tests/
    ├── Unit/
    └── Property/
```

## Architecture Conventions

- **Clean Architecture** — 4 layers: Domain → Application → Infrastructure → Api. Dependencies flow inward only.
- **Entity-named subfolders** — Repositories and Services are organized into subfolders named after the entity they manage (e.g., `Repositories/PasswordHistory/PasswordHistoryRepository.cs`). Namespaces match folder paths.
- **ApiResponse envelope** — All API responses wrapped in `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, and `CorrelationId`.
- **DomainException pattern** — Business rule violations throw typed exceptions (e.g., `InvalidCredentialsException`) with error codes, HTTP status codes, and correlation IDs. Caught by `GlobalExceptionHandlerMiddleware`.
- **Middleware pipeline** — CORS → CorrelationId → GlobalExceptionHandler → Serilog → RateLimiter → Routing → Auth → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
- **Polly resilience** — Inter-service HTTP calls use retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s).
- **Redis outbox** — Audit events published via `LPUSH outbox:{service}` for async processing by UtilityService.

## How to Run

```bash
cd src/backend/SecurityService/SecurityService.Api
cp .env.example .env   # Edit with your values
dotnet run
```

Service starts at `http://localhost:5001`. Swagger UI at `/swagger`.

## Environment Variables

See [`.env.example`](SecurityService.Api/.env.example) for all variables. Key settings:

| Variable | Description |
|----------|-------------|
| `DATABASE_CONNECTION_STRING` | PostgreSQL connection string |
| `REDIS_CONNECTION_STRING` | Redis host:port |
| `JWT_SECRET_KEY` | HMAC signing key (min 32 chars) |
| `ACCESS_TOKEN_EXPIRY_MINUTES` | Access token TTL (default: 15) |
| `REFRESH_TOKEN_EXPIRY_DAYS` | Refresh token TTL (default: 7) |
| `LOGIN_RATE_LIMIT_MAX` | Max login attempts per window |
| `ACCOUNT_LOCKOUT_MAX_ATTEMPTS` | Failed attempts before lockout |
| `OTP_EXPIRY_MINUTES` | OTP validity period |
| `SERVICE_SECRET` | Shared secret for service-to-service auth |

## Key Design Decisions

- **JWT rotation** — Refresh tokens are single-use. Reuse of a rotated token revokes all sessions for that user (replay attack protection).
- **Redis session store** — Sessions are stored in Redis for fast lookup and automatic TTL-based expiry.
- **Polly resilience** — HTTP calls to ProfileService and UtilityService use retry + circuit breaker policies.
- **BCrypt password hashing** — Passwords hashed with BCrypt; password history prevents reuse.
- **Correlation IDs** — Every request gets a correlation ID propagated across service calls for distributed tracing.
