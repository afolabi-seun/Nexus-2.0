# Inter-Service Error Propagation

## Overview

WEP services communicate synchronously via typed HTTP clients and asynchronously via Redis outbox queues. Errors propagate across service boundaries through three mechanisms:

| Mechanism | Direction | Purpose |
|-----------|-----------|---------|
| Typed service clients | Caller ← Downstream | Deserialize downstream errors, re-throw as local DomainException |
| Correlation ID propagation | Caller → Downstream | Same trace ID across all services in a request chain |
| Redis outbox | All services → UtilityCoreService | Centralized error and audit log storage |

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

When ProfileCoreService calls WalletCoreService and it fails:

```json
// Client called: POST /api/v1/customers (ProfileCoreService)
// ProfileCoreService called: POST /api/v1/customer-wallets (WalletCoreService)
// WalletCoreService returned 400 with WALLET_SUSPENDED

// Client receives from ProfileCoreService:
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

The error code `5002` (WalletCoreService range) tells the client exactly which service produced the error, even though the response came from ProfileCoreService.

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
Client → ProfileCoreService → WalletCoreService → SecurityCoreService
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

All services publish errors asynchronously to UtilityCoreService via per-service Redis queues:

```
ProfileCoreService   → wep:outbox:profile
SecurityCoreService  → wep:outbox:security
TransactionCoreService → wep:outbox:transaction
WalletCoreService    → wep:outbox:wallet
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
        ServiceName = "ProfileCoreService",
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

### Consumer Side (UtilityCoreService)

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

Once processed, the error is stored in UtilityCoreService's `error_log` table and queryable via:

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
1. Client sends POST /api/v1/customers to ProfileCoreService
   → CorrelationIdMiddleware generates: abc-123

2. ProfileCoreService calls WalletCoreService to create customer wallet
   → CorrelationIdDelegatingHandler attaches X-Correlation-Id: abc-123
   → X-Tenant-Id: 11111111-... attached

3. WalletCoreService fails with INSUFFICIENT_BALANCE
   → Returns 400 { errorCode: "INSUFFICIENT_BALANCE", correlationId: "abc-123" }
   → Publishes error to wep:outbox:wallet with correlationId: abc-123

4. ProfileCoreService's WalletServiceClient deserializes the error
   → Re-throws as DomainException(5001, "INSUFFICIENT_BALANCE", ...)
   → GlobalExceptionHandlerMiddleware catches it
   → Returns 400 to client with correlationId: abc-123
   → Publishes error to wep:outbox:profile with correlationId: abc-123

5. UtilityCoreService's OutboxProcessor picks up both error events
   → Stores two error_log entries, both with correlationId: abc-123

6. Developer queries: GET /api/v1/error-logs?correlationId=abc-123
   → Sees the full chain:
     - WalletCoreService: INSUFFICIENT_BALANCE (original error)
     - ProfileCoreService: INSUFFICIENT_BALANCE (propagated error)
```

---

Previous: [Exception Handling Middleware](./ERROR_MANAGEMENT_MIDDLEWARE.md) · Next: [Live Demo Guide](./ERROR_MANAGEMENT_DEMO.md)
