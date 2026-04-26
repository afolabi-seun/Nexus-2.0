# Implementation Plan: SecurityService

## Overview

Incremental implementation of the SecurityService microservice following Clean Architecture (.NET 8) with four projects: Domain, Application, Infrastructure, and Api, plus a co-located test project. All projects live under `src/backend/SecurityService/` in the Nexus-2.0 monorepo. Tasks build from the innermost layer (Domain) outward, wiring everything together in Program.cs at the end. All code is C# targeting `net8.0`.

## Tasks

- [x] 1. Solution and project scaffolding
  - [x] 1.1 Create monorepo folder structure and .NET 8 projects with project references
    - Create `src/backend/SecurityService/` directory
    - Create `src/backend/SecurityService/SecurityService.Domain` (class library, net8.0, zero project references)
    - Create `src/backend/SecurityService/SecurityService.Application` (class library, net8.0, references Domain)
    - Create `src/backend/SecurityService/SecurityService.Infrastructure` (class library, net8.0, references Domain + Application)
    - Create `src/backend/SecurityService/SecurityService.Api` (web project, net8.0, references Application + Infrastructure)
    - Create `src/backend/SecurityService/SecurityService.Tests` (xUnit test project, net8.0, references Domain + Application + Infrastructure)
    - Create `src/frontend/` placeholder directory
    - Create `docker/` placeholder directory
    - Add all projects to `Nexus-2.0.sln`
    - _Requirements: REQ-032_

  - [x] 1.2 Add NuGet package references to each project
    - Domain: no external packages
    - Application: `FluentValidation` only
    - Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Microsoft.Extensions.Http.Polly`, `Polly`, `BCrypt.Net-Next`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Redis`, `DotNetEnv`
    - Api: `FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore`
    - _Requirements: REQ-032_

- [x] 2. Domain layer — Entities, exceptions, error codes, interfaces
  - [x] 2.1 Create domain entities (`PasswordHistory`, `ServiceToken`) and common interfaces (`IOrganizationEntity`)
    - Implement `PasswordHistory` entity with `PasswordHistoryId`, `UserId`, `PasswordHash`, `DateCreated`
    - Implement `ServiceToken` entity with `ServiceTokenId`, `ServiceId`, `ServiceName`, `TokenHash`, `DateCreated`, `ExpiryDate`, `IsRevoked`
    - Create `IOrganizationEntity` marker interface in `Common/`
    - _Requirements: REQ-020, REQ-030_

  - [x] 2.2 Create `ErrorCodes` static class and `DomainException` base class
    - Implement `ErrorCodes` with all constants (1000, 2001–2025, 9999) and their string/int pairs
    - Implement `DomainException` base class with `ErrorValue`, `ErrorCode`, `StatusCode`, `CorrelationId`
    - _Requirements: REQ-022_

  - [x] 2.3 Create all concrete domain exception classes (2001–2025)
    - `InvalidCredentialsException` (2001, 401), `AccountLockedException` (2002, 423), `AccountInactiveException` (2003, 403), `PasswordReuseNotAllowedException` (2004, 400), `PasswordRecentlyUsedException` (2005, 400), `FirstTimeUserRestrictedException` (2006, 403), `OtpExpiredException` (2007, 400), `OtpVerificationFailedException` (2008, 400), `OtpMaxAttemptsException` (2009, 429), `RateLimitExceededException` (2010, 429 — include `RetryAfterSeconds`), `InsufficientPermissionsException` (2011, 403), `TokenRevokedException` (2012, 401), `RefreshTokenReuseException` (2013, 401), `ServiceNotAuthorizedException` (2016, 403), `SuspiciousLoginException` (2017, 403), `PasswordComplexityFailedException` (2018, 400), `OrganizationMismatchException` (2019, 403), `DepartmentAccessDeniedException` (2020, 403), `NotFoundException` (2021, 404), `ConflictException` (2022, 409), `ServiceUnavailableException` (2023, 503), `SessionExpiredException` (2024, 401), `InvalidDepartmentRoleException` (2025, 403)
    - _Requirements: REQ-022_

  - [x] 2.4 Create helper constants and enums (`RoleNames`, `EntityStatuses`)
    - `RoleNames` with `OrgAdmin`, `DeptLead`, `Member`, `Viewer` and their numeric values (100, 75, 50, 25)
    - `EntityStatuses` with `Active = "A"`, `Suspended = "S"`, `Deactivated = "D"`
    - _Requirements: REQ-007, REQ-030_

  - [x] 2.5 Create domain service interfaces
    - `IAuthService` (LoginAsync, LogoutAsync, RefreshTokenAsync, GenerateCredentialsAsync)
    - `IJwtService` (GenerateAccessToken, GenerateRefreshToken, GenerateServiceToken, ValidateToken, GetTokenExpiry, GetJti)
    - `ISessionService` (CreateSessionAsync, GetSessionsAsync, RevokeSessionAsync, RevokeAllSessionsExceptCurrentAsync, RevokeAllSessionsAsync)
    - `IOtpService` (GenerateOtpAsync, VerifyOtpAsync)
    - `IRateLimiterService` (CheckRateLimitAsync)
    - `IAnomalyDetectionService` (CheckLoginAnomalyAsync, AddTrustedIpAsync)
    - `IPasswordService` (ForcedChangeAsync, ResetRequestAsync, ResetConfirmAsync, ValidateComplexity, IsPasswordInHistoryAsync)
    - `IServiceTokenService` (IssueTokenAsync, ValidateServiceTokenAsync)
    - `IOutboxService` (PublishAsync)
    - `IErrorCodeResolverService` (ResolveAsync)
    - _Requirements: REQ-001, REQ-003, REQ-004, REQ-008, REQ-009, REQ-010, REQ-011, REQ-012, REQ-013, REQ-014, REQ-031, REQ-033_

  - [x] 2.6 Create repository interfaces
    - `IPasswordHistoryRepository` (GetLastNByUserIdAsync, AddAsync)
    - _Requirements: REQ-011, REQ-020_

- [x] 3. Application layer — DTOs, contracts, validators
  - [x] 3.1 Create `ApiResponse<T>` envelope and `ErrorDetail` classes
    - `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `ErrorValue`, `Message`, `CorrelationId`, `Errors`
    - `ErrorDetail` with `Field`, `Message`
    - _Requirements: REQ-022, REQ-018.3_

  - [x] 3.2 Create request DTOs
    - `LoginRequest`, `RefreshTokenRequest`, `LogoutRequest`, `OtpRequest`, `OtpVerifyRequest`, `CredentialGenerateRequest`, `ForcedPasswordChangeRequest`, `PasswordResetRequest`, `PasswordResetConfirmRequest`, `ServiceTokenIssueRequest`
    - _Requirements: REQ-001, REQ-003, REQ-004, REQ-005, REQ-006, REQ-009, REQ-011, REQ-013_

  - [x] 3.3 Create response DTOs
    - `LoginResponse` (AccessToken, RefreshToken, ExpiresIn, IsFirstTimeUser)
    - `SessionResponse` (SessionId, DeviceId, IpAddress, CreatedAt)
    - `ServiceTokenResponse` (Token, ExpiresInSeconds)
    - _Requirements: REQ-001, REQ-008, REQ-013_

  - [x] 3.4 Create inter-service contract DTOs
    - `ProfileUserResponse` (Id, Email, PasswordHash, FlgStatus, OrganizationId, DepartmentId, RoleName, DepartmentRole, IsFirstTimeUser, DeviceId)
    - `ErrorCodeResponse` (ResponseCode, Description)
    - _Requirements: REQ-021, REQ-031_

  - [x] 3.5 Create FluentValidation validators for all request DTOs
    - `LoginRequestValidator` (Email required + valid, Password required)
    - `RefreshTokenRequestValidator` (RefreshToken required, DeviceId required)
    - `OtpRequestValidator` (Identity required)
    - `OtpVerifyRequestValidator` (Identity required, Code required + 6-digit numeric)
    - `ForcedPasswordChangeRequestValidator` (NewPassword required + min 8 + uppercase + lowercase + digit + special char)
    - `PasswordResetRequestValidator` (Email required + valid)
    - `PasswordResetConfirmRequestValidator` (Email + OtpCode 6-digit + NewPassword complexity)
    - `CredentialGenerateRequestValidator` (MemberId required, Email required + valid)
    - `ServiceTokenIssueRequestValidator` (ServiceId required, ServiceName required)
    - _Requirements: REQ-023, REQ-011.1_

  - [x] 3.6 Write property tests for password complexity validation rules
    - **Property: Any string meeting all complexity rules (≥8 chars, uppercase, lowercase, digit, special) passes validation; any string missing at least one rule fails**
    - **Validates: REQ-011.1, REQ-011.2**

- [x] 4. Checkpoint — Verify Domain and Application layers compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Infrastructure layer — Data access (EF Core + PostgreSQL)
  - [x] 5.1 Create `SecurityDbContext` with entity configurations
    - Configure `PasswordHistory` (PK, UserId index, PasswordHash required)
    - Configure `ServiceToken` (PK, ServiceId index, required fields)
    - Note: these entities are NOT organization-scoped (no `IOrganizationEntity`, no global query filters)
    - _Requirements: REQ-020, REQ-030_

  - [x] 5.2 Create `DatabaseMigrationHelper` for auto-migration on startup
    - Apply pending EF Core migrations automatically
    - _Requirements: REQ-020.3_

  - [x] 5.3 Implement `PasswordHistoryRepository`
    - `GetLastNByUserIdAsync` — retrieve last N password hashes ordered by DateCreated descending
    - `AddAsync` — insert new password history entry
    - _Requirements: REQ-011.3, REQ-011.7, REQ-020_

- [x] 6. Infrastructure layer — Configuration
  - [x] 6.1 Create `AppSettings` and `JwtConfig` configuration classes
    - `AppSettings.FromEnvironment()` loading from env vars via DotNetEnv
    - All configurable values: DB connection, Redis connection, JWT settings, rate limit params, lockout params, OTP params, service URLs, allowed origins
    - `JwtConfig` with Issuer, Audience, SecretKey, expiry settings
    - _Requirements: REQ-025_

  - [x] 6.2 Create `.env.example` with all required environment variables
    - Document all env vars with sensible defaults
    - _Requirements: REQ-025_

- [x] 7. Infrastructure layer — Redis services
  - [x] 7.1 Implement `RateLimiterService` with Redis Lua sliding window script
    - `CheckRateLimitAsync` using sorted set with ZREMRANGEBYSCORE + ZCARD + ZADD
    - Return `(IsAllowed, RetryAfterSeconds)`
    - _Requirements: REQ-012_

  - [x] 7.2 Implement `SessionService` (Redis-backed multi-device sessions)
    - `CreateSessionAsync` — store session JSON at `session:{userId}:{deviceId}`
    - `GetSessionsAsync` — scan and return all sessions for a user with pagination
    - `RevokeSessionAsync` — delete specific session, blacklist JWT jti
    - `RevokeAllSessionsExceptCurrentAsync` — revoke all except current device
    - `RevokeAllSessionsAsync` — revoke all sessions for a user
    - _Requirements: REQ-008, REQ-019_

  - [x] 7.3 Implement `OtpService` (Redis-backed OTP generation and verification)
    - `GenerateOtpAsync` — generate 6-digit code, store in Redis at `otp:{identity}` with 5-min TTL and attempt counter
    - `VerifyOtpAsync` — verify code, increment attempts on failure, delete on success or max attempts
    - _Requirements: REQ-009, REQ-019_

  - [x] 7.4 Implement `AnomalyDetectionService` (trusted IP set in Redis)
    - `CheckLoginAnomalyAsync` — check IP against `trusted_ips:{userId}` set
    - `AddTrustedIpAsync` — add IP to trusted set with 90-day TTL refresh
    - _Requirements: REQ-014, REQ-019_

  - [x] 7.5 Implement `OutboxService` (Redis LPUSH to `outbox:security`)
    - `PublishAsync` — serialize `OutboxMessage` to JSON, LPUSH to `outbox:security`
    - Retry up to 3 times with exponential backoff on failure
    - Move to `dlq:security` after 3 failures
    - _Requirements: REQ-033_

  - [x] 7.6 Write property tests for sliding window rate limiter
    - **Property: For N requests within a window of max M, the first M are allowed and request M+1 is denied with RetryAfterSeconds > 0**
    - **Validates: REQ-012.1, REQ-012.4**

- [x] 8. Infrastructure layer — JWT and password services
  - [x] 8.1 Implement `JwtService`
    - `GenerateAccessToken` — create JWT with userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti claims; configurable expiry
    - `GenerateRefreshToken` — cryptographically random token string
    - `GenerateServiceToken` — JWT with serviceId, serviceName claims only (no organizationId)
    - `ValidateToken` — validate signature, issuer, audience, expiry
    - `GetTokenExpiry`, `GetJti` — extract claims from token
    - _Requirements: REQ-002, REQ-013_

  - [x] 8.2 Implement `PasswordService`
    - `ValidateComplexity` — enforce min 8 chars, uppercase, lowercase, digit, special char (!@#$%^&*)
    - `IsPasswordInHistoryAsync` — check new password against last 5 BCrypt hashes via repository
    - `ForcedChangeAsync` — validate complexity, check not same as temp password, update hash via ProfileService, record old hash in password_history, set IsFirstTimeUser=false
    - `ResetRequestAsync` — generate OTP, publish notification via outbox
    - `ResetConfirmAsync` — verify OTP, validate complexity, check history, update hash via ProfileService, record old hash
    - _Requirements: REQ-005, REQ-011_

  - [x] 8.3 Implement `ServiceTokenService`
    - `IssueTokenAsync` — generate service JWT, BCrypt-hash it, store in ServiceToken table, cache in Redis at `service_token:{serviceId}` with 23-hour TTL
    - `ValidateServiceTokenAsync` — validate token, check ACL, check revocation
    - _Requirements: REQ-013_

  - [x] 8.4 Write property tests for JWT token claims
    - **Property: Every generated access token, when validated, contains exactly the claims (userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti) that were passed to GenerateAccessToken, and jti is unique across invocations**
    - **Validates: REQ-002.1, REQ-002.4**

- [x] 9. Infrastructure layer — Service clients and error code resolver
  - [x] 9.1 Create `IProfileServiceClient` and `ProfileServiceClient` typed client
    - `GetTeamMemberByEmailAsync` — call ProfileService with service-to-service JWT, cache result in Redis at `user_cache:{userId}` for 15 minutes
    - `UpdatePasswordHashAsync` — update password hash via ProfileService
    - `SetIsFirstTimeUserAsync` — set IsFirstTimeUser flag via ProfileService
    - Automatic service token refresh when within 30 seconds of expiry
    - _Requirements: REQ-001.5, REQ-021_

  - [x] 9.2 Create `IUtilityServiceClient` and `UtilityServiceClient` typed client
    - `GetErrorCodeAsync` — call UtilityService error code registry endpoint
    - _Requirements: REQ-031.2_

  - [x] 9.3 Implement `ErrorCodeResolverService`
    - Check Redis cache at `error_code:{code}` first (24-hour TTL)
    - Call UtilityService on cache miss
    - Fall back to static `MapErrorToResponseCode` mapping on failure
    - _Requirements: REQ-031_

  - [x] 9.4 Create `CorrelationIdDelegatingHandler`
    - Propagate `X-Correlation-Id` header on all outgoing HTTP calls
    - _Requirements: REQ-021.5, REQ-017.4_

  - [x] 9.5 Create `DependencyInjection` extension class for Infrastructure service registration
    - Register all services, repositories, typed HTTP clients with Polly policies (3 retries exponential, circuit breaker 5/30s, 10s timeout)
    - Register Redis `IConnectionMultiplexer`
    - Register `SecurityDbContext`
    - _Requirements: REQ-021.2, REQ-032_

- [x] 10. Infrastructure layer — Auth service (login orchestration)
  - [x] 10.1 Implement `AuthService`
    - `LoginAsync` — full login flow: rate limit check → lockout check → resolve user (cache/ProfileService) → status check → BCrypt verify → lockout increment on failure → anomaly detection → reset lockout on success → issue tokens → create session → store refresh token → publish audit event
    - `LogoutAsync` — remove session, blacklist jti, remove refresh token, publish audit event
    - `RefreshTokenAsync` — validate refresh token → detect reuse (revoke all on reuse) → invalidate old → issue new pair → update session → store new refresh hash
    - `GenerateCredentialsAsync` — generate temp password, BCrypt hash, store via ProfileService, set IsFirstTimeUser=true, publish notification
    - _Requirements: REQ-001, REQ-003, REQ-004, REQ-006, REQ-010_

- [x] 11. Checkpoint — Verify Infrastructure layer compiles and unit tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Api layer — Middleware pipeline
  - [x] 12.1 Implement `CorrelationIdMiddleware`
    - Generate or propagate `X-Correlation-Id` header, store in `HttpContext.Items["CorrelationId"]`, include in response headers
    - _Requirements: REQ-017.4_

  - [x] 12.2 Implement `GlobalExceptionHandlerMiddleware`
    - Catch `DomainException` → resolve via `IErrorCodeResolverService` → return `ApiResponse<object>` with `application/problem+json`
    - Catch `RateLimitExceededException` → add `Retry-After` header
    - Catch unhandled exceptions → return 500 `INTERNAL_ERROR`, no stack trace leakage, publish error event to outbox
    - _Requirements: REQ-022, REQ-031_

  - [x] 12.3 Implement `RateLimiterMiddleware` (unauthenticated endpoints)
    - Apply rate limiting on `/api/v1/auth/login` (5/15min per IP) and `/api/v1/auth/otp/request` (3/5min per IP)
    - Use `IRateLimiterService` with IP-based identity
    - _Requirements: REQ-012.1, REQ-012.2, REQ-012.5_

  - [x] 12.4 Implement `JwtClaimsMiddleware`
    - Extract JWT claims (userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti) and store in `HttpContext.Items`
    - _Requirements: REQ-002, REQ-017.1_

  - [x] 12.5 Implement `TokenBlacklistMiddleware`
    - Check `blacklist:{jti}` in Redis for every authenticated request
    - Return 401 `TOKEN_REVOKED` if blacklisted
    - _Requirements: REQ-015_

  - [x] 12.6 Implement `FirstTimeUserMiddleware`
    - Block all endpoints except `POST /api/v1/password/forced-change` when `IsFirstTimeUser=true`
    - Return 403 `FIRST_TIME_USER_RESTRICTED`
    - _Requirements: REQ-005.1, REQ-017.1_

  - [x] 12.7 Implement `AuthenticatedRateLimiterMiddleware`
    - Apply per-user rate limiting on authenticated endpoints using userId from JWT claims
    - _Requirements: REQ-012.3, REQ-012.5_

  - [x] 12.8 Implement `RoleAuthorizationMiddleware`
    - Extract `roleName` and `departmentId` from JWT claims
    - OrgAdmin → organization-wide access
    - DeptLead → department-scoped access (own department only)
    - Member/Viewer → enforce department access matrix
    - Return 403 `INSUFFICIENT_PERMISSIONS` or `DEPARTMENT_ACCESS_DENIED`
    - _Requirements: REQ-007_

  - [x] 12.9 Implement `OrganizationScopeMiddleware`
    - Extract `organizationId` from JWT claims, validate against route/query params
    - Skip for service-auth tokens
    - Return 403 `ORGANIZATION_MISMATCH` on cross-org access
    - _Requirements: REQ-016_

  - [x] 12.10 Create `ServiceAuthAttribute` for service-to-service endpoint protection
    - Validate service JWT on endpoints marked with `[ServiceAuth]`
    - Return 403 `SERVICE_NOT_AUTHORIZED` if invalid
    - _Requirements: REQ-006.2, REQ-013.3_

  - [x] 12.11 Create `MiddlewarePipelineExtensions` to register middleware in correct order
    - CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → FirstTimeUserGuard → AuthenticatedRateLimiter → RoleAuthorization → OrganizationScope → Controllers
    - _Requirements: REQ-017.1_

- [x] 13. Api layer — Controllers
  - [x] 13.1 Implement `AuthController`
    - `POST /api/v1/auth/login` — no auth, accepts `LoginRequest`, returns `LoginResponse`
    - `POST /api/v1/auth/logout` — Bearer auth, invalidates current session
    - `POST /api/v1/auth/refresh` — no auth, accepts `RefreshTokenRequest`, returns `LoginResponse`
    - `POST /api/v1/auth/otp/request` — no auth, accepts `OtpRequest`
    - `POST /api/v1/auth/otp/verify` — no auth, accepts `OtpVerifyRequest`
    - `POST /api/v1/auth/credentials/generate` — `[ServiceAuth]`, accepts `CredentialGenerateRequest`
    - All responses wrapped in `ApiResponse<T>` with CorrelationId
    - _Requirements: REQ-001, REQ-003, REQ-004, REQ-006, REQ-009, REQ-018_

  - [x] 13.2 Implement `PasswordController`
    - `POST /api/v1/password/forced-change` — Bearer auth, accepts `ForcedPasswordChangeRequest`
    - `POST /api/v1/password/reset/request` — no auth, accepts `PasswordResetRequest`
    - `POST /api/v1/password/reset/confirm` — no auth, accepts `PasswordResetConfirmRequest`
    - _Requirements: REQ-005, REQ-011, REQ-018_

  - [x] 13.3 Implement `SessionController`
    - `GET /api/v1/sessions` — Bearer auth, paginated (page, pageSize with max 100)
    - `DELETE /api/v1/sessions/{sessionId}` — Bearer auth, revoke specific session
    - `DELETE /api/v1/sessions/all` — Bearer auth, revoke all except current
    - _Requirements: REQ-008, REQ-018, REQ-029_

  - [x] 13.4 Implement `ServiceTokenController`
    - `POST /api/v1/service-tokens/issue` — `[ServiceAuth]`, accepts `ServiceTokenIssueRequest`, returns `ServiceTokenResponse`
    - _Requirements: REQ-013, REQ-018_

- [x] 14. Api layer — Program.cs and DI composition root
  - [x] 14.1 Create `Program.cs` with full DI registration and middleware pipeline
    - Load `.env` via DotNetEnv, build `AppSettings`
    - Register Infrastructure services via `DependencyInjection` extension
    - Register FluentValidation validators (auto-discovery), suppress ModelStateInvalidFilter
    - Register JWT Bearer authentication with `JwtConfig`
    - Register CORS with `AllowedOrigins`
    - Register health checks (PostgreSQL + Redis)
    - Register Swagger (Development mode only)
    - Apply `DatabaseMigrationHelper` on startup
    - Build middleware pipeline in correct order via `MiddlewarePipelineExtensions`
    - Map controllers, health check endpoints (`/health`, `/ready`)
    - _Requirements: REQ-017, REQ-023, REQ-024, REQ-025, REQ-026, REQ-027, REQ-032_

- [x] 15. Api layer — Cross-cutting extensions
  - [x] 15.1 Create `ControllerServiceExtensions` for controller-specific DI
    - Register controllers with `ApiResponse<T>` envelope conventions
    - _Requirements: REQ-018.3_

  - [x] 15.2 Create `SwaggerServiceExtensions`
    - Configure Swagger with JWT Bearer auth support, API info
    - Development mode only
    - _Requirements: REQ-027_

  - [x] 15.3 Create `HealthCheckExtensions`
    - Register PostgreSQL and Redis health checks
    - Map `/health` (liveness) and `/ready` (readiness) endpoints
    - _Requirements: REQ-024_

  - [x] 15.4 Create `Dockerfile` and `.env` / `.env.example`
    - Multi-stage Dockerfile for SecurityService.Api
    - `.env.example` documenting all environment variables
    - _Requirements: REQ-025_

  - [x] 15.5 Configure structured logging conventions
    - Configure logging to use structured properties on all log entries
    - Ensure `GlobalExceptionHandlerMiddleware` logs DomainExceptions with: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`
    - Ensure unhandled exception logs include: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`
    - Ensure downstream call failure logs include: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`
    - _Requirements: REQ-028_

- [x] 16. Checkpoint — Full build verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

- [x] 17. Testing
  - [x] 17.1 Write property tests for error code resolver static mapping
    - **Property: Every known error code in `ErrorCodes` maps to a non-empty response code via `MapErrorToResponseCode`, and the mapping is deterministic (same input always produces same output)**
    - **Validates: REQ-031.5, REQ-031.6**

  - [x] 17.2 Write property tests for OTP generation and verification
    - **Property: A generated OTP code is always exactly 6 digits (000000–999999), and verifying with the correct code within TTL always succeeds**
    - **Validates: REQ-009.1, REQ-009.2**

  - [x] 17.3 Write property tests for token blacklist consistency
    - **Property: After logout, the jti is always present in the blacklist; before logout, the jti is never in the blacklist. Blacklist check is idempotent.**
    - **Validates: REQ-015.1, REQ-015.2, REQ-015.3**

  - [x] 17.4 Write property tests for refresh token rotation
    - **Property: After a successful refresh, the old refresh token hash is deleted and a new hash is stored. Using the old token after rotation triggers reuse detection and revokes all sessions.**
    - **Validates: REQ-003.1, REQ-003.2**

  - [x] 17.5 Write property tests for account lockout
    - **Property: After exactly N failed login attempts (configurable), the account is locked. While locked, login returns ACCOUNT_LOCKED without credential check. After lockout expiry, login is allowed again.**
    - **Validates: REQ-010.1, REQ-010.2, REQ-010.3**

  - [x] 17.6 Write unit tests for middleware pipeline order
    - Verify middleware registration order matches REQ-017.1 specification
    - Test GlobalExceptionHandlerMiddleware returns correct ApiResponse for DomainException and unhandled exceptions
    - Test TokenBlacklistMiddleware rejects blacklisted tokens
    - Test FirstTimeUserMiddleware blocks non-password-change endpoints
    - **Validates: REQ-017, REQ-022, REQ-015, REQ-005.1**

  - [x] 17.7 Write unit tests for FluentValidation validators
    - Test each validator with valid and invalid inputs
    - Verify password complexity rules across all password validators
    - Verify OTP code format (6-digit numeric)
    - **Validates: REQ-023, REQ-011.1**

  - [x] 17.8 Write unit tests for AuthService login flow
    - Test successful login returns tokens and creates session
    - Test invalid credentials increments lockout counter
    - Test locked account returns 423 without credential check
    - Test inactive account returns 403
    - Test anomaly detection blocks suspicious IPs
    - **Validates: REQ-001, REQ-010, REQ-014**

- [x] 18. Final checkpoint — Full integration verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

## Notes

- All SecurityService projects live under `src/backend/SecurityService/` in the monorepo
- Tests are co-located at `src/backend/SecurityService/SecurityService.Tests/`
- Frontend placeholder at `src/frontend/`, Docker config at `docker/`
- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements (REQ-NNN) for traceability
- The implementation language is C# (.NET 8) as specified in the design document
- Checkpoints ensure incremental validation at layer boundaries
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
- SecurityService entities (PasswordHistory, ServiceToken) are NOT organization-scoped — no global query filters needed
