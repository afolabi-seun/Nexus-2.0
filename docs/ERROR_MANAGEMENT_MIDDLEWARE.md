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

> **Note:** SecurityService uses a string-based OutboxService API (`PublishAsync(string key, string json)`) while the other 4 services use an object-based API (`PublishAsync(object message)`). Both produce identical JSON envelopes.

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

Previous: [Validation Pipeline](./ERROR_MANAGEMENT_VALIDATION.md) · Next: [Inter-Service Error Propagation](./ERROR_MANAGEMENT_PROPAGATION.md)
