# Nexus 2.0 Error Management

Comprehensive guide to error handling, error codes, validation, middleware, inter-service propagation, and observability across all Nexus 2.0 services.

## Table of Contents

1. [Error Management Architecture](#error-management-architecture)
2. [Error Code Registry & Resolution](#error-code-registry--resolution)
3. [Validation Pipeline](#validation-pipeline)
4. [Exception Handling Middleware](#exception-handling-middleware)
5. [Inter-Service Error Propagation](#inter-service-error-propagation)
6. [Live Demo Guide](#live-demo-guide)

Individual files are available in the [`docs/`](./INDEX.md) folder for focused reading.

---

# Error Management Architecture

## Overview

Nexus 2.0 uses a layered error handling strategy where **no service method throws exceptions for expected business failures**. Instead, errors flow through three distinct layers, each with a clear responsibility:

| Layer | Handles | Returns |
|-------|---------|---------|
| **ServiceResult\<T\>** | Business logic outcomes (not found, conflict, validation) | Result object with status code, error code, message |
| **DomainException** | Invariant violations, constraint breaches, rate limits | Caught by GlobalExceptionHandlerMiddleware |
| **GlobalExceptionHandlerMiddleware** | Unhandled/unexpected exceptions | Standardised 500 response, no internals leaked |

Every response — success or failure — is wrapped in the **ApiResponse\<T\>** envelope with a `correlationId` for end-to-end tracing.

---

## Middleware Pipeline Order

The order matters. Each middleware has a specific role in the error handling chain:

```
Request
  │
  ▼
┌─────────────────────────────┐
│ 1. CorrelationIdMiddleware  │  Generate/propagate X-Correlation-Id
├─────────────────────────────┤
│ 2. GlobalExceptionHandler   │  Catch DomainException → structured error response
│    Middleware                │  Catch Exception → generic 500 response
├─────────────────────────────┤
│ 3. ErrorResponseLogging     │  Log 5xx responses that didn't throw exceptions
│    Middleware                │  (ServiceResult-based 500s)
├─────────────────────────────┤
│ 4. RateLimiterMiddleware    │  IP-based rate limiting (unauthenticated)
├─────────────────────────────┤
│ 5. Authentication           │  JWT Bearer validation
├─────────────────────────────┤
│ 6. Authorization            │  Role-based access control
├─────────────────────────────┤
│ 7. JwtClaimsMiddleware      │  Extract TenantId, RoleId, UserId from JWT
├─────────────────────────────┤
│ 8. TokenBlacklistMiddleware │  Check JWT JTI against Redis blacklist
├─────────────────────────────┤
│ 9. AuthenticatedRateLimiter │  Per-user rate limiting
│    Middleware                │
├─────────────────────────────┤
│ 10. TenantScopeMiddleware   │  Set TenantId on DbContext
├─────────────────────────────┤
│ 11. Controller + Filters    │  NullBodyFilter → FluentValidation → Action
└─────────────────────────────┘
```

Key design decisions:
- **CorrelationId is first** — every middleware and handler downstream can reference it
- **GlobalExceptionHandler wraps everything** — any exception thrown at any layer is caught and formatted
- **ErrorResponseLogging sits inside GlobalExceptionHandler** — it catches ServiceResult-based 5xx responses that don't throw exceptions (the `ErrorLogged` flag prevents double-logging)

Source: `Core/Extensions/MiddlewarePipelineExtensions.cs` in each service.

---

## The ApiResponse\<T\> Envelope

Every API response uses this structure:

```json
// Success
{
  "responseCode": "00",
  "responseDescription": "Request successful",
  "success": true,
  "data": { ... },
  "errorCode": null,
  "errorValue": null,
  "message": "Customer created successfully.",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "errors": null
}

// Failure
{
  "responseCode": "06",
  "responseDescription": "Customer already exists",
  "success": false,
  "data": null,
  "errorCode": "CUSTOMER_ALREADY_EXISTS",
  "errorValue": 3001,
  "message": "A customer with this phone number already exists.",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "errors": null
}
```

Fields:
- `responseCode` — Two-digit category code (see Response Code Mapping below)
- `responseDescription` — Human-readable description
- `success` — Boolean flag
- `data` — Payload on success, null on failure (except validation errors where field details go here)
- `errorCode` — Machine-readable error code string (e.g. `CUSTOMER_ALREADY_EXISTS`)
- `errorValue` — Numeric error code for programmatic handling (e.g. `3001`)
- `message` — Contextual message
- `correlationId` — End-to-end trace identifier
- `errors` — Field-level validation errors (only on 422 responses)

Source: `Data/DTOs/ApiResponse.cs` in each service.

---

## Response Code Mapping

The `responseCode` field groups error codes into two-digit categories for client-side handling:

| Code | Category | Error Codes |
|------|----------|-------------|
| `00` | Success | — |
| `01` | Authentication failed | `INVALID_CREDENTIALS`, `TOKEN_EXPIRED`, `INVALID_TOKEN`, `UNAUTHORIZED`, `TOKEN_REVOKED`, `REFRESH_TOKEN_REUSE` |
| `02` | Account locked/suspended | `ACCOUNT_LOCKED`, `ACCOUNT_INACTIVE`, `TRANSACTION_PIN_LOCKED` |
| `03` | Authorization denied | `INSUFFICIENT_PERMISSIONS`, `SERVICE_NOT_AUTHORIZED`, `TENANT_MISMATCH`, `FIRST_TIME_USER_RESTRICTED` |
| `04` | OTP error | `OTP_EXPIRED`, `OTP_VERIFICATION_FAILED`, `OTP_MAX_ATTEMPTS` |
| `05` | Password policy | `PASSWORD_REUSE_NOT_ALLOWED`, `PASSWORD_RECENTLY_USED`, `PASSWORD_COMPLEXITY_FAILED` |
| `06` | Duplicate/conflict | `CUSTOMER_ALREADY_EXISTS`, `PHONE_ALREADY_REGISTERED`, `DUPLICATE_PRIMARY_WALLET`, etc. |
| `07` | Resource not found | `TRANSACTION_NOT_FOUND`, `TEMPLATE_NOT_FOUND`, `HOLD_NOT_FOUND`, etc. |
| `08` | Limit exceeded | `MAX_DEVICES_REACHED`, `SPENDING_LIMIT_EXCEEDED`, `LAST_ADMIN_CANNOT_DEACTIVATE`, etc. |
| `09` | Wallet status error | `WALLET_SUSPENDED`, `INSUFFICIENT_BALANCE`, `SME_INACTIVE`, etc. |
| `10` | Transfer blocked | `INTERNAL_TRANSFERS_BLOCKED`, `TRANSACTION_PIN_INVALID`, `TRANSACTION_PIN_REQUIRED` |
| `11` | Payment expired | `PAYMENT_LINK_EXPIRED`, `QR_PAYMENT_EXPIRED`, `REDEMPTION_CODE_EXPIRED` |
| `12` | External provider error | `BILL_PROVIDER_ERROR`, `CARD_CHARGE_FAILED`, `INVALID_METER_NUMBER` |
| `13` | Security alert | `SUSPICIOUS_LOGIN` |
| `14` | Idempotency | `IDEMPOTENCY_KEY_REQUIRED` |
| `15` | KYC error | `KYC_DOCUMENT_TOO_LARGE`, `KYC_INVALID_FORMAT` |
| `16` | Audit immutable | `AUDIT_LOG_IMMUTABLE` |
| `96` | Validation error | `VALIDATION_ERROR` |
| `97` | Rate limit | `RATE_LIMIT_EXCEEDED` |
| `98` | Internal error | `INTERNAL_ERROR`, `NOTIFICATION_DISPATCH_FAILED` |
| `99` | Unknown | Unmapped error codes |

Each service has its own `MapErrorToResponseCode` fallback that covers its service-specific error codes. The fallback is only used when all higher-priority tiers (in-memory cache, Redis, UtilityService HTTP) are unavailable. SecurityService covers auth-specific codes (INVALID_CREDENTIALS, ACCOUNT_LOCKED, OTP/password patterns), while ProfileService and WorkService cover entity-specific codes (DUPLICATE, NOT_FOUND, IMMUTABLE patterns). BillingService has a billing-specific fallback. All services share common mappings for VALIDATION_ERROR (96), INTERNAL_ERROR (98), and unknown codes (99).

---

## ServiceResult\<T\> Pattern

Service methods return `ServiceResult<T>` instead of throwing exceptions for expected outcomes:

```csharp
// Service layer — returns result, never throws for business logic
public async Task<ServiceResult<CustomerResponse>> GetByIdAsync(Guid tenantId, Guid id)
{
    var entity = await _repo.FindByIdAsync(tenantId, id);
    return entity == null
        ? ServiceResult<CustomerResponse>.NotFound("Customer not found.")
        : ServiceResult<CustomerResponse>.Ok(MapToResponse(entity));
}

// Controller — one-liner, no try/catch
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id) =>
    (await _customerService.GetByIdAsync(GetTenantId(), id)).ToActionResult();
```

Factory methods:

| Method | HTTP Status | Use Case |
|--------|-------------|----------|
| `Ok(data, message?)` | 200 | Successful retrieval or update |
| `Created(data, message?)` | 201 | Successful creation |
| `NoContent(message?)` | 204 | Successful deletion |
| `NotFound(message)` | 404 | Entity not found |
| `Fail(errorValue, errorCode, message, statusCode)` | Any | Custom failure |

The `ToActionResult()` extension converts `ServiceResult<T>` into the `ApiResponse<T>` envelope:

```
ServiceResult.Ok(data)       → 200 { success: true,  data: {...} }
ServiceResult.NotFound(msg)  → 404 { success: false, errorCode: "NOT_FOUND" }
ServiceResult.Fail(...)      → 4xx { success: false, errorCode: "...", errorValue: ... }
null ServiceResult            → 500 { success: false, errorCode: "INTERNAL_ERROR" }
Empty collection              → 200 { success: true,  data: [],  message: "No data found." }
```

### Authentication Failure Responses (401)

JWT Bearer authentication failures return a structured JSON body (not an empty response). The `OnChallenge` event handler in each service's JWT configuration produces:

```json
{
  "responseCode": "01",
  "responseDescription": "Unauthorized",
  "success": false,
  "data": null,
  "errorCode": "TOKEN_EXPIRED",
  "errorValue": 1001,
  "message": "Token has expired. Please refresh your token or log in again.",
  "correlationId": "...",
  "errors": null
}
```

| Scenario | `errorCode` | `errorValue` | `message` |
|----------|-------------|--------------|----------|
| Token lifetime exceeded | `TOKEN_EXPIRED` | 1001 | Token has expired. Please refresh your token or log in again. |
| Malformed, wrong signature, revoked | `INVALID_TOKEN` | 1002 | *(from JWT validation)* |
| No token provided | `UNAUTHORIZED` | 1003 | Authentication required. |

Source: `JwtAuthenticationExtensions.cs` → `JwtBearerEvents.OnChallenge` in each service.

Source: `Core/Results/ServiceResult.cs` and `Core/Results/ServiceResultExtensions.cs` in each service.

---

## Correlation ID Flow

Every request gets a unique `correlationId` that follows it across all services:

```
Client Request
  │
  │  X-Correlation-Id: (optional — client can provide one)
  ▼
CorrelationIdMiddleware
  │  → Reads X-Correlation-Id header or generates new GUID
  │  → Stores in HttpContext.Items["CorrelationId"]
  │  → Echoes back in response header
  ▼
Service A (e.g. ProfileService)
  │
  │  Calls Service B via typed service client
  │  CorrelationIdDelegatingHandler attaches X-Correlation-Id header
  ▼
Service B (e.g. WorkService)
  │  → CorrelationIdMiddleware reads the propagated header
  │  → Same correlationId used in logs, error responses, audit events
  ▼
Response flows back with same correlationId
```

The `correlationId` appears in:
- Every API response (`ApiResponse.CorrelationId`)
- Every error log published to UtilityService
- Every audit log entry
- Response headers (`X-Correlation-Id`)

This means you can take a `correlationId` from a client error response and trace it through error logs, audit logs, and across service boundaries.

Source: `Core/Middleware/CorrelationIdMiddleware.cs` in each service.

---

## DomainException Hierarchy

`DomainException` is the base class for all expected error conditions that can't be expressed as `ServiceResult` (e.g. thrown from repositories, validators, or deep in the call stack):

```
DomainException (base)
  ├── ErrorValue: int          (e.g. 3006)
  ├── ErrorCode: string        (e.g. "MAX_DEVICES_REACHED")
  ├── StatusCode: HttpStatusCode
  └── CorrelationId: string    (set by GlobalExceptionHandlerMiddleware)

Subclasses (ProfileService example):
  ├── CustomerAlreadyExistsException      → 409
  ├── CustomerAlreadyAttachedException    → 409
  ├── PhoneAlreadyExistsException         → 409
  ├── BeneficiaryAlreadyExistsException   → 409
  ├── TenantNameDuplicateException        → 409
  ├── MaxDevicesReachedException          → 400
  ├── MaxBeneficiariesReachedException    → 400
  ├── MaxCardsReachedException            → 400
  ├── InviteExpiredOrInvalidException     → 400
  ├── LastAdminException                  → 400
  ├── KycDocumentTooLargeException        → 400
  ├── KycInvalidFormatException           → 400
  ├── TemplateNotFoundException           → 404
  ├── TenantMismatchException             → 403
  ├── UnauthorizedException               → 403
  ├── RateLimitExceededException          → 429 (+ Retry-After header)
  └── ValidationErrorException            → 422
```

Each subclass is a one-liner that pre-fills `ErrorValue`, `ErrorCode`, `StatusCode`, and a default message:

```csharp
public class MaxDevicesReachedException : DomainException
{
    public MaxDevicesReachedException()
        : base(ErrorCodes.MaxDevicesReachedValue, ErrorCodes.MaxDevicesReached,
            "Maximum number of devices (5) has been reached.",
            HttpStatusCode.BadRequest) { }
}
```

`RateLimitExceededException` is special — it carries `RetryAfterSeconds` which `GlobalExceptionHandlerMiddleware` uses to set the `Retry-After` response header.

---

## How It All Connects

A request flows through the system like this:

```
1. Request arrives → CorrelationIdMiddleware assigns correlationId

2a. VALIDATION ERROR:
    Controller → NullBodyFilter / FluentValidation → 422 ApiResponse
    (never reaches service layer)

2b. BUSINESS LOGIC (expected):
    Controller → Service → returns ServiceResult.NotFound()
    → ToActionResult() → 404 ApiResponse with correlationId

2c. DOMAIN VIOLATION (expected, thrown):
    Controller → Service → Repository throws DomainException
    → GlobalExceptionHandlerMiddleware catches it
    → Resolves error code via IErrorCodeResolverService
    → Returns structured ApiResponse with correlationId
    → Publishes error log to UtilityService via Redis outbox

2d. UNEXPECTED ERROR:
    Any layer throws unhandled Exception
    → GlobalExceptionHandlerMiddleware catches it
    → Returns generic 500 ApiResponse (no internals leaked)
    → Logs inner exception message for diagnostics
    → Publishes error log to UtilityService via Redis outbox

3. ErrorResponseLoggingMiddleware checks:
   → If status >= 500 AND not already logged → publishes error log
   → Prevents double-logging via "ErrorLogged" flag in HttpContext.Items
```

---


---

# Error Code Registry & Resolution

## Overview

Error codes are **centrally managed** in UtilityService's `error_code_entry` table and **consumed at runtime** by all 5 services via a multi-tier cache. This means:

- One source of truth for all error codes, descriptions, HTTP status codes, and response codes
- Error code metadata can be updated at runtime without redeploying services
- Services degrade gracefully if UtilityService is unavailable (local fallback)

---

## UtilityService: The Source of Truth

### Error Code Entry Model

Each error code is stored as a row in the `error_code_entry` table (`uba_wep_utility` schema):

| Column | Type | Example | Purpose |
|--------|------|---------|---------|
| `error_code_entry_id` | UUID | `cdb931bc-...` | Primary key |
| `code` | string | `INVALID_CREDENTIALS` | Machine-readable error code |
| `value` | int | `2001` | Numeric error value for programmatic handling |
| `http_status_code` | int | `401` | HTTP status code to return |
| `response_code` | string(10) | `01` | Two-digit category code |
| `description` | string | `Invalid username or password` | Human-readable description |
| `service_name` | string | `SecurityService` | Owning service |
| `reference_code` | string | `ERC-20250615-A1B2C3D4` | Human-readable reference |
| `date_created` | timestamp | `2025-01-01T00:00:00Z` | — |
| `date_updated` | timestamp | `2025-01-01T00:00:00Z` | — |

### CRUD Endpoints

```
POST   /api/v1/error-codes       [PlatformAdmin]   Create a new error code
GET    /api/v1/error-codes        [Authorize]        List all error codes
GET    /api/v1/error-codes/{id}   [Authorize]        Get by ID
PUT    /api/v1/error-codes/{id}   [PlatformAdmin]   Update an error code
DELETE /api/v1/error-codes/{id}   [PlatformAdmin]   Soft-delete an error code
```

Create request:
```json
{
  "code": "CUSTOM_ERROR",
  "value": 9001,
  "httpStatusCode": 400,
  "responseCode": "99",
  "description": "A custom error for a new feature",
  "serviceName": "ProfileService"
}
```

Duplicate `code` values are rejected with `ERROR_CODE_DUPLICATE` (6002 / 409).

### Seeded Error Codes

All error codes are seeded via migration (`20250327000001_SeedErrorCodes.cs`). The seed covers every error code across all services so the registry is populated on first deployment.

---

## Per-Service Error Code Ranges

Each service owns a dedicated numeric range to avoid collisions:

| Service | Range | Count | Examples |
|---------|-------|-------|----------|
| Shared | 1000 | 1 | `VALIDATION_ERROR` (1000) |
| SecurityService | 2001–2022 | 22 | `INVALID_CREDENTIALS` (2001), `ACCOUNT_LOCKED` (2002), `OTP_EXPIRED` (2007) |
| ProfileService | 3001–3024 | 24 | `CUSTOMER_ALREADY_EXISTS` (3001), `MAX_DEVICES_REACHED` (3006), `PHONE_ALREADY_REGISTERED` (3010) |
| BillingService | 4001–4023 | 23 | `PAYMENT_LINK_EXPIRED` (4001), `IDEMPOTENCY_KEY_REQUIRED` (4006), `BILL_PROVIDER_ERROR` (4012) |
| WorkService | 5001–5026 | 26 | `INSUFFICIENT_BALANCE` (5001), `WALLET_SUSPENDED` (5002), `SPENDING_LIMIT_EXCEEDED` (5010) |
| UtilityService | 6001–6010 | 10 | `AUDIT_LOG_IMMUTABLE` (6001), `ERROR_CODE_DUPLICATE` (6002), `NOTIFICATION_DISPATCH_FAILED` (6004) |

Each service also defines three **common error codes** at the end of its range with the same string names but service-specific numeric values:

| Error Code | Security | Profile | Transaction | Wallet | Utility |
|------------|----------|---------|-------------|--------|---------|
| `NOT_FOUND` | 2020 | 3017 | 4020 | 5023 | 6008 |
| `CONFLICT` | 2021 | 3018 | 4021 | 5024 | 6009 |
| `SERVICE_UNAVAILABLE` | 2022 | 3019 | 4022 | 5025 | 6010 |

This means the `errorValue` in a response tells you exactly which service produced the error, even when the `errorCode` string is the same.

### Where Error Codes Are Defined in Code

Each service has a static `ErrorCodes` class at `Core/Exceptions/ErrorCodes.cs`:

```csharp
// ProfileCoreService.Api/Core/Exceptions/ErrorCodes.cs
public static class ErrorCodes
{
    public const int ValidationErrorValue = 1000;
    public const string ValidationError = "VALIDATION_ERROR";

    public const int CustomerAlreadyExistsValue = 3001;
    public const string CustomerAlreadyExists = "CUSTOMER_ALREADY_EXISTS";

    // ... service-specific codes ...

    // Common failure modes (service-specific numeric values)
    public const int NotFoundValue = 3017;
    public const string NotFound = "NOT_FOUND";

    public const int ConflictValue = 3018;
    public const string Conflict = "CONFLICT";

    public const int ServiceUnavailableValue = 3019;
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}
```

These constants are used by `DomainException` subclasses and `ServiceResult.Fail()` calls. The string codes (e.g. `"CUSTOMER_ALREADY_EXISTS"`) match the `code` column in the registry.

---

## Multi-Tier Error Code Resolution

When `GlobalExceptionHandlerMiddleware` catches a `DomainException`, it needs to resolve the error code string (e.g. `"CUSTOMER_ALREADY_EXISTS"`) into the full metadata (`responseCode`, `responseDescription`, `httpStatusCode`). This is done by `IErrorCodeResolverService` with a 4-tier fallback:

```
┌──────────────────────────────────────────────────────────────────┐
│                    ResolveAsync("CUSTOMER_ALREADY_EXISTS")       │
│                                                                  │
│  Tier 1: In-Memory Cache (ConcurrentDictionary)                 │
│  ┌─────────────────────────────────────────────┐                │
│  │ Hit? → Return immediately (zero latency)    │                │
│  │ Miss? ↓                                     │                │
│  └─────────────────────────────────────────────┘                │
│                                                                  │
│  Tier 2: Redis Hash (wep:error_codes_registry, 24h TTL)        │
│  ┌─────────────────────────────────────────────┐                │
│  │ Hit? → Populate memory cache, return        │                │
│  │ Miss or Redis down? ↓                       │                │
│  └─────────────────────────────────────────────┘                │
│                                                                  │
│  Tier 3: HTTP call to UtilityService                        │
│  ┌─────────────────────────────────────────────┐                │
│  │ GET /api/v1/error-codes (service-to-service │                │
│  │ JWT auth via IUtilityServiceClient)         │                │
│  │ Success? → Populate memory + Redis, return  │                │
│  │ Failure? ↓                                  │                │
│  └─────────────────────────────────────────────┘                │
│                                                                  │
│  Tier 4: Local Fallback (hardcoded switch expression)           │
│  ┌─────────────────────────────────────────────┐                │
│  │ Maps known error codes to defaults           │                │
│  │ Unknown codes → ("99", "An error occurred")  │                │
│  │ ALWAYS returns — never fails                 │                │
│  └─────────────────────────────────────────────┘                │
└──────────────────────────────────────────────────────────────────┘
```

### Resolution Flow in Code

```csharp
public async Task<ErrorCodeInfo> ResolveAsync(string errorCode)
{
    // Tier 1: In-memory
    if (_memoryCache.TryGetValue(errorCode, out var cached))
        return cached;

    // Tier 2: Redis
    try
    {
        var redisValue = await db.HashGetAsync(RedisCacheKey, errorCode);
        if (redisValue.HasValue) { /* populate memory, return */ }
    }
    catch { /* Redis down — continue */ }

    // Tier 3: HTTP refresh from UtilityService
    try
    {
        await RefreshCacheAsync();  // fetches ALL codes, populates memory + Redis
        if (_memoryCache.TryGetValue(errorCode, out var refreshed)) return refreshed;
    }
    catch { /* UtilityService down — continue */ }

    // Tier 4: Local fallback — always succeeds
    return FallbackResolve(errorCode);
}
```

### Background Cache Refresh

Each consuming service runs an `ErrorCodeCacheRefreshService` (hosted `BackgroundService`) that refreshes the cache every 24 hours:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await resolver.RefreshCacheAsync();           // Tier 3 → populates Tier 1 + 2
        await Task.Delay(TimeSpan.FromHours(24));     // Refresh interval
    }
}
```

This means:
- On startup, the background service pre-warms the cache
- During normal operation, Tier 1 (in-memory) handles most lookups with zero latency
- If Redis and UtilityService are both down, the local fallback ensures error responses are still properly formatted

### Local Fallback

Each service has its own `FallbackResolve()` method that maps its known error codes to defaults. This is the last resort — it ensures the service can still return structured error responses even if all external dependencies are down:

```csharp
// ProfileService fallback (excerpt)
private static ErrorCodeInfo FallbackResolve(string errorCode) => errorCode switch
{
    "VALIDATION_ERROR"          => new("96", "Validation error", 422),
    "NOT_FOUND"                 => new("07", "Not found", 404),
    "CONFLICT"                  => new("06", "Conflict", 409),
    "CUSTOMER_ALREADY_EXISTS"   => new("06", "Customer already exists", 409),
    "MAX_DEVICES_REACHED"       => new("08", "Max devices reached", 400),
    "PHONE_ALREADY_REGISTERED"  => new("06", "Phone already registered", 409),
    // ... other service-specific codes ...
    _                           => new("99", "An error occurred", 400)
};
```

Each service's fallback only covers its own error codes plus the shared ones. Unknown codes default to `("99", "An error occurred", 400)`.

---

## Redis Key

All services share a single Redis hash for the error code cache:

```
Key:    wep:error_codes_registry
Type:   Hash
TTL:    24 hours
Fields: error code string → JSON { ResponseCode, ResponseDescription, HttpStatusCode }
```

Example:
```
HGET wep:error_codes_registry CUSTOMER_ALREADY_EXISTS
→ {"ResponseCode":"06","ResponseDescription":"Customer with this phone already exists under SME","HttpStatusCode":409}
```

---

## How It All Fits Together

```
1. PlatformAdmin seeds/manages error codes via UtilityService CRUD endpoints

2. On startup, each consuming service's BackgroundService calls:
   GET /api/v1/error-codes → populates in-memory + Redis cache

3. At runtime, when GlobalExceptionHandlerMiddleware catches a DomainException:
   → Calls IErrorCodeResolverService.ResolveAsync(ex.ErrorCode)
   → Gets responseCode + description from cache (usually Tier 1)
   → Builds ApiResponse with resolved metadata

4. If UtilityService is down:
   → Tier 1/2 serve from cache (up to 24h stale)
   → Tier 4 fallback ensures responses are always structured

5. If a new error code is added to the registry:
   → Next background refresh (or next cache miss) picks it up
   → No service restart required
```

---


---

# Validation Pipeline

## Overview

Validation errors are the **most common error type** in Nexus 2.0. They are handled entirely at the controller layer — before any service or repository code runs — via a three-stage pipeline:

```
Request Body
  │
  ▼
┌──────────────────────────┐
│ 1. NullBodyFilter        │  Missing body? → 422 immediately
├──────────────────────────┤
│ 2. FluentValidation      │  Field rules fail? → 422 with field errors
│    (auto-validation)     │
├──────────────────────────┤
│ 3. Model State Factory   │  ASP.NET model binding errors → 422
│    (ConfigureApiBehavior) │
└──────────────────────────┘
  │
  ▼
Controller Action (only reached if all 3 stages pass)
```

All three stages return the same `ApiResponse<T>` envelope with `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, and HTTP 422.

---

## Stage 1: NullBodyFilter

Catches requests with a missing or null body before FluentValidation even runs. Registered as a global action filter on all controllers.

```csharp
// Core/Filters/NullBodyFilter.cs
public class NullBodyFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var param in context.ActionDescriptor.Parameters)
        {
            if (param.BindingInfo?.BindingSource?.Id == "Body" &&
                context.ActionArguments.TryGetValue(param.Name, out var value) && value == null)
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = false, ErrorCode = "VALIDATION_ERROR", ErrorValue = 1000,
                    Message = "Request body is required.",
                    ResponseCode = "99", ResponseDescription = "Validation failed"
                }) { StatusCode = 422 };
                return;
            }
        }
    }
}
```

Response when body is null:
```json
// POST /api/v1/customers  (no body)
// HTTP 422
{
  "responseCode": "99",
  "responseDescription": "Validation failed",
  "success": false,
  "data": null,
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Request body is required.",
  "correlationId": "...",
  "errors": null
}
```

Registration: `Core/Extensions/ControllerServiceExtensions.cs`
```csharp
services.AddControllers(options =>
{
    options.Filters.Add<NullBodyFilter>();
});
```

---

## Stage 2: FluentValidation Auto-Validation

FluentValidation validators are auto-discovered from the assembly and run automatically on every `[FromBody]` parameter before the controller action executes.

### Registration

```csharp
// Core/Extensions/ApplicationServiceExtensions.cs
services.AddValidatorsFromAssemblyContaining<ProfileDbContext>();

// Core/Extensions/ControllerServiceExtensions.cs
services.AddFluentValidationAutoValidation();
```

`AddValidatorsFromAssemblyContaining` scans the assembly for all classes inheriting `AbstractValidator<T>` and registers them. `AddFluentValidationAutoValidation` hooks them into the ASP.NET model validation pipeline so they run automatically — no manual `validator.Validate()` calls needed.

### Validator Example

```csharp
// Validators/CustomerCreateRequestValidator.cs
public class CustomerCreateRequestValidator : AbstractValidator<CustomerCreateRequest>
{
    private const string NigerianPhoneRegex = @"^(0\d{10}|\+234\d{10})$";

    public CustomerCreateRequestValidator()
    {
        RuleFor(x => x.SmeId)
            .NotEmpty().WithMessage("SmeId is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName is required.")
            .MaximumLength(100).WithMessage("FirstName must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName is required.")
            .MaximumLength(100).WithMessage("LastName must not exceed 100 characters.");

        RuleFor(x => x.PhoneNo)
            .NotEmpty().WithMessage("PhoneNo is required.")
            .Matches(NigerianPhoneRegex).WithMessage("PhoneNo must be a valid Nigerian phone number.")
            .MaximumLength(15).WithMessage("PhoneNo must not exceed 15 characters.");

        RuleFor(x => x.EmailAddress)
            .EmailAddress().WithMessage("EmailAddress must be a valid email address.")
            .MaximumLength(255).WithMessage("EmailAddress must not exceed 255 characters.")
            .When(x => x.EmailAddress != null);  // Only validate if provided

        RuleFor(x => x.Dob)
            .NotNull().WithMessage("Dob is required.");
    }
}
```

Key patterns:
- `.When(x => x.Field != null)` — conditional validation for optional fields
- `.WithMessage(...)` — custom error messages (not framework defaults)
- Regex patterns for Nigerian phone numbers, emails, etc.
- Max length rules matching database column constraints

### Naming Convention

Validators follow the pattern `{RequestDto}Validator.cs` in the `Validators/` folder:

| Validator | DTO | Service |
|-----------|-----|---------|
| `CustomerCreateRequestValidator` | `CustomerCreateRequest` | Profile |
| `OnboardingRequestValidator` | `OnboardingRequest` | Profile |
| `InviteCreateRequestValidator` | `InviteCreateRequest` | Profile |
| `CardCreateRequestValidator` | `CardCreateRequest` | Profile |
| `BeneficiaryCreateRequestValidator` | `BeneficiaryCreateRequest` | Profile |
| `LoginRequestValidator` | `LoginRequest` | Security |
| `ErrorCodeCreateRequestValidator` | `ErrorCodeCreateRequest` | Utility |

---

## Stage 3: Model State Factory (InvalidModelStateResponseFactory)

When FluentValidation (or ASP.NET model binding) adds errors to `ModelState`, the custom `InvalidModelStateResponseFactory` formats them into the standard `ApiResponse` envelope:

```csharp
// Core/Extensions/ControllerServiceExtensions.cs
options.InvalidModelStateResponseFactory = context =>
{
    var fieldErrors = context.ModelState
        .Where(e => e.Value?.Errors.Count > 0)
        .SelectMany(e => e.Value!.Errors.Select(err => new
        {
            Field = e.Key,
            Message = err.ErrorMessage
        }))
        .ToList();

    var correlationId = context.HttpContext.Items["CorrelationId"] as string;

    var response = new ApiResponse<object>
    {
        Success = false,
        ErrorCode = "VALIDATION_ERROR",
        ErrorValue = 1000,
        ResponseCode = "96",
        ResponseDescription = "Validation error",
        Message = "Validation error",
        Data = fieldErrors,       // Field-level errors go in data
        CorrelationId = correlationId
    };

    return new ObjectResult(response) { StatusCode = 422 };
};
```

Note: `SuppressModelStateInvalidFilter = false` (the default) ensures ASP.NET automatically returns 422 when `ModelState` is invalid — the controller action is never invoked.

### Validation Error Response

```json
// POST /api/v1/customers
// { "firstName": "", "phoneNo": "invalid" }
// HTTP 422
{
  "responseCode": "96",
  "responseDescription": "Validation error",
  "success": false,
  "data": [
    { "field": "SmeId", "message": "SmeId is required." },
    { "field": "FirstName", "message": "FirstName is required." },
    { "field": "PhoneNo", "message": "PhoneNo must be a valid Nigerian phone number." },
    { "field": "LastName", "message": "LastName is required." },
    { "field": "Dob", "message": "Dob is required." }
  ],
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Validation error",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "errors": null
}
```

Key points:
- Field-level errors are in `data` (not `errors`) — this is by design so clients can iterate the array
- Every failing field gets its own entry with the field name and message
- Multiple errors per field are possible (e.g. both `NotEmpty` and `MaximumLength` fail)
- The `correlationId` is included even on validation errors for traceability

---

## What Validation Does NOT Do

Validation only checks **structural correctness** of the request. It does NOT check:

- Whether the referenced `SmeId` exists (that's the service layer's job)
- Whether the phone number is already registered (that's a uniqueness check in the service/repository)
- Whether the user has permission to create the resource (that's authorization middleware)

This separation means:
- **422** = structurally invalid request (validation)
- **400** = structurally valid but business rule violation (service layer)
- **404** = referenced entity doesn't exist (service layer)
- **409** = duplicate/conflict (service or repository layer)

---

## Consistency Across Services

All 5 services use the same validation setup:
- `NullBodyFilter` registered as a global filter
- `AddFluentValidationAutoValidation()` for auto-validation
- `AddValidatorsFromAssemblyContaining<>()` for auto-discovery
- Same `InvalidModelStateResponseFactory` producing identical 422 responses
- Same `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, `responseCode: "96"` across all services

---


---

# Exception Handling Middleware

## Overview

Two middleware components handle exceptions and error logging in every Nexus 2.0 service:

| Middleware | Position | Responsibility |
|------------|----------|----------------|
| `GlobalExceptionHandlerMiddleware` | #2 (outermost catch-all) | Catches all thrown exceptions, formats structured responses |
| `ErrorResponseLoggingMiddleware` | #3 (inside GlobalExceptionHandler) | Catches 5xx responses that didn't throw exceptions |

Together they ensure:
- Every error returns a structured `ApiResponse` envelope
- No stack traces or internals are leaked to clients
- Every error is published to UtilityService's error log via Redis outbox
- No error is double-logged

---

## GlobalExceptionHandlerMiddleware

This is the outermost catch-all. It handles two categories of exceptions:

### 1. DomainException (Expected Errors)

Thrown by service/repository code for known business rule violations (e.g. duplicate customer, max devices reached, rate limit exceeded).

Flow:
```
DomainException thrown
  │
  ▼
GlobalExceptionHandlerMiddleware catches it
  │
  ├── Sets correlationId on the exception
  ├── Logs as Warning (not Error — this is expected)
  ├── Resolves error code via IErrorCodeResolverService (multi-tier cache)
  ├── Builds ApiResponse with resolved responseCode + description
  ├── Sets HTTP status from exception's StatusCode property
  ├── If RateLimitExceededException → adds Retry-After header
  ├── Writes JSON response
  ├── Publishes error log to UtilityService via Redis outbox
  └── Sets HttpContext.Items["ErrorLogged"] = true (prevents double-logging)
```

Response:
```json
// MaxDevicesReachedException thrown
// HTTP 400
{
  "responseCode": "08",
  "responseDescription": "Maximum 5 devices per user",
  "success": false,
  "data": null,
  "errorCode": "MAX_DEVICES_REACHED",
  "errorValue": 3006,
  "message": "Maximum number of devices (5) has been reached.",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "errors": null
}
```

Note: `responseCode` ("08") and `responseDescription` ("Maximum 5 devices per user") come from the error code registry via `IErrorCodeResolverService`, not from the exception itself. The exception provides `errorCode`, `errorValue`, `message`, and `statusCode`.

### 2. Unhandled Exception (Unexpected Errors)

Any exception that is NOT a `DomainException` — e.g. `NullReferenceException`, `DbUpdateException` not caught by the repository, network failures, etc.

Flow:
```
Unhandled Exception thrown
  │
  ▼
GlobalExceptionHandlerMiddleware catches it
  │
  ├── Extracts inner exception message (e.g. the actual PostgreSQL error)
  ├── Logs as Error with full exception type and inner message
  ├── Returns HTTP 500 with generic message (NO internals leaked)
  ├── Publishes error log with inner exception detail for diagnostics
  └── Sets HttpContext.Items["ErrorLogged"] = true
```

Response:
```json
// HTTP 500
{
  "responseCode": "98",
  "responseDescription": "An unexpected internal error occurred",
  "success": false,
  "data": null,
  "errorCode": "INTERNAL_ERROR",
  "errorValue": 0,
  "message": "An unexpected error occurred.",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "errors": null
}
```

The client sees a generic message. The **actual error detail** (including inner exception) is published to the error log in UtilityService for developer diagnostics:

```
// Error log entry (visible in GET /api/v1/error-logs)
{
  "errorCode": "INTERNAL_ERROR",
  "message": "An error occurred while saving changes → 23505: duplicate key value violates unique constraint 'ix_customer_phone_no'",
  "stackTrace": "at ProfileService.Api.Infrastructure.Repositories...",
  "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
  "severity": "Error"
}
```

### Rate Limit Exception (Special Case)

`RateLimitExceededException` is a `DomainException` subclass with special handling:

```csharp
if (ex is RateLimitExceededException rateLimitEx)
{
    context.Response.Headers["Retry-After"] = rateLimitEx.RetryAfterSeconds.ToString();
}
```

Response:
```json
// HTTP 429
// Retry-After: 60
{
  "responseCode": "97",
  "responseDescription": "Rate limit exceeded",
  "success": false,
  "data": null,
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "errorValue": 3016,
  "message": "Rate limit exceeded. Try again in 60 seconds.",
  "correlationId": "..."
}
```

Rate limit exceptions are **not published to the error log** (they'd flood it). The middleware explicitly skips outbox publishing for `RateLimitExceededException`.

### Source Code

`Core/Middleware/GlobalExceptionHandlerMiddleware.cs` — identical structure in all 5 services, only `ServiceName` constant differs.

---

## ErrorResponseLoggingMiddleware

This middleware catches a specific edge case: **5xx responses that didn't throw exceptions**.

### When Does This Happen?

When a service method returns `ServiceResult.Fail(...)` with a 500-level status code instead of throwing an exception. The `ToActionResult()` extension writes the response, but no exception is thrown, so `GlobalExceptionHandlerMiddleware` never fires.

### How It Works

```csharp
public async Task InvokeAsync(HttpContext context, IOutboxService outboxService)
{
    await _next(context);  // Let the request complete normally

    // After response is written, check if it's a 5xx that wasn't already logged
    if (context.Response.StatusCode >= 500 && !context.Items.ContainsKey("ErrorLogged"))
    {
        // Publish error log to UtilityService via Redis outbox
        var envelope = new
        {
            Type = "error",
            Payload = new
            {
                TenantId = /* from HttpContext.Items */,
                ServiceName = "ProfileService",
                ErrorCode = $"HTTP_{context.Response.StatusCode}",
                Message = $"{context.Request.Method} {context.Request.Path} returned {context.Response.StatusCode}",
                CorrelationId = /* from HttpContext.Items */,
                Severity = "Error"
            }
        };
        await outboxService.PublishAsync(RedisKeys.Outbox, JsonSerializer.Serialize(envelope));
    }
}
```

### The "ErrorLogged" Flag

The `ErrorLogged` flag in `HttpContext.Items` prevents double-logging:

```
Scenario A: Exception thrown
  → GlobalExceptionHandlerMiddleware catches it, publishes error log, sets ErrorLogged = true
  → ErrorResponseLoggingMiddleware sees ErrorLogged, skips

Scenario B: ServiceResult returns 500 (no exception)
  → GlobalExceptionHandlerMiddleware doesn't fire (no exception)
  → ErrorResponseLoggingMiddleware sees status >= 500 AND no ErrorLogged flag
  → Publishes error log

Scenario C: Normal 4xx error
  → Neither middleware publishes (not 5xx, not an unhandled exception)
  → DomainException 4xx errors ARE logged by GlobalExceptionHandlerMiddleware
```

### Source Code

`Core/Middleware/ErrorResponseLoggingMiddleware.cs` — identical structure in all 5 services.

---

## PostgreSQL Constraint Mapping

The `TenantScopedRepository<T>` base class catches `DbUpdateException` from EF Core and maps PostgreSQL error states to `DomainException`:

```csharp
// Infrastructure/Repositories/Common/TenantScopedRepository.cs
public virtual async Task<T> CreateAsync(Guid tenantId, T entity)
{
    _context.Set<T>().Add(entity);
    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
    {
        throw MapPostgresException(pg);
    }
    return entity;
}

private static DomainException MapPostgresException(PostgresException pg)
{
    var constraint = pg.ConstraintName ?? "unknown";
    var detail = pg.Detail ?? pg.MessageText;
    return pg.SqlState switch
    {
        "23505" => new DomainException(
            ErrorCodes.ConflictValue, ErrorCodes.Conflict,
            $"Duplicate value violates unique constraint '{constraint}': {detail}",
            HttpStatusCode.Conflict),       // → 409

        "23503" => new DomainException(
            ErrorCodes.NotFoundValue, ErrorCodes.NotFound,
            $"Referenced entity does not exist (constraint '{constraint}'): {detail}",
            HttpStatusCode.BadRequest),     // → 400

        _ => new DomainException(
            ErrorCodes.ConflictValue, ErrorCodes.Conflict,
            $"Database constraint violation '{constraint}' ({pg.SqlState}): {detail}",
            HttpStatusCode.Conflict)        // → 409
    };
}
```

| PostgreSQL State | Meaning | Maps To | HTTP |
|------------------|---------|---------|------|
| `23505` | Unique violation | `CONFLICT` | 409 |
| `23503` | Foreign key violation | `NOT_FOUND` | 400 |
| Other | Unknown constraint | `CONFLICT` | 409 |

The constraint name and detail are included in the error message for diagnostics:

```json
// Duplicate phone number insert
// HTTP 409
{
  "errorCode": "CONFLICT",
  "errorValue": 3018,
  "message": "Duplicate value violates unique constraint 'ix_customer_phone_no': Key (phone_no)=(+2348012345678) already exists."
}
```

This mapping applies to both `CreateAsync` and `UpdateAsync` in the base repository. Subclass repositories that call `SaveChangesAsync` directly should implement the same pattern.

### Two Layers of Uniqueness Protection

Nexus 2.0 enforces uniqueness at two levels:

1. **Application level** — Service methods check for duplicates before insert (e.g. `FindByPhoneAsync`) and throw specific `DomainException` subclasses (e.g. `CustomerAlreadyExistsException`) with descriptive messages

2. **Database level** — Unique indexes catch race conditions where two concurrent requests pass the application check. The repository's `MapPostgresException` converts these to generic `CONFLICT` errors

The application-level check provides better error messages. The database-level check is the safety net.

---

## Rate Limiting Middleware

Two rate limiting middleware components throw `RateLimitExceededException`:

### RateLimiterMiddleware (Unauthenticated)

Position #4 in the pipeline. Enforces IP-based sliding window limits on public endpoints:

| Endpoint | Limit |
|----------|-------|
| `POST /api/v1/onboarding/complete` | Per-IP |
| `POST /api/v1/invites/{token}/accept` | Per-IP |
| `POST /api/v1/customers/self-register` | Per-IP |

```csharp
var result = await rateLimiterService.CheckRateLimitAsync(clientIp, "default");
if (!result.Allowed)
    throw new RateLimitExceededException(result.RetryAfterSeconds);
```

### AuthenticatedRateLimiterMiddleware (Per-User)

Position #9 in the pipeline (after JWT extraction). Enforces per-user limits on write-heavy endpoints:

| Endpoint | Type Key |
|----------|----------|
| `POST /api/v1/customers` | `customer_create` |
| `POST /api/v1/customers/attach` | `customer_attach` |
| `POST /api/v1/invites` | `invite_create` |
| `POST /api/v1/kyc/{id}/documents` | `kyc_upload` |

Both use Redis sliding windows via `IRateLimiterService` and throw `RateLimitExceededException` which `GlobalExceptionHandlerMiddleware` handles with the `Retry-After` header.

---

## Error Publishing to Outbox

Both middleware components publish errors to UtilityService via the Redis outbox with the same envelope structure:

```json
{
  "type": "error",
  "payload": {
    "tenantId": "11111111-1111-1111-1111-111111111111",
    "serviceName": "ProfileService",
    "errorCode": "INTERNAL_ERROR",
    "message": "An error occurred while saving changes → 23505: duplicate key...",
    "stackTrace": "at ProfileService.Api.Infrastructure...",
    "correlationId": "da490d7b-73a9-4f1f-8580-e7a391607286",
    "severity": "Error"
  },
  "timestamp": "2025-06-15T10:30:00Z",
  "id": "a1b2c3d4-..."
}
```

| Field | Source |
|-------|--------|
| `tenantId` | `HttpContext.Items["TenantId"]` (from JWT or X-Tenant-Id header) |
| `serviceName` | Hardcoded constant per service |
| `errorCode` | From `DomainException.ErrorCode` or `"INTERNAL_ERROR"` |
| `message` | Exception message (includes inner exception for unhandled errors) |
| `stackTrace` | Full stack trace (only in outbox, never in API response) |
| `correlationId` | From `HttpContext.Items["CorrelationId"]` |
| `severity` | `"Warning"` for DomainException, `"Error"` for unhandled |

The outbox publish is wrapped in a try/catch — if Redis is down, the error is logged locally but the API response is still returned to the client.

---


---

# Inter-Service Error Propagation

## Overview

Nexus 2.0 services communicate synchronously via typed HTTP clients and asynchronously via Redis outbox queues. Errors propagate across service boundaries through three mechanisms:

| Mechanism | Direction | Purpose |
|-----------|-----------|---------|
| Typed service clients | Caller ← Downstream | Deserialize downstream errors, re-throw as local DomainException |
| Correlation ID propagation | Caller → Downstream | Same trace ID across all services in a request chain |
| Redis outbox | All services → UtilityService | Centralized error and audit log storage |

---

## Downstream Error Deserialization

Every typed service client (e.g. `WalletServiceClient`, `ProfileServiceClient`) follows the same `HandleDownstreamErrorAsync` pattern:

```
Caller Service                          Downstream Service
     │                                        │
     │  POST /api/v1/customer-wallets         │
     │  Authorization: Bearer <service-jwt>   │
     │  X-Tenant-Id: 11111111-...             │
     │  X-Correlation-Id: abc-123             │
     │ ─────────────────────────────────────► │
     │                                        │
     │        HTTP 400                        │
     │        { errorCode: "WALLET_SUSPENDED",│
     │          errorValue: 5002,             │
     │          message: "Wallet is suspended"│
     │        }                               │
     │ ◄───────────────────────────────────── │
     │                                        │
     │  Deserialize ApiResponse               │
     │  Re-throw as DomainException(          │
     │    5002, "WALLET_SUSPENDED",           │
     │    "Wallet is suspended",              │
     │    HttpStatusCode.BadRequest)          │
     │                                        │
     │  GlobalExceptionHandlerMiddleware      │
     │  catches it → returns to client        │
```

### HandleDownstreamErrorAsync

This method is identical across all typed service clients:

```csharp
private async Task HandleDownstreamErrorAsync(
    HttpResponseMessage response, string endpoint, long elapsedMs)
{
    // 1. Log the failure with correlation ID and timing
    _logger.LogWarning(
        "Downstream call failed. CorrelationId={CorrelationId} " +
        "Downstream={Service} Endpoint={Endpoint} Status={Status} Elapsed={Ms}ms",
        correlationId, DownstreamServiceName, endpoint, statusCode, elapsedMs);

    // 2. Try to deserialize the downstream ApiResponse
    var body = await response.Content.ReadAsStringAsync();
    try
    {
        var downstream = JsonSerializer.Deserialize<ApiResponse<object>>(body, JsonOptions);
        if (downstream != null && !string.IsNullOrEmpty(downstream.ErrorCode))
        {
            // 3. Re-throw as a local DomainException with downstream's error details
            throw new DomainException(
                downstream.ErrorValue ?? 0,
                downstream.ErrorCode,
                downstream.Message ?? $"{DownstreamServiceName} returned an error.",
                response.StatusCode);
        }
    }
    catch (JsonException) { /* Non-JSON response — fall through */ }
    catch (DomainException) { throw; }

    // 4. Fallback: downstream returned non-JSON or unexpected format
    throw new DomainException(
        ErrorCodes.ServiceUnavailableValue,
        ErrorCodes.ServiceUnavailable,
        $"{DownstreamServiceName} returned HTTP {statusCode} for {endpoint}.",
        HttpStatusCode.ServiceUnavailable);
}
```

Key behaviors:
- **Structured downstream error** → Re-thrown as `DomainException` preserving the original `errorCode`, `errorValue`, and `message`. The client sees the downstream error as if it originated from the caller service.
- **Non-JSON or unexpected response** → Wrapped as `SERVICE_UNAVAILABLE`. The client sees a generic service unavailable error.
- **404 from downstream** → Some clients handle this specially (e.g. `GetUserByUsernameAsync` returns `null` on 404 instead of throwing).

### What the Client Sees

When ProfileService calls WorkService and it fails:

```json
// Client called: POST /api/v1/customers (ProfileService)
// ProfileService called: POST /api/v1/customer-wallets (WorkService)
// WorkService returned 400 with WALLET_SUSPENDED

// Client receives from ProfileService:
// HTTP 400
{
  "responseCode": "09",
  "responseDescription": "Wallet is suspended",
  "success": false,
  "errorCode": "WALLET_SUSPENDED",
  "errorValue": 5002,
  "message": "Wallet is suspended",
  "correlationId": "abc-123"
}
```

The error code `5002` (WorkService range) tells the client exactly which service produced the error, even though the response came from ProfileService.

---

## Correlation ID Propagation

The same `correlationId` follows a request across all service boundaries:

### Step 1: Generation (CorrelationIdMiddleware)

```csharp
// Runs on every incoming request in every service
var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString();
context.Items["CorrelationId"] = correlationId;
context.Response.Headers["X-Correlation-Id"] = correlationId;
```

### Step 2: Outgoing Propagation (CorrelationIdDelegatingHandler)

Registered as a `DelegatingHandler` on all typed `HttpClient` instances:

```csharp
// Automatically attached to every outgoing inter-service HTTP call
protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken)
{
    var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string
                        ?? Guid.NewGuid().ToString();
    request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
    return base.SendAsync(request, cancellationToken);
}
```

### Step 3: Downstream Receives It

The downstream service's `CorrelationIdMiddleware` reads the propagated header instead of generating a new one. The same ID is used in all logs, error responses, and audit events.

### Full Chain Example

```
Client → ProfileService → WorkService → SecurityService
         correlationId: abc-123
                                correlationId: abc-123 (propagated)
                                                       correlationId: abc-123 (propagated)

All error logs, audit logs, and API responses use abc-123.
Query: GET /api/v1/error-logs?correlationId=abc-123
→ Returns error entries from ALL services involved in this request chain.
```

---

## Polly Resilience Policies

All inter-service HTTP clients are configured with Polly policies for transient failure handling:

```csharp
// Core/Extensions/ApplicationServiceExtensions.cs (same in all services)
services.AddHttpClient("WalletCoreService", client =>
{
    client.BaseAddress = new Uri(appSettings.WalletCoreServiceBaseUrl);
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)))
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))))
.AddTransientHttpErrorPolicy(p =>
    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

| Policy | Configuration | Behavior |
|--------|---------------|----------|
| Timeout | 10 seconds | Aborts request if downstream doesn't respond in 10s |
| Retry | 3 retries, exponential backoff (1s, 2s, 4s) | Retries on transient HTTP errors (5xx, network failures) |
| Circuit Breaker | 5 failures → 30s open | Stops calling downstream for 30s after 5 consecutive failures |

Error flow with Polly:
```
1. First call fails (5xx) → Polly retries after 1s
2. Second call fails (5xx) → Polly retries after 2s
3. Third call fails (5xx) → Polly retries after 4s
4. Fourth call fails (5xx) → Polly gives up, exception propagates
5. HandleDownstreamErrorAsync catches it → DomainException(SERVICE_UNAVAILABLE)
6. GlobalExceptionHandlerMiddleware → 503 ApiResponse to client

If 5 calls fail within a window:
→ Circuit breaker opens for 30s
→ Subsequent calls fail immediately (no HTTP call made)
→ BrokenCircuitException → SERVICE_UNAVAILABLE
```

---

## Service-to-Service JWT & Tenant Propagation

Every outgoing inter-service call attaches two headers:

### Authorization Header

```csharp
private async Task AttachHeadersAsync(HttpClient client)
{
    var token = await GetServiceTokenAsync();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    // ...
}
```

Service tokens are cached locally with a 30-second buffer before expiry. If the token is expired or missing, a new one is issued via `IServiceAuthService`.

### X-Tenant-Id Header

```csharp
var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString();
if (!string.IsNullOrEmpty(tenantId))
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", tenantId);
```

Service-to-service JWTs are stateless identity tokens (no tenant claim). Tenant context is propagated via the `X-Tenant-Id` header so the downstream service can scope its database queries correctly.

---

## Redis Outbox: Async Error Publishing

All services publish errors asynchronously to UtilityService via per-service Redis queues:

```
ProfileService   → wep:outbox:profile
SecurityService  → wep:outbox:security
BillingService → wep:outbox:transaction
WorkService    → wep:outbox:wallet
```

### Publisher Side (All Services)

`GlobalExceptionHandlerMiddleware` and `ErrorResponseLoggingMiddleware` publish error events:

```csharp
var envelope = new
{
    Type = "error",
    Payload = new
    {
        TenantId = tenantId,
        ServiceName = "ProfileService",
        ErrorCode = "INTERNAL_ERROR",
        Message = "An error occurred → 23505: duplicate key...",
        StackTrace = ex.StackTrace,
        CorrelationId = correlationId,
        Severity = "Error"
    },
    Timestamp = DateTime.UtcNow,
    Id = Guid.NewGuid()
};
await outboxService.PublishAsync(RedisKeys.Outbox, JsonSerializer.Serialize(envelope));
```

### Consumer Side (UtilityService)

`OutboxProcessorHostedService` runs as a background service, polling all 4 outbox queues:

```csharp
// Polls every few seconds
foreach (var queue in OutboxQueues)  // profile, security, transaction, wallet
{
    await ProcessQueueAsync(queue, cancellationToken);
}
```

Processing uses `RPOPLPUSH` for reliability:

```
1. RPOPLPUSH wep:outbox:profile → wep:outbox:profile:processing
   (atomically moves message to processing queue)

2. Route by envelope.Type:
   - "error"        → IErrorLogService.AddAsync()     → error_log table
   - "audit"        → IAuditLogService.AddAsync()     → audit_log table
   - "notification" → INotificationDispatchService     → notification_log table

3. On success: LREM wep:outbox:profile:processing (remove from processing)
4. On failure: Move back to wep:outbox:profile for retry
```

### Error Log Entry

Once processed, the error is stored in UtilityService's `error_log` table and queryable via:

```
GET /api/v1/error-logs                              (all error logs, paginated)
GET /api/v1/error-logs?correlationId=abc-123        (filter by correlation ID)
```

Each entry includes:
- `tenantId` — which tenant's request caused the error
- `serviceName` — which service produced the error
- `errorCode` — the error code string
- `message` — error detail (includes inner exception for unhandled errors)
- `stackTrace` — full stack trace (for developer diagnostics)
- `correlationId` — links back to the original client request
- `severity` — `Warning` (DomainException) or `Error` (unhandled)

---

## End-to-End Tracing Example

```
1. Client sends POST /api/v1/customers to ProfileService
   → CorrelationIdMiddleware generates: abc-123

2. ProfileService calls WorkService to create customer wallet
   → CorrelationIdDelegatingHandler attaches X-Correlation-Id: abc-123
   → X-Tenant-Id: 11111111-... attached

3. WorkService fails with INSUFFICIENT_BALANCE
   → Returns 400 { errorCode: "INSUFFICIENT_BALANCE", correlationId: "abc-123" }
   → Publishes error to wep:outbox:wallet with correlationId: abc-123

4. ProfileService's WalletServiceClient deserializes the error
   → Re-throws as DomainException(5001, "INSUFFICIENT_BALANCE", ...)
   → GlobalExceptionHandlerMiddleware catches it
   → Returns 400 to client with correlationId: abc-123
   → Publishes error to wep:outbox:profile with correlationId: abc-123

5. UtilityService's OutboxProcessor picks up both error events
   → Stores two error_log entries, both with correlationId: abc-123

6. Developer queries: GET /api/v1/error-logs?correlationId=abc-123
   → Sees the full chain:
     - WorkService: INSUFFICIENT_BALANCE (original error)
     - ProfileService: INSUFFICIENT_BALANCE (propagated error)
```

---


---

# Live Demo Guide

## Prerequisites

- All 5 services running (`dotnet run` in each service folder)
- Postman with `WEP_Environment.postman_environment.json` imported
- At least one SME onboarded (run `POST /api/v1/onboarding/complete` first — see `TESTING_GUIDE.md`)
- A valid PlatformAdmin access token (from login)

---

## Demo Flow

The demo walks through each error layer in order, from the outermost (validation) to the deepest (inter-service propagation), then shows the observability trail.

---

## 1. Validation Errors (422)

### 1a. Null Body — NullBodyFilter

```
POST /api/v1/customers
Authorization: Bearer {{accessToken}}
Content-Type: application/json

(no body)
```

Response (HTTP 422):
```json
{
  "responseCode": "99",
  "responseDescription": "Validation failed",
  "success": false,
  "data": null,
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Request body is required.",
  "correlationId": "..."
}
```

**Talking point:** NullBodyFilter catches this before FluentValidation even runs.

### 1b. Field Validation — FluentValidation

```
POST /api/v1/customers
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "firstName": "",
  "phoneNo": "invalid",
  "smeId": "00000000-0000-0000-0000-000000000000"
}
```

Response (HTTP 422):
```json
{
  "responseCode": "96",
  "responseDescription": "Validation error",
  "success": false,
  "data": [
    { "field": "SmeId", "message": "SmeId is required." },
    { "field": "FirstName", "message": "FirstName is required." },
    { "field": "PhoneNo", "message": "PhoneNo must be a valid Nigerian phone number." },
    { "field": "LastName", "message": "LastName is required." },
    { "field": "Dob", "message": "Dob is required." }
  ],
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Validation error",
  "correlationId": "..."
}
```

**Talking points:**
- Every failing field gets its own entry
- `responseCode: "96"` = validation category
- `errorValue: 1000` is shared across all 5 services
- The request never reached the service layer

---

## 2. Business Logic Errors (ServiceResult)

### 2a. Not Found (404)

```
GET /api/v1/customers/00000000-0000-0000-0000-000000000099
Authorization: Bearer {{accessToken}}
```

Response (HTTP 404):
```json
{
  "responseCode": "07",
  "responseDescription": "Not found",
  "success": false,
  "data": null,
  "errorCode": "NOT_FOUND",
  "errorValue": 0,
  "message": "Customer not found.",
  "correlationId": "..."
}
```

**Talking point:** This is a `ServiceResult.NotFound()` — no exception thrown, controller is a one-liner.

### 2b. Empty Collection (200)

```
GET /api/v1/customers?smeId={{smeId}}
Authorization: Bearer {{accessToken}}
```

(If no customers exist yet)

Response (HTTP 200):
```json
{
  "responseCode": "00",
  "responseDescription": "No data found.",
  "success": true,
  "data": {
    "items": [],
    "totalCount": 0,
    "page": 1,
    "pageSize": 20
  },
  "correlationId": "..."
}
```

**Talking point:** Empty collections return 200 with an empty array, not 404. This is by design — the query succeeded, there's just no data.

---

## 3. Domain Exceptions (DomainException → GlobalExceptionHandler)

### 3a. Conflict — Duplicate Entity (409)

Create a customer, then try to create another with the same phone number:

```
POST /api/v1/customers
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "smeId": "{{smeId}}",
  "firstName": "Adebayo",
  "lastName": "Ogundimu",
  "phoneNo": "+2348012345678",
  "dob": "1990-01-15"
}
```

First call → 201 (success). Second call with same `phoneNo`:

Response (HTTP 409):
```json
{
  "responseCode": "06",
  "responseDescription": "Phone number already registered",
  "success": false,
  "data": null,
  "errorCode": "PHONE_ALREADY_REGISTERED",
  "errorValue": 3010,
  "message": "Phone number +2348012345678 is already registered.",
  "correlationId": "..."
}
```

**Talking points:**
- Application-level uniqueness check threw `PhoneAlreadyExistsException`
- `GlobalExceptionHandlerMiddleware` caught it, resolved the error code from the registry
- `responseCode: "06"` and `responseDescription` came from the error code registry, not the exception
- `errorValue: 3010` is in the ProfileService range (3001–3024)

### 3b. Database Constraint Violation (409)

If the application-level check is bypassed (race condition), the database unique index catches it:

Response (HTTP 409):
```json
{
  "responseCode": "06",
  "responseDescription": "Conflict",
  "success": false,
  "data": null,
  "errorCode": "CONFLICT",
  "errorValue": 3018,
  "message": "Duplicate value violates unique constraint 'ix_customer_phone_no': Key (phone_no)=(+2348012345678) already exists.",
  "correlationId": "..."
}
```

**Talking point:** Two layers of protection — application check gives better messages, database constraint is the safety net for race conditions.

### 3c. Limit Exceeded (400)

Register 5 devices for a user, then try a 6th:

```
POST /api/v1/devices
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "deviceTypeId": "a0000000-0000-0000-0000-000000000001",
  "deviceName": "Device 6",
  "isPrimary": false
}
```

Response (HTTP 400):
```json
{
  "responseCode": "08",
  "responseDescription": "Maximum 5 devices per user",
  "success": false,
  "data": null,
  "errorCode": "MAX_DEVICES_REACHED",
  "errorValue": 3006,
  "message": "Maximum number of devices (5) has been reached.",
  "correlationId": "..."
}
```

### 3d. Rate Limiting (429)

Hit a rate-limited endpoint repeatedly:

```
POST /api/v1/onboarding/complete
Content-Type: application/json

(repeat rapidly)
```

Response (HTTP 429):
```json
{
  "responseCode": "97",
  "responseDescription": "Rate limit exceeded",
  "success": false,
  "data": null,
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "errorValue": 3016,
  "message": "Rate limit exceeded. Try again in 60 seconds.",
  "correlationId": "..."
}
```

Response headers:
```
Retry-After: 60
```

**Talking point:** `RateLimitExceededException` is the only `DomainException` subclass that adds a response header. It's also the only one that is NOT published to the error log (to avoid flooding).

---

## 4. Error Code Registry

### 4a. View All Error Codes

```
GET /api/v1/error-codes
Authorization: Bearer {{accessToken}}
```

Response (HTTP 200):
```json
{
  "success": true,
  "data": [
    {
      "errorCodeEntryId": "cdb931bc-...",
      "code": "INVALID_CREDENTIALS",
      "value": 2001,
      "httpStatusCode": 401,
      "responseCode": "01",
      "description": "Invalid username or password",
      "serviceName": "SecurityService",
      "referenceCode": "ERC-20250101-..."
    },
    ...
  ]
}
```

**Talking point:** This is the source of truth. When `GlobalExceptionHandlerMiddleware` catches a `DomainException`, it calls `IErrorCodeResolverService.ResolveAsync(errorCode)` which looks up the `responseCode` and `description` from this registry (via multi-tier cache).

### 4b. Resilience Demo — Stop UtilityService

1. Stop UtilityService (`Ctrl+C`)
2. Trigger an error in ProfileService (e.g. duplicate customer)
3. Observe: the error response still has correct `responseCode` and `responseDescription`

**Talking point:** The multi-tier cache (in-memory → Redis → HTTP → local fallback) means error responses are properly formatted even when UtilityService is down. The local fallback is the last resort — hardcoded in each service.

4. Restart UtilityService
5. Wait for the background refresh (or trigger a cache miss)
6. Observe: cache is repopulated from the registry

---

## 5. Observability Trail

After triggering several errors above, query the centralized logs:

### 5a. Error Logs

```
GET /api/v1/error-logs
Authorization: Bearer {{accessToken}}
```

Response (HTTP 200):
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "serviceName": "ProfileService",
        "errorCode": "PHONE_ALREADY_REGISTERED",
        "message": "Phone number +2348012345678 is already registered.",
        "stackTrace": "at ProfileService.Api.Infrastructure.Services...",
        "correlationId": "abc-123",
        "severity": "Warning",
        "dateCreated": "2025-06-15T10:30:00Z",
        "referenceCode": "ERL-20250615-..."
      },
      ...
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 20
  }
}
```

**Talking points:**
- Every `DomainException` (severity: Warning) and unhandled exception (severity: Error) is here
- `correlationId` matches the client response — you can trace end-to-end
- `stackTrace` is available for diagnostics but never leaked to the client
- Rate limit errors are excluded (they'd flood the log)

### 5b. Trace a Specific Error

Copy a `correlationId` from any error response and query:

```
GET /api/v1/error-logs?correlationId=abc-123
Authorization: Bearer {{accessToken}}
```

**Talking point:** For cross-service errors, you'll see multiple entries with the same `correlationId` — one from each service in the chain.

### 5c. Audit Logs

```
GET /api/v1/audit-logs
Authorization: Bearer {{accessToken}}
```

**Talking point:** Successful operations are in audit logs, failed operations are in error logs. Both share the same `correlationId` so you can see the full picture of what happened during a request.

---

## 6. Inter-Service Error Propagation

### 6a. Downstream Error Passthrough

Create a customer (which triggers wallet creation in WorkService). If WorkService is down:

```
POST /api/v1/customers
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "smeId": "{{smeId}}",
  "firstName": "Test",
  "lastName": "User",
  "phoneNo": "+2349087654321",
  "dob": "1990-01-15"
}
```

Response (HTTP 503):
```json
{
  "responseCode": "99",
  "responseDescription": "Service unavailable",
  "success": false,
  "data": null,
  "errorCode": "SERVICE_UNAVAILABLE",
  "errorValue": 3019,
  "message": "WorkService returned HTTP 503 for /api/v1/customer-wallets.",
  "correlationId": "..."
}
```

**Talking points:**
- ProfileService's `WalletServiceClient` caught the failure
- Polly retried 3 times with exponential backoff before giving up
- The error was re-thrown as a local `DomainException(SERVICE_UNAVAILABLE)`
- `errorValue: 3019` tells you ProfileService produced this (its SERVICE_UNAVAILABLE range)
- The `correlationId` can be used to find error logs from both services

### 6b. Downstream Business Error Passthrough

If WorkService is running but returns a business error (e.g. duplicate wallet):

Response (HTTP 409):
```json
{
  "errorCode": "DUPLICATE_PRIMARY_WALLET",
  "errorValue": 5013,
  "message": "Primary wallet already exists for this SME"
}
```

**Talking point:** `errorValue: 5013` is in the WorkService range — the client can tell exactly which service produced the error even though the response came from ProfileService.

---

## Quick Reference: Error Response Cheat Sheet

| Scenario | HTTP | responseCode | errorCode | Source |
|----------|------|-------------|-----------|--------|
| Null body | 422 | 99 | VALIDATION_ERROR | NullBodyFilter |
| Field validation | 422 | 96 | VALIDATION_ERROR | FluentValidation |
| Entity not found | 404 | 07 | NOT_FOUND | ServiceResult |
| Duplicate entity | 409 | 06 | (specific code) | DomainException |
| DB constraint race | 409 | 06 | CONFLICT | TenantScopedRepository |
| Limit exceeded | 400 | 08 | (specific code) | DomainException |
| Rate limited | 429 | 97 | RATE_LIMIT_EXCEEDED | RateLimiterMiddleware |
| Downstream error | varies | varies | (downstream code) | Service client |
| Service down | 503 | 99 | SERVICE_UNAVAILABLE | Service client + Polly |
| Unhandled exception | 500 | 98 | INTERNAL_ERROR | GlobalExceptionHandler |

---

## Presentation Tips

- Keep a terminal with `dotnet run` logs visible — colleagues can see the middleware pipeline and Polly retries in real time
- Use the Postman collection's "Tests" tab to highlight the `correlationId` in responses
- The device `SetPrimaryAsync` bug we fixed is a great "before/after" example — show the error log entry with the stack trace pointing to the root cause, then show the fix
- Stop/start UtilityService mid-demo to show the error code cache fallback in action

---


---

