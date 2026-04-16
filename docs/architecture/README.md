# Architecture Documentation

Internal documentation for Nexus 2.0 platform patterns and conventions.

## Error Management

| Document | Covers |
|----------|--------|
| [ERROR_HANDLING.md](ERROR_HANDLING.md) | DomainException hierarchy, GlobalExceptionHandlerMiddleware, PostgreSQL constraint mapping, error flow |
| [ERROR_CODES.md](ERROR_CODES.md) | Error code registry, per-service ranges (2xxx–6xxx), 3-tier resolution, two-digit response codes |
| [API_RESPONSES.md](API_RESPONSES.md) | ApiResponse envelope, PaginatedResponse, correlation ID flow, HTTP status code mapping |
| [VALIDATION.md](VALIDATION.md) | FluentValidation in Application layer, validation error response shape |

## Security & Access Control

| Document | Covers |
|----------|--------|
| [AUTHENTICATION_AND_SECURITY.md](AUTHENTICATION_AND_SECURITY.md) | Login flow, JWT, refresh tokens, OTP, sessions, lockout, anomaly detection, service-to-service auth |
| [AUTHORIZATION_RBAC.md](AUTHORIZATION_RBAC.md) | Role hierarchy, RoleAuthorizationMiddleware, department scoping, organization scoping |

## Architecture & Communication

| Document | Covers |
|----------|--------|
| [INTER_SERVICE_COMMUNICATION.md](INTER_SERVICE_COMMUNICATION.md) | Service clients, Polly resilience, correlation ID propagation, error propagation, Redis outbox |
| [CODE_STRUCTURE.md](CODE_STRUCTURE.md) | Clean Architecture layers, GenericRepository, folder conventions, FlgStatus pattern, DI registration |
