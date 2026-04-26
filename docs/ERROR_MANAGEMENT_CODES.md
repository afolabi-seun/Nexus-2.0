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
| Shared | 1000–1003 | 4 | `VALIDATION_ERROR` (1000), `TOKEN_EXPIRED` (1001), `INVALID_TOKEN` (1002), `UNAUTHORIZED` (1003) |
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

    // Authentication (shared across all services)
    public const int TokenExpiredValue = 1001;
    public const string TokenExpired = "TOKEN_EXPIRED";

    public const int InvalidTokenValue = 1002;
    public const string InvalidToken = "INVALID_TOKEN";

    public const int UnauthorizedValue = 1003;
    public const string Unauthorized = "UNAUTHORIZED";

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

Previous: [Architecture](./ERROR_MANAGEMENT_ARCHITECTURE.md) · Next: [Validation Pipeline](./ERROR_MANAGEMENT_VALIDATION.md)
