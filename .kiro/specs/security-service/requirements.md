# Requirements Document — SecurityService

## Introduction

This document defines the complete requirements for the Nexus-2.0 SecurityService — the authentication, authorization, and security microservice of the Enterprise Agile Platform. SecurityService runs on port 5001 with database `nexus_security` and follows Clean Architecture (.NET 8) with Domain / Application / Infrastructure / Api layers.

SecurityService does NOT own user records. ProfileService is the source of truth for TeamMember data. SecurityService resolves user identity by calling ProfileService via service-to-service JWT, with a 15-minute Redis cache (`user_cache:{userId}`).

All requirements are derived from the platform documentation:
- `docs/nexus-2.0-backend-requirements.md` (REQ-001 – REQ-020)
- `docs/nexus-2.0-backend-specification.md` (SecurityService specification, sections 4.1–4.15)
- `docs/platform-specification.md` (predecessor WEP patterns)

## Glossary

- **SecurityService**: Microservice (port 5001, database `nexus_security`) responsible for authentication, JWT issuance, session management, department-aware RBAC, OTP verification, rate limiting, anomaly detection, password management, and service-to-service auth.
- **ProfileService**: Microservice (port 5002) that owns TeamMember records. SecurityService calls ProfileService to resolve user identity.
- **TeamMember**: A user within an organization, assigned to one or more departments with department-scoped roles. Equivalent to "SmeUser" in the predecessor WEP platform.
- **Organization**: Top-level tenant entity. All data is scoped to an organization via `OrganizationId`.
- **Department**: Functional unit within an organization (e.g., Engineering, QA, DevOps, Product, Design).
- **Role**: Department-scoped permission level — OrgAdmin (100), DeptLead (75), Member (50), Viewer (25).
- **JWT**: JSON Web Token used for Bearer authentication. Contains department-aware claims.
- **Access_Token**: Short-lived JWT (default 15 minutes) containing user identity and authorization claims.
- **Refresh_Token**: Long-lived token (default 7 days) used to obtain new access/refresh token pairs. BCrypt-hashed before Redis storage.
- **Session**: Redis-backed record of an active user login, keyed by `session:{userId}:{deviceId}`.
- **Token_Blacklist**: Redis set of revoked JWT `jti` values, keyed by `blacklist:{jti}` with TTL equal to remaining token lifetime.
- **OTP**: One-Time Password — 6-digit numeric code with 5-minute TTL, stored in Redis at `otp:{identity}`.
- **Lockout**: Account lockout mechanism tracking failed login attempts in Redis (`lockout:{identity}` counter, `lockout:locked:{identity}` flag).
- **Password_History**: PostgreSQL table tracking the last 5 BCrypt-hashed passwords per user to prevent reuse.
- **Service_Token**: Short-lived JWT for inter-service communication, cached in Redis at `service_token:{serviceId}` with 23-hour TTL.
- **Outbox**: Redis-based async messaging pattern. SecurityService publishes audit events to `outbox:security`. UtilityService polls and processes the queue.
- **Rate_Limiter**: Sliding window algorithm implemented via Redis Lua script, keyed by `rate:{identity}:{endpoint}`.
- **Anomaly_Detector**: Component that maintains trusted IP sets per user (`trusted_ips:{userId}`, 90-day TTL) and flags geo-location anomalies.
- **FlgStatus**: Soft-delete lifecycle field — `A` (Active), `S` (Suspended), `D` (Deactivated).
- **ApiResponse**: Standardized JSON envelope `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, `Errors` fields.
- **DomainException**: Base exception class for business rule violations, containing `ErrorValue`, `ErrorCode`, `StatusCode`, and `CorrelationId`.
- **CorrelationId**: End-to-end trace identifier (`X-Correlation-Id` header) propagated across all service calls.
- **Polly**: .NET resilience library used for retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s) on inter-service calls.
- **BCrypt**: Password hashing algorithm used for all password and refresh token storage.
- **Middleware_Pipeline**: Ordered chain of ASP.NET Core middleware that processes every request through SecurityService.
- **IOrganizationEntity**: Marker interface for entities scoped to an organization, enabling EF Core global query filters.
- **Clean_Architecture**: Four-layer architecture — Domain (entities, interfaces), Application (DTOs, validators), Infrastructure (EF Core, Redis, HTTP clients), Api (controllers, middleware).

## Requirements

### Requirement 1: Team Member Login (REQ-001)

**User Story:** As a team member, I want to log in with my email and password so that I can access the platform.

#### Acceptance Criteria

1. WHEN a team member submits valid email and password to `POST /api/v1/auth/login`, THE SecurityService SHALL return HTTP 200 with `accessToken`, `refreshToken`, `expiresIn`, and `isFirstTimeUser` flag.
2. WHEN the email does not match any team member, THE SecurityService SHALL return HTTP 401 with error code `INVALID_CREDENTIALS` (2001).
3. WHEN the password does not match the stored BCrypt hash, THE SecurityService SHALL increment the lockout counter in Redis (`lockout:{email}`) and return HTTP 401 with `INVALID_CREDENTIALS` (2001).
4. WHEN login succeeds, THE SecurityService SHALL reset the lockout counter, create a session in Redis (`session:{userId}:{deviceId}`), store the BCrypt-hashed refresh token (`refresh:{userId}:{deviceId}`), and publish an audit event to `outbox:security`.
5. WHEN the user record is not in Redis cache (`user_cache:{userId}`), THE SecurityService SHALL call ProfileService `GET /api/v1/team-members/by-email/{email}` via service-to-service JWT and cache the result for 15 minutes.
6. WHILE the account is locked (`lockout:locked:{email}` exists in Redis), THE SecurityService SHALL return HTTP 423 with `ACCOUNT_LOCKED` (2002) without checking credentials.
7. WHILE the account has `FlgStatus` of `S` or `D`, THE SecurityService SHALL return HTTP 403 with `ACCOUNT_INACTIVE` (2003).

### Requirement 2: JWT Token Structure — Department-Aware Claims (REQ-002)

**User Story:** As the platform, I want JWT tokens to contain organization, department, and role claims so that downstream services can enforce department-scoped authorization.

#### Acceptance Criteria

1. WHEN a JWT access token is issued, THE SecurityService SHALL include claims: `userId` (Guid), `organizationId` (Guid), `departmentId` (Guid — primary department), `roleName` (string: OrgAdmin/DeptLead/Member/Viewer), `departmentRole` (string), `deviceId` (string), and `jti` (unique token ID).
2. WHEN the access token is issued, THE SecurityService SHALL set its TTL to the value configured via `ACCESS_TOKEN_EXPIRY_MINUTES` (default 15 minutes).
3. WHEN a refresh token is issued, THE SecurityService SHALL BCrypt-hash the token before storage in Redis with a TTL configured via `REFRESH_TOKEN_EXPIRY_DAYS` (default 7 days).
4. THE SecurityService SHALL generate a unique `jti` claim for every access token to support individual token revocation via the blacklist.

### Requirement 3: Token Refresh and Rotation (REQ-003)

**User Story:** As a team member, I want my session to persist seamlessly so that I don't have to re-login frequently.

#### Acceptance Criteria

1. WHEN a valid refresh token is submitted to `POST /api/v1/auth/refresh`, THE SecurityService SHALL invalidate the old refresh token, issue a new access/refresh token pair, and return HTTP 200.
2. WHEN a previously-used (already-rotated) refresh token is submitted, THE SecurityService SHALL detect reuse, revoke ALL sessions for the user, and return HTTP 401 with `REFRESH_TOKEN_REUSE` (2013).
3. WHEN the refresh token has expired, THE SecurityService SHALL return HTTP 401 with `SESSION_EXPIRED` (2024).
4. WHEN a new token pair is issued via refresh, THE SecurityService SHALL store the new BCrypt-hashed refresh token in Redis at `refresh:{userId}:{deviceId}` and update the session at `session:{userId}:{deviceId}`.

### Requirement 4: Logout (REQ-004)

**User Story:** As a team member, I want to log out so that my session is invalidated.

#### Acceptance Criteria

1. WHEN a team member calls `POST /api/v1/auth/logout` with a valid Bearer token, THE SecurityService SHALL remove the session from Redis (`session:{userId}:{deviceId}`), add the JWT's `jti` to the token blacklist (`blacklist:{jti}`) with TTL equal to the remaining token lifetime, and return HTTP 200.
2. WHEN logout succeeds, THE SecurityService SHALL remove the refresh token from Redis (`refresh:{userId}:{deviceId}`).
3. WHEN logout succeeds, THE SecurityService SHALL publish an audit event to `outbox:security`.

### Requirement 5: First Login Forced Password Reset (REQ-005)

**User Story:** As a new team member, I want to be forced to change my temporary password on first login so that my account is secured.

#### Acceptance Criteria

1. WHILE a team member has `IsFirstTimeUser=true`, WHEN the team member attempts to access any endpoint other than `POST /api/v1/password/forced-change`, THE SecurityService SHALL return HTTP 403 with `FIRST_TIME_USER_RESTRICTED` (2006) via `FirstTimeUserMiddleware`.
2. WHEN the team member submits a new password to `POST /api/v1/password/forced-change`, THE SecurityService SHALL validate password complexity, store the BCrypt hash, set `IsFirstTimeUser=false` via ProfileService, record the old password in `password_history`, and return HTTP 200.
3. WHEN the new password matches the temporary password, THE SecurityService SHALL return HTTP 400 with `PASSWORD_REUSE_NOT_ALLOWED` (2004).
4. WHEN the forced password change succeeds, THE SecurityService SHALL publish an audit event to `outbox:security`.

### Requirement 6: Credential Generation for Invited Members (REQ-006)

**User Story:** As the platform, I want to generate initial credentials for invited team members so that they can log in for the first time.

#### Acceptance Criteria

1. WHEN ProfileService calls `POST /api/v1/auth/credentials/generate` with `{memberId, email}` via service-to-service JWT, THE SecurityService SHALL generate a temporary password, BCrypt-hash it, store it, set `IsFirstTimeUser=true`, publish a credential notification to `outbox:security`, and return HTTP 200.
2. WHEN the endpoint is called without a valid service-to-service JWT, THE SecurityService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED` (2016).
3. WHEN credentials are generated for a memberId that already has credentials, THE SecurityService SHALL overwrite the existing credentials and reset `IsFirstTimeUser=true`.

### Requirement 7: Department-Based RBAC (REQ-007)

**User Story:** As an organization, I want role-based access control scoped to departments so that team members only have permissions relevant to their department context.

#### Acceptance Criteria

1. WHEN a request reaches `RoleAuthorizationMiddleware`, THE SecurityService SHALL extract `roleName` and `departmentId` from JWT claims and compare against endpoint-level role requirements.
2. WHEN an OrgAdmin makes any request, THE SecurityService SHALL grant access regardless of department (organization-wide access).
3. WHILE a user has the DeptLead role, WHEN the user makes a department-scoped request, THE SecurityService SHALL grant access only if the user belongs to the target department.
4. WHEN a Member attempts to assign a task to another member, THE SecurityService SHALL return HTTP 403 with `INSUFFICIENT_PERMISSIONS` (2011).
5. WHEN a Viewer attempts to create or modify any entity, THE SecurityService SHALL return HTTP 403 with `INSUFFICIENT_PERMISSIONS` (2011).
6. WHEN a user attempts a department-scoped operation on a department they do not belong to, THE SecurityService SHALL return HTTP 403 with `DEPARTMENT_ACCESS_DENIED` (2020).
7. THE SecurityService SHALL enforce the following department access matrix:

| Operation | OrgAdmin | DeptLead | Member | Viewer |
|-----------|----------|----------|--------|--------|
| Create story | Allowed | Allowed | Allowed | Denied |
| Assign story to any dept | Allowed | Denied | Denied | Denied |
| Assign story within dept | Allowed | Allowed | Denied | Denied |
| Create task | Allowed | Allowed | Allowed | Denied |
| Assign task to any dept | Allowed | Denied | Denied | Denied |
| Assign task within dept | Allowed | Allowed | Denied | Denied |
| Self-assign task | Allowed | Allowed | Allowed | Denied |
| Manage sprint | Allowed | Allowed | Denied | Denied |
| View board | Allowed | Allowed | Allowed | Allowed |
| Manage organization | Allowed | Denied | Denied | Denied |
| Manage department | Allowed | Allowed (own) | Denied | Denied |
| Invite members | Allowed | Allowed (own dept) | Denied | Denied |

### Requirement 8: Session Management — Multi-Device, Redis-Backed (REQ-008)

**User Story:** As a team member, I want to manage my active sessions across multiple devices so that I can control where I'm logged in.

#### Acceptance Criteria

1. WHEN a team member calls `GET /api/v1/sessions`, THE SecurityService SHALL return all active sessions for the user, each with device info, IP address, and creation timestamp.
2. WHEN a team member calls `DELETE /api/v1/sessions/{sessionId}`, THE SecurityService SHALL remove that session from Redis and blacklist the corresponding JWT's `jti` with TTL equal to the remaining token lifetime.
3. WHEN a team member calls `DELETE /api/v1/sessions/all`, THE SecurityService SHALL revoke all sessions except the current one.
4. WHEN a session is revoked, THE SecurityService SHALL add the JWT's `jti` to `blacklist:{jti}` and remove the refresh token from `refresh:{userId}:{deviceId}`.
5. WHEN a session is revoked, THE SecurityService SHALL publish an audit event to `outbox:security`.

### Requirement 9: OTP Verification (REQ-009)

**User Story:** As a team member, I want OTP-based verification for sensitive operations so that my account is protected.

#### Acceptance Criteria

1. WHEN `POST /api/v1/auth/otp/request` is called with a valid identity, THE SecurityService SHALL generate a 6-digit numeric code, store it in Redis (`otp:{identity}`) with 5-minute TTL and attempt counter initialized to 0, and dispatch the code via notification to `outbox:security`.
2. WHEN `POST /api/v1/auth/otp/verify` is called with the correct code within the TTL, THE SecurityService SHALL return HTTP 200 with verification success and delete the OTP from Redis.
3. WHEN the wrong code is submitted to `POST /api/v1/auth/otp/verify`, THE SecurityService SHALL increment the attempt counter in Redis.
4. IF the attempt counter reaches 3 failed attempts, THEN THE SecurityService SHALL return HTTP 429 with `OTP_MAX_ATTEMPTS` (2009) and delete the OTP from Redis.
5. WHEN the OTP has expired (past 5-minute TTL), THE SecurityService SHALL return HTTP 400 with `OTP_EXPIRED` (2007).
6. WHEN OTP requests exceed 3 per 5-minute window for the same identity, THE SecurityService SHALL return HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010).

### Requirement 10: Account Lockout (REQ-010)

**User Story:** As the platform, I want to lock accounts after repeated failed login attempts so that brute-force attacks are mitigated.

#### Acceptance Criteria

1. WHEN a user fails login 10 times (configurable via `ACCOUNT_LOCKOUT_MAX_ATTEMPTS`) within 24 hours (configurable via `ACCOUNT_LOCKOUT_WINDOW_HOURS`), THE SecurityService SHALL set `lockout:locked:{identity}` in Redis with 60-minute TTL (configurable via `ACCOUNT_LOCKOUT_DURATION_MINUTES`) and publish an audit event to `outbox:security`.
2. WHILE a user is locked (`lockout:locked:{identity}` exists in Redis), WHEN the user attempts to log in, THE SecurityService SHALL return HTTP 423 with `ACCOUNT_LOCKED` (2002) without checking credentials.
3. WHEN the lockout duration expires (Redis key TTL expires), THE SecurityService SHALL allow the user to attempt login again.
4. WHEN login succeeds, THE SecurityService SHALL reset the lockout counter by deleting `lockout:{identity}` from Redis.

### Requirement 11: Password Management (REQ-011)

**User Story:** As a team member, I want secure password management with complexity enforcement and history tracking so that my account remains protected.

#### Acceptance Criteria

1. WHEN a password is set or changed, THE SecurityService SHALL validate complexity: minimum 8 characters, at least 1 uppercase letter, 1 lowercase letter, 1 digit, and 1 special character from the set `!@#$%^&*`.
2. WHEN a password fails complexity validation, THE SecurityService SHALL return HTTP 400 with `PASSWORD_COMPLEXITY_FAILED` (2018).
3. WHEN a new password matches any of the last 5 passwords in `password_history`, THE SecurityService SHALL return HTTP 400 with `PASSWORD_RECENTLY_USED` (2005).
4. WHEN `POST /api/v1/password/reset/request` is called with a valid email, THE SecurityService SHALL send an OTP to the registered email via `outbox:security`.
5. WHEN `POST /api/v1/password/reset/confirm` is called with a valid OTP and new password, THE SecurityService SHALL update the password hash, record the old hash in `password_history`, and return HTTP 200.
6. THE SecurityService SHALL store all password hashes using BCrypt with a work factor sufficient for production use.
7. WHEN a password is changed (via forced-change or reset), THE SecurityService SHALL record the old password hash in the `password_history` table with the `UserId` and `DateCreated` timestamp.

### Requirement 12: Rate Limiting (REQ-012)

**User Story:** As the platform, I want sliding-window rate limiting so that the system is protected from abuse.

#### Acceptance Criteria

1. WHEN login attempts exceed 5 per 15-minute window (per IP), THE SecurityService SHALL return HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010) and a `Retry-After` header indicating seconds until the window resets.
2. WHEN OTP requests exceed 3 per 5-minute window (per IP), THE SecurityService SHALL return HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010) and a `Retry-After` header.
3. WHEN authenticated requests exceed the configurable per-user limit, THE SecurityService SHALL return HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010) and a `Retry-After` header.
4. THE SecurityService SHALL implement rate limiting using a sliding window algorithm via Redis Lua script with key pattern `rate:{identity}:{endpoint}`.
5. THE SecurityService SHALL apply rate limiting via `RateLimiterMiddleware` for unauthenticated endpoints and `AuthenticatedRateLimiterMiddleware` for authenticated endpoints.

### Requirement 13: Service-to-Service JWT Authentication (REQ-013)

**User Story:** As a backend service, I want to authenticate with other services using short-lived JWTs so that inter-service communication is secure.

#### Acceptance Criteria

1. WHEN a service calls `POST /api/v1/service-tokens/issue` with valid service credentials, THE SecurityService SHALL issue a short-lived JWT with `serviceId` and `serviceName` claims (no `organizationId`), cache it in Redis (`service_token:{serviceId}`, 23-hour TTL), and return HTTP 200.
2. WHEN a downstream service receives a request with a service JWT, THE SecurityService SHALL validate the token using the shared secret and check the service ACL.
3. WHEN the calling service is not in the ACL, THE SecurityService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED` (2016).
4. WHEN the cached service token is within 30 seconds of expiry, THE SecurityService SHALL automatically refresh it before making the inter-service call.
5. WHEN the `POST /api/v1/service-tokens/issue` endpoint is called without valid service credentials, THE SecurityService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED` (2016).

### Requirement 14: Anomaly Detection (REQ-014)

**User Story:** As the platform, I want to detect suspicious login activity so that compromised accounts are flagged.

#### Acceptance Criteria

1. WHEN a user logs in from a new IP address, THE SecurityService SHALL check the IP against the trusted set (`trusted_ips:{userId}`, 90-day TTL).
2. WHEN the new IP's geo-location differs significantly from trusted IPs, THE SecurityService SHALL flag the login as suspicious, publish an audit event to `outbox:security`, and return HTTP 403 with `SUSPICIOUS_LOGIN` (2017).
3. WHEN a login succeeds from a known IP, THE SecurityService SHALL refresh the trusted IP set TTL to 90 days.
4. WHEN a login succeeds from a new IP that passes geo-location checks, THE SecurityService SHALL add the IP to the trusted set.

### Requirement 15: Token Blacklist Enforcement (REQ-015)

**User Story:** As the platform, I want revoked tokens to be immediately rejected so that logged-out sessions cannot be reused.

#### Acceptance Criteria

1. WHEN any request arrives with a Bearer token, THE SecurityService SHALL check `blacklist:{jti}` in Redis via `TokenBlacklistMiddleware`.
2. WHEN the `jti` exists in the blacklist, THE SecurityService SHALL return HTTP 401 with `TOKEN_REVOKED` (2012).
3. WHEN the `jti` is not blacklisted, THE SecurityService SHALL allow the request to proceed through the middleware pipeline.
4. WHEN a token is blacklisted, THE SecurityService SHALL set the Redis key TTL to the remaining lifetime of the access token so that expired blacklist entries are automatically cleaned up.

### Requirement 16: Organization Scope Enforcement (REQ-016)

**User Story:** As the platform, I want all requests to be scoped to the authenticated user's organization so that cross-organization data access is prevented.

#### Acceptance Criteria

1. WHEN any authenticated request is processed, THE SecurityService SHALL extract `organizationId` from JWT claims via `OrganizationScopeMiddleware` and validate it against route/query parameters.
2. WHEN a request attempts to access data from a different organization, THE SecurityService SHALL return HTTP 403 with `ORGANIZATION_MISMATCH` (2019).
3. WHEN inter-service calls are made, THE SecurityService SHALL propagate the `X-Organization-Id` header.
4. THE SecurityService SHALL apply EF Core global query filters by `OrganizationId` on all entities implementing `IOrganizationEntity`.

### Requirement 17: Middleware Pipeline Order (REQ-017)

**User Story:** As the platform, I want a well-defined middleware pipeline so that security concerns are enforced in the correct order.

#### Acceptance Criteria

1. WHEN a request enters SecurityService, THE SecurityService SHALL execute middleware in this exact order: CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → FirstTimeUserGuard → RoleAuthorization → OrganizationScope → Controllers.
2. WHEN `GlobalExceptionHandlerMiddleware` catches a `DomainException`, THE SecurityService SHALL return the appropriate HTTP status code with `application/problem+json` content type and the `ApiResponse<T>` envelope including `ErrorCode`, `ErrorValue`, and `CorrelationId`.
3. WHEN `GlobalExceptionHandlerMiddleware` catches an unhandled exception, THE SecurityService SHALL return HTTP 500 with a generic error message and publish an error event to `outbox:security`.
4. THE SecurityService SHALL generate or propagate a `CorrelationId` (`X-Correlation-Id` header) on every request via `CorrelationIdMiddleware` and include it in all API responses.

### Requirement 18: SecurityService API Endpoints (REQ-018)

**User Story:** As a developer, I want a complete set of security endpoints so that all authentication and authorization flows are supported.

#### Acceptance Criteria

1. THE SecurityService SHALL expose the following endpoints:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/auth/login` | None | Team member login |
| POST | `/api/v1/auth/logout` | Bearer | Invalidate session |
| POST | `/api/v1/auth/refresh` | None | Rotate refresh token |
| POST | `/api/v1/auth/otp/request` | None | Request OTP |
| POST | `/api/v1/auth/otp/verify` | None | Verify OTP |
| POST | `/api/v1/auth/credentials/generate` | Service | Generate initial credentials |
| POST | `/api/v1/password/forced-change` | Bearer | First-time password change |
| POST | `/api/v1/password/reset/request` | None | Request password reset |
| POST | `/api/v1/password/reset/confirm` | None | Confirm password reset |
| GET | `/api/v1/sessions` | Bearer | List active sessions |
| DELETE | `/api/v1/sessions/{sessionId}` | Bearer | Revoke specific session |
| DELETE | `/api/v1/sessions/all` | Bearer | Revoke all except current |
| POST | `/api/v1/service-tokens/issue` | Service | Issue service-to-service JWT |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

2. THE SecurityService SHALL use URL path versioning with prefix `/api/v1/`.
3. THE SecurityService SHALL return all responses in the `ApiResponse<T>` envelope format with `CorrelationId`.
4. WHEN a request fails FluentValidation, THE SecurityService SHALL return HTTP 422 with error code `VALIDATION_ERROR` (1000) and the list of validation errors.

### Requirement 19: Redis Key Patterns (REQ-019)

**User Story:** As a developer, I want well-defined Redis key patterns so that caching, sessions, and rate limiting are consistent and predictable.

#### Acceptance Criteria

1. THE SecurityService SHALL use the following Redis key patterns with their specified TTLs:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `rate:{identity}:{endpoint}` | Sliding window rate limit counters | Window duration |
| `otp:{identity}` | OTP code + attempt counter | 5 minutes |
| `session:{userId}:{deviceId}` | Active session metadata | Access token expiry |
| `refresh:{userId}:{deviceId}` | BCrypt-hashed refresh token | 7 days |
| `blacklist:{jti}` | Revoked access token JTI | Remaining token TTL |
| `lockout:{identity}` | Failed login attempt counter | 24 hours |
| `lockout:locked:{identity}` | Account locked flag | 1 hour |
| `trusted_ips:{userId}` | Set of known IP addresses | 90 days |
| `service_token:{serviceId}` | Service-to-service JWT cache | 23 hours |
| `outbox:security` | Outbox queue for audit events | Until processed |
| `user_cache:{userId}` | Cached user record from ProfileService | 15 minutes |

2. THE SecurityService SHALL use consistent key naming with colon-separated segments.
3. THE SecurityService SHALL set appropriate TTLs on all Redis keys to prevent unbounded memory growth.

### Requirement 20: SecurityService Data Models (REQ-020)

**User Story:** As a developer, I want well-defined data models so that the SecurityService database schema is clear and supports all security operations.

#### Acceptance Criteria

1. THE SecurityService SHALL maintain a `password_history` table with columns: `PasswordHistoryId` (Guid, PK), `UserId` (Guid, indexed), `PasswordHash` (string, required), `DateCreated` (DateTime).
2. THE SecurityService SHALL maintain a `service_token` table with columns: `ServiceTokenId` (Guid, PK), `ServiceId` (string, indexed, required), `ServiceName` (string, required), `TokenHash` (string, required), `DateCreated` (DateTime), `ExpiryDate` (DateTime), `IsRevoked` (bool).
3. THE SecurityService SHALL use EF Core with PostgreSQL (Npgsql) and apply auto-migrations via `DatabaseMigrationHelper` on startup.
4. THE SecurityService SHALL index the `UserId` column on `password_history` and the `ServiceId` column on `service_token` for query performance.

### Requirement 21: Inter-Service Resilience (REQ-021)

**User Story:** As a developer, I want typed service clients with Polly resilience policies so that inter-service communication from SecurityService is reliable and fault-tolerant.

#### Acceptance Criteria

1. WHEN SecurityService communicates with ProfileService, THE SecurityService SHALL use a typed service client interface (`IProfileServiceClient`).
2. WHEN the typed client makes an HTTP call, THE SecurityService SHALL apply Polly resilience policies: 3 retries with exponential backoff (1s, 2s, 4s), circuit breaker (5 failures → 30s open), and 10s timeout per request.
3. WHEN a downstream service returns 4xx or 5xx, THE SecurityService SHALL attempt to deserialize the response as `ApiResponse<object>` and throw a `DomainException` with the downstream error code. IF deserialization fails, THEN THE SecurityService SHALL throw a `DomainException` with `SERVICE_UNAVAILABLE`.
4. WHEN the circuit breaker opens, THE SecurityService SHALL throw a `DomainException` with `SERVICE_UNAVAILABLE`.
5. WHEN an inter-service call is made, THE SecurityService SHALL propagate the `X-Correlation-Id` header via `CorrelationIdDelegatingHandler`.
6. WHEN a downstream call fails, THE SecurityService SHALL log at Warning level with structured properties: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

### Requirement 22: Standardized Error Handling (REQ-022)

**User Story:** As a developer, I want all errors handled consistently so that clients receive predictable error responses from SecurityService.

#### Acceptance Criteria

1. WHEN a `DomainException` is thrown, THE SecurityService SHALL catch it via `GlobalExceptionHandlerMiddleware` and return an `ApiResponse<object>` with `application/problem+json` content type, including the error's `ErrorCode`, `ErrorValue`, `Message`, and `CorrelationId`.
2. WHEN an unhandled exception is thrown, THE SecurityService SHALL return HTTP 500 with `ErrorCode = "INTERNAL_ERROR"`, `Message = "An unexpected error occurred."`, and `CorrelationId`. THE SecurityService SHALL not leak stack traces or internal details.
3. WHEN a `RateLimitExceededException` is thrown, THE SecurityService SHALL add a `Retry-After` header to the error response.
4. WHEN any error response is returned, THE SecurityService SHALL include the `CorrelationId` from `HttpContext.Items["CorrelationId"]`.

### Requirement 23: FluentValidation Pipeline (REQ-023)

**User Story:** As a developer, I want automatic request validation so that invalid data is rejected before reaching SecurityService business logic.

#### Acceptance Criteria

1. WHEN a request DTO has a corresponding FluentValidation validator, THE SecurityService SHALL auto-discover and execute the validator before the controller action.
2. WHEN validation fails, THE SecurityService SHALL return HTTP 422 with `ErrorCode = "VALIDATION_ERROR"`, `ErrorValue = 1000`, and per-field errors in the `Errors` array as `{ field, message }` objects.
3. WHEN ASP.NET Core's built-in `ModelStateInvalidFilter` is configured, THE SecurityService SHALL disable it via `SuppressModelStateInvalidFilter = true` to let FluentValidation handle all validation.

### Requirement 24: Health Checks (REQ-024)

**User Story:** As a DevOps engineer, I want health check endpoints so that I can monitor SecurityService availability and readiness.

#### Acceptance Criteria

1. WHEN `GET /health` is called, THE SecurityService SHALL return HTTP 200 if the process is running (liveness probe).
2. WHEN `GET /ready` is called, THE SecurityService SHALL check PostgreSQL connectivity and Redis connectivity.
3. WHEN both PostgreSQL and Redis are healthy, THE SecurityService SHALL return HTTP 200 for the readiness probe.
4. IF either PostgreSQL or Redis is unhealthy, THEN THE SecurityService SHALL return a non-200 status for the readiness probe.

### Requirement 25: Configuration via Environment Variables (REQ-025)

**User Story:** As a DevOps engineer, I want all SecurityService configuration via environment variables so that the service is 12-factor compliant.

#### Acceptance Criteria

1. WHEN SecurityService starts, THE SecurityService SHALL load configuration from a `.env` file via `DotNetEnv` and populate an `AppSettings` singleton.
2. WHEN a required environment variable is missing, THE SecurityService SHALL throw `InvalidOperationException` at startup with a clear message identifying the missing variable.
3. WHEN optional environment variables are missing, THE SecurityService SHALL use sensible defaults.

### Requirement 26: CORS Configuration (REQ-026)

**User Story:** As a developer, I want CORS configured so that the frontend can communicate with SecurityService.

#### Acceptance Criteria

1. WHEN SecurityService starts, THE SecurityService SHALL configure CORS with allowed origins from the `ALLOWED_ORIGINS` environment variable (comma-separated list).
2. WHEN a preflight request is received, THE SecurityService SHALL respond with appropriate CORS headers.

### Requirement 27: Swagger Documentation (REQ-027)

**User Story:** As a developer, I want Swagger UI so that I can explore and test SecurityService API endpoints.

#### Acceptance Criteria

1. WHILE SecurityService is running in Development mode, THE SecurityService SHALL serve Swagger UI at `/swagger`.
2. WHEN Swagger is configured, THE SecurityService SHALL include JWT Bearer authentication support for testing authenticated endpoints.

### Requirement 28: Structured Logging (REQ-028)

**User Story:** As a developer, I want structured logging so that SecurityService logs are searchable and correlatable across requests.

#### Acceptance Criteria

1. WHEN a `DomainException` is logged, THE SecurityService SHALL include structured properties: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`.
2. WHEN an unhandled exception is logged, THE SecurityService SHALL include structured properties: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`.
3. WHEN a downstream call fails, THE SecurityService SHALL include structured properties: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

### Requirement 29: Pagination (REQ-029)

**User Story:** As a developer, I want consistent pagination on SecurityService list endpoints so that large datasets are handled efficiently.

#### Acceptance Criteria

1. WHEN `GET /api/v1/sessions` is called, THE SecurityService SHALL support `page` (default 1) and `pageSize` (default 20, max 100) query parameters.
2. WHEN the response is paginated, THE SecurityService SHALL include `TotalCount`, `Page`, `PageSize`, `TotalPages`, and the `Data` array.
3. WHEN `pageSize` exceeds 100, THE SecurityService SHALL cap the value at 100.

### Requirement 30: Soft Delete Pattern (REQ-030)

**User Story:** As the platform, I want soft deletes so that SecurityService data is never permanently lost and can be recovered if needed.

#### Acceptance Criteria

1. WHEN an entity is "deleted" in SecurityService, THE SecurityService SHALL set the entity's `FlgStatus` to `D` (Deactivated) instead of physically removing the record.
2. WHEN entities are queried, THE SecurityService SHALL apply EF Core global query filters to exclude entities with `FlgStatus = 'D'` by default.
3. WHEN an admin query requires access to deleted entities, THE SecurityService SHALL support bypassing the query filter via `.IgnoreQueryFilters()`.
4. THE SecurityService SHALL never perform physical deletion of records.

### Requirement 31: Error Code Resolver Service (REQ-031)

**User Story:** As a developer, I want error codes resolved to standardized response codes and descriptions so that all error responses from SecurityService are consistent and centrally managed.

#### Acceptance Criteria

1. WHEN `GlobalExceptionHandlerMiddleware` catches a `DomainException`, THE SecurityService SHALL use `IErrorCodeResolverService` to resolve the error code to its `ResponseCode` and `ResponseDescription`.
2. WHEN `IErrorCodeResolverService` resolves an error code, THE SecurityService SHALL call UtilityService's error code registry endpoint (`GET /api/v1/error-codes/{code}`) via a typed service client with Polly resilience policies (3 retries with exponential backoff, circuit breaker, and 10s timeout).
3. WHEN an error code is successfully resolved from UtilityService, THE SecurityService SHALL cache the result in Redis at key `error_code:{code}` with a 24-hour TTL.
4. WHEN a cached entry exists for the error code in Redis (`error_code:{code}`), THE SecurityService SHALL return the cached `ResponseCode` and `ResponseDescription` without calling UtilityService.
5. IF `IErrorCodeResolverService` cannot reach UtilityService (network failure, timeout, or circuit breaker open), THEN THE SecurityService SHALL fall back to a local static mapping using the `MapErrorToResponseCode` method.
6. THE SecurityService SHALL map error codes to response codes following this static mapping:

| Error Code Pattern | Response Code |
|--------------------|---------------|
| `INVALID_CREDENTIALS` | `01` |
| `ACCOUNT_LOCKED` or `ACCOUNT_INACTIVE` | `02` |
| Permission errors (`INSUFFICIENT_PERMISSIONS`, `DEPARTMENT_ACCESS_DENIED`, `ORGANIZATION_MISMATCH`) | `03` |
| OTP errors (codes starting with `OTP_`) | `04` |
| Password errors (codes starting with `PASSWORD_`) | `05` |
| Duplicate/conflict errors (`DUPLICATE` or `CONFLICT` in code) | `06` |
| Not found errors (`NOT_FOUND` in code) | `07` |
| `RATE_LIMIT_EXCEEDED` | `08` |
| Invalid errors (codes starting with `INVALID_`) | `09` |
| `VALIDATION_ERROR` | `96` |
| `INTERNAL_ERROR` | `98` |
| Default (unmatched codes) | `99` |

### Requirement 32: Clean Architecture Layer Enforcement (REQ-032)

**User Story:** As a developer, I want strict layer boundaries enforced across SecurityService projects so that architectural integrity is maintained and dependencies flow inward only.

#### Acceptance Criteria

1. THE SecurityService SHALL be structured as four .NET projects: `SecurityService.Domain`, `SecurityService.Application`, `SecurityService.Infrastructure`, and `SecurityService.Api`.
2. THE SecurityService.Domain project SHALL have zero `ProjectReference` entries and zero ASP.NET Core or EF Core package references.
3. THE SecurityService.Application project SHALL reference only `SecurityService.Domain` and contain no infrastructure packages (only FluentValidation is allowed as an external package).
4. THE SecurityService.Infrastructure project SHALL reference `SecurityService.Domain` and `SecurityService.Application`.
5. THE SecurityService.Api project SHALL reference `SecurityService.Application` and `SecurityService.Infrastructure` and serve as the composition root for dependency injection registration.
6. THE SecurityService SHALL target `net8.0` across all four projects.

### Requirement 33: Redis Outbox Message Format (REQ-033)

**User Story:** As a developer, I want a standardized outbox message format so that audit events published by SecurityService are consistently structured and reliably delivered.

#### Acceptance Criteria

1. WHEN SecurityService publishes an audit event to `outbox:security`, THE SecurityService SHALL use a JSON message containing the following fields: `MessageId` (Guid), `MessageType` (string, e.g., "AuditEvent", "NotificationRequest"), `ServiceName` ("SecurityService"), `OrganizationId` (Guid, nullable for service-level events), `UserId` (Guid, nullable), `Action` (string, e.g., "Login", "Logout", "PasswordChanged", "AccountLocked", "SessionRevoked", "CredentialGenerated", "SuspiciousLogin"), `EntityType` (string, e.g., "Session", "Password", "Account"), `EntityId` (string), `OldValue` (string, nullable), `NewValue` (string, nullable), `IpAddress` (string, nullable), `CorrelationId` (string), `Timestamp` (DateTime UTC), and `RetryCount` (int, default 0).
2. WHEN publishing an outbox message fails, THE SecurityService SHALL retry the publish operation up to 3 times with exponential backoff.
3. IF the outbox message fails to publish after 3 retry attempts, THEN THE SecurityService SHALL move the message to a dead-letter queue at `dlq:security`.
4. THE SecurityService SHALL set `RetryCount` to 0 on initial publish and increment it on each retry attempt.
