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
| `01` | Authentication failed | `INVALID_CREDENTIALS`, `TOKEN_REVOKED`, `REFRESH_TOKEN_REUSE` |
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

Next: [Error Code Registry & Resolution](./ERROR_MANAGEMENT_CODES.md)
