# Error Codes

Error code registry, per-service ranges, multi-tier resolution, and the two-digit response code system.

## Error Code Structure

Every error has three identifiers:

| Field | Type | Example | Purpose |
|-------|------|---------|---------|
| `ErrorCode` | string | `ORGANIZATION_NAME_DUPLICATE` | Machine-readable, used in API responses and client-side handling |
| `ErrorValue` | int | `3005` | Numeric identifier, encodes the owning service via range |
| `ResponseCode` | string | `06` | Two-digit category code for client classification |

## Per-Service Error Value Ranges

Each service owns a numeric range. This prevents collisions and tells you which service originated an error at a glance.

| Range | Service | Examples |
|-------|---------|----------|
| `1000` | Shared | `VALIDATION_ERROR` (1000) |
| `2001–2025` | SecurityService | `INVALID_CREDENTIALS` (2001), `ACCOUNT_LOCKED` (2002), `OTP_EXPIRED` (2007), `REFRESH_TOKEN_REUSE` (2013) |
| `3001–3031` | ProfileService | `EMAIL_ALREADY_REGISTERED` (3001), `ORGANIZATION_NAME_DUPLICATE` (3005), `DEPARTMENT_CODE_DUPLICATE` (3009) |
| `4001–4072` | WorkService | `STORY_NOT_FOUND` (4001), `SPRINT_NOT_IN_PLANNING` (4006), `TIMER_ALREADY_ACTIVE` (4050), `INVALID_RISK_SEVERITY` (4061) |
| `5001–5017` | BillingService | `SUBSCRIPTION_ALREADY_EXISTS` (5001), `PLAN_NOT_FOUND` (5002), `FEATURE_NOT_AVAILABLE` (5013) |
| `6001–6015` | UtilityService | `AUDIT_LOG_IMMUTABLE` (6001), `ERROR_CODE_DUPLICATE` (6002), `OUTBOX_PROCESSING_FAILED` (6015) |
| `9001–9002` | Cross-service (DB) | `UNIQUE_CONSTRAINT_VIOLATION` (9001), `FOREIGN_KEY_VIOLATION` (9002) |
| `9999` | Cross-service | `INTERNAL_ERROR` (9999) |

## ErrorCodes Static Class

Each service defines its codes in `{Service}.Domain/Exceptions/ErrorCodes.cs`:

```csharp
public static class ErrorCodes
{
    public const string OrganizationNameDuplicate = "ORGANIZATION_NAME_DUPLICATE";
    public const int OrganizationNameDuplicateValue = 3005;

    // Database Constraints
    public const string UniqueConstraintViolation = "UNIQUE_CONSTRAINT_VIOLATION";
    public const int UniqueConstraintViolationValue = 9001;
    public const string ForeignKeyViolation = "FOREIGN_KEY_VIOLATION";
    public const int ForeignKeyViolationValue = 9002;

    // Internal
    public const string InternalError = "INTERNAL_ERROR";
    public const int InternalErrorValue = 9999;
}
```

Convention: every code has a `string` constant and a matching `int` constant with `Value` suffix.

## Error Code Registry (UtilityService)

UtilityService is the source of truth for error code metadata. It stores error codes in the `error_code_entries` table with:

- `Code` — the string error code
- `Value` — the numeric value
- `HttpStatusCode` — expected HTTP status
- `ResponseCode` — two-digit response code
- `Description` — human-readable description
- `ServiceName` — owning service

CRUD operations via `ErrorCodeService` at `/api/v1/error-codes`. The full registry is cached in Redis under key `error_codes_registry` with a 24-hour TTL. Cache is invalidated on create/update/delete.

## Three-Tier Error Code Resolution

When `GlobalExceptionHandlerMiddleware` catches a `DomainException`, it calls `IErrorCodeResolverService.ResolveAsync(errorCode)` to get the `ResponseCode` and `ResponseDescription`. Resolution follows three tiers:

```
Tier 1: Redis cache (key: error_code:{errorCode})
    ↓ miss
Tier 2: UtilityService API (GET /api/v1/error-codes/{code})
    ↓ miss or unavailable
Tier 3: Static fallback (MapErrorToResponseCode switch)
```

### Tier 1 — Redis Cache

Each service caches resolved error codes in Redis with a 24-hour TTL:

```
Key:   error_code:ORGANIZATION_NAME_DUPLICATE
Value: {"responseCode":"06","description":"Organization name duplicate"}
```

### Tier 2 — UtilityService API

If Redis misses, the service calls UtilityService via its typed HTTP client. UtilityService checks its own Redis hash cache (`error_codes_registry`), then falls back to its PostgreSQL `error_code_entries` table.

### Tier 3 — Static Fallback

If UtilityService is unavailable, each service has a local `MapErrorToResponseCode` method:

```csharp
public static string MapErrorToResponseCode(string errorCode) => errorCode switch
{
    _ when errorCode.Contains("DUPLICATE") || errorCode.Contains("CONFLICT") || errorCode.Contains("ALREADY") => "06",
    _ when errorCode.Contains("NOT_FOUND") => "07",
    "ORGANIZATION_MISMATCH" or "INSUFFICIENT_PERMISSIONS" => "03",
    "RATE_LIMIT_EXCEEDED" => "08",
    _ when errorCode.StartsWith("INVALID_") => "09",
    _ when errorCode.Contains("IMMUTABLE") || errorCode.Contains("CANNOT") => "10",
    "VALIDATION_ERROR" => "96",
    "INTERNAL_ERROR" => "98",
    _ => "99"
};
```

This ensures error responses are always meaningful, even if Redis and UtilityService are both down.

## Two-Digit Response Code System

The `ResponseCode` field in `ApiResponse<T>` is a two-digit string that categorizes the error for client-side handling:

| Code | Category | Examples |
|------|----------|----------|
| `00` | Success | All successful responses |
| `03` | Authorization | `INSUFFICIENT_PERMISSIONS`, `ORGANIZATION_MISMATCH` |
| `06` | Duplicate/Conflict | `*_DUPLICATE`, `*_ALREADY_EXISTS`, `*_CONFLICT` |
| `07` | Not Found | `*_NOT_FOUND` |
| `08` | Rate Limit | `RATE_LIMIT_EXCEEDED` |
| `09` | Invalid Input | `INVALID_*` |
| `10` | Immutable/Forbidden | `*_IMMUTABLE`, `*_CANNOT_*` |
| `96` | Validation | `VALIDATION_ERROR` (FluentValidation failures) |
| `98` | Internal Error | `INTERNAL_ERROR` |
| `99` | Unknown | Unmapped error codes |

## Adding a New Error Code

1. Choose the next available value in your service's range
2. Add constants to `ErrorCodes.cs`:
   ```csharp
   public const string MyNewError = "MY_NEW_ERROR";
   public const int MyNewErrorValue = 3032;
   ```
3. Create the typed exception class
4. Optionally register in UtilityService's error code registry for richer metadata

## Related Docs

- [ERROR_HANDLING.md](ERROR_HANDLING.md) — How exceptions flow through the middleware
- [API_RESPONSES.md](API_RESPONSES.md) — ApiResponse envelope structure
