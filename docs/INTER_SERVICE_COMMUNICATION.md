# Inter-Service Communication

## Overview

WEP services communicate through two channels:

| Channel | Pattern | Use Case |
|---------|---------|----------|
| Synchronous HTTP | Typed service clients with Polly resilience | Real-time operations (wallet creation, user lookup, password sync) |
| Asynchronous Redis outbox | LPUSH → RPOPLPUSH consumer | Fire-and-forget events (audit logs, error logs, notifications) |

Both channels propagate `correlationId` and `tenantId` for end-to-end tracing and tenant isolation.

---

## Service Communication Map

```
                    ┌──────────────────┐
                    │  UtilityCoreService │
                    │  (error codes,     │
                    │   audit, notifs)   │
                    └────────▲───────────┘
                             │
              IUtilityServiceClient (all 4 services)
              Redis outbox (all 4 services)
                             │
┌──────────────┐    ┌───────┴────────┐    ┌──────────────────┐
│  Security    │◄──►│   Profile      │───►│   Wallet         │
│  CoreService │    │   CoreService  │    │   CoreService    │
└──────────────┘    └───────┬────────┘    └──────────────────┘
                            │                      ▲
                    ┌───────┴────────┐             │
                    │  Transaction   │─────────────┘
                    │  CoreService   │
                    └────────────────┘
```

| Caller | Downstream | Interface | Operations |
|--------|-----------|-----------|------------|
| Profile → Security | `ISecurityServiceClient` (via HTTP) | Credential generation, password sync |
| Security → Profile | `IProfileServiceClient` | User lookup by identity/username/ID, password update |
| Profile → Wallet | `IWalletServiceClient` | Customer wallet creation, KYC limit updates, cascade suspend/reactivate |
| Transaction → Wallet | `IWalletServiceClient` | Balance checks, debits, credits, hold operations |
| Security → Wallet | `IWalletServiceClient` | Spending limit checks during PIN verification |
| All → Utility | `IUtilityServiceClient` | Error code registry fetch |
| All → Utility | Redis outbox | Audit logs, error logs, notifications |

---

## Typed Service Clients

Each inter-service dependency is abstracted behind a typed interface with compile-time contract safety.

### Interface Pattern

```csharp
// ProfileCoreService.Api/Infrastructure/Services/ServiceClients/IWalletServiceClient.cs
public interface IWalletServiceClient
{
    Task<CreateCustomerWalletResponse> CreateCustomerWalletAsync(
        CreateCustomerWalletRequest request, CancellationToken cancellationToken = default);

    Task UpdateKycLimitsAsync(Guid custId, Guid tenantId, string kycLevel,
        CancellationToken cancellationToken = default);

    Task CascadeSuspendAsync(Guid smeId, Guid tenantId,
        CancellationToken cancellationToken = default);

    Task CascadeReactivateAsync(Guid smeId, Guid tenantId,
        CancellationToken cancellationToken = default);
}
```

### Contract DTOs

Each service defines typed request/response DTOs in its `Contracts/` folder:

```csharp
// ProfileCoreService.Api/Contracts/CreateCustomerWalletRequest.cs
public class CreateCustomerWalletRequest
{
    public Guid CustId { get; set; }
    public Guid TenantId { get; set; }
    public string CustomerWalletName { get; set; } = string.Empty;
}

// ProfileCoreService.Api/Contracts/CreateCustomerWalletResponse.cs
public class CreateCustomerWalletResponse
{
    public Guid Id { get; set; }
    public string WalletName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

These are compile-time contracts — if the downstream service changes its response shape, the caller gets a build error, not a runtime deserialization failure.

### Implementation Pattern

Every service client follows the same structure:

```csharp
public class WalletServiceClient : IWalletServiceClient
{
    private const string DownstreamServiceName = "WalletCoreService";

    public async Task<CreateCustomerWalletResponse> CreateCustomerWalletAsync(
        CreateCustomerWalletRequest request, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/api/v1/customer-wallets";
        var client = _httpClientFactory.CreateClient(DownstreamServiceName);
        await AttachHeadersAsync(client);  // JWT + X-Tenant-Id

        var content = new StringContent(
            JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        var response = await client.PostAsync(endpoint, content, cancellationToken);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
            await HandleDownstreamErrorAsync(response, endpoint, sw.ElapsedMilliseconds);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<CreateCustomerWalletResponse>>(json);

        return apiResponse?.Data
            ?? throw new DomainException(
                ErrorCodes.ServiceUnavailableValue, ErrorCodes.ServiceUnavailable,
                $"{DownstreamServiceName} returned a null response for {endpoint}.",
                HttpStatusCode.ServiceUnavailable);
    }
}
```

Key behaviors:
- `AttachHeadersAsync` — adds service JWT and X-Tenant-Id header
- `Stopwatch` — logs elapsed time for performance monitoring
- `HandleDownstreamErrorAsync` — deserializes downstream errors, re-throws as local `DomainException` (see [Error Propagation](./ERROR_MANAGEMENT_PROPAGATION.md))
- Null response guard — throws `SERVICE_UNAVAILABLE` if deserialization returns null

---

## Service-to-Service JWT Authentication

Services authenticate to each other using stateless JWTs issued by `IServiceAuthService`.

### Token Structure

```json
{
  "sub": "ProfileCoreService",
  "jti": "a1b2c3d4-...",
  "serviceId": "ProfileCoreService",
  "serviceName": "ProfileCoreService",
  "tokenType": "service",
  "exp": 1718500000,
  "iss": "WEP",
  "aud": "WEP"
}
```

Key differences from user JWTs:
- `tokenType: "service"` (vs `"user"`)
- `serviceId` claim instead of `sub` with user ID
- No `TenantId`, `RoleId`, or `RoleName` claims — tenant context comes from `X-Tenant-Id` header

### Token Lifecycle

```
1. Service client calls AttachHeadersAsync()
2. GetServiceTokenAsync() checks local cache (30s buffer before expiry)
3. Cache miss → IServiceAuthService.IssueServiceTokenAsync()
   a. Check Redis cache (wep:service_token:{serviceId}, 23h TTL)
   b. Cache miss → Generate new JWT, cache in Redis
4. Token attached as Authorization: Bearer header
```

### ACL Enforcement

`ServiceAuthService` maintains an in-memory ACL:

```csharp
private static readonly Dictionary<string, string[]> ServiceAcl = new()
{
    ["ProfileCoreService"]     = ["*"],
    ["WalletCoreService"]      = ["*"],
    ["TransactionCoreService"] = ["*"],
    ["UtilityCoreService"]     = ["*"],
    ["SecurityCoreService"]    = ["*"]
};
```

Currently all services have wildcard access. The ACL can be tightened to restrict which endpoints each service can call.

### Token Caching

| Layer | Key | TTL | Purpose |
|-------|-----|-----|---------|
| Local (in-memory) | Per-client instance | Until 30s before expiry | Avoid Redis round-trip |
| Redis | `wep:service_token:{serviceId}` | 23 hours | Shared across service instances |

Token lifetime is configurable via `SERVICE_TOKEN_LIFETIME_MINUTES` in `.env` (default: 60).

---

## Header Propagation

Every outgoing inter-service call attaches three headers:

### Authorization

```
Authorization: Bearer <service-jwt>
```

### X-Tenant-Id

```
X-Tenant-Id: 11111111-1111-1111-1111-111111111111
```

Service JWTs don't carry tenant claims. The caller reads `TenantId` from `HttpContext.Items` (set by the original user's JWT) and propagates it via header. The downstream service's `TenantScopeMiddleware` reads this header for service-auth tokens and sets it on the DbContext.

### X-Correlation-Id

```
X-Correlation-Id: da490d7b-73a9-4f1f-8580-e7a391607286
```

Propagated by `CorrelationIdDelegatingHandler`, registered on all `HttpClient` instances:

```csharp
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string
                            ?? Guid.NewGuid().ToString();
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        return base.SendAsync(request, cancellationToken);
    }
}
```

---

## Polly Resilience Policies

All `HttpClient` instances are configured with three Polly policies:

```csharp
services.AddHttpClient("WalletCoreService", client =>
{
    client.BaseAddress = new Uri(appSettings.WalletCoreServiceBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
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
| Timeout | 10 seconds | Aborts if downstream doesn't respond |
| Retry | 3 retries, exponential backoff (1s, 2s, 4s) | Retries on 5xx and network errors |
| Circuit Breaker | 5 failures → 30s open | Stops calling downstream after repeated failures |

### Failure Sequence

```
Call 1: 503 → retry after 1s
Call 2: 503 → retry after 2s
Call 3: 503 → retry after 4s
Call 4: 503 → Polly gives up → HandleDownstreamErrorAsync → DomainException(SERVICE_UNAVAILABLE)

After 5 consecutive failures:
→ Circuit breaker opens for 30s
→ Subsequent calls fail immediately (BrokenCircuitException)
→ After 30s, circuit half-opens → next call is a test
→ Success → circuit closes; Failure → circuit re-opens
```

---

## Redis Outbox (Asynchronous)

Fire-and-forget events are published to Redis lists and consumed by UtilityCoreService.

### Publisher Side

Every service has an `OutboxService` that does a simple `LPUSH`:

```csharp
public class OutboxService : IOutboxService
{
    public async Task PublishAsync(string queueName, string jsonPayload)
    {
        var db = _redis.GetDatabase();
        await db.ListLeftPushAsync(queueName, jsonPayload);
    }
}
```

### Queue Names

| Service | Queue Key |
|---------|-----------|
| ProfileCoreService | `wep:outbox:profile` |
| SecurityCoreService | `wep:outbox:security` |
| TransactionCoreService | `wep:outbox:transaction` |
| WalletCoreService | `wep:outbox:wallet` |

### Envelope Format

```json
{
  "type": "audit",
  "payload": {
    "tenantId": "11111111-...",
    "serviceName": "ProfileCoreService",
    "action": "CustomerCreated",
    "entityType": "Customer",
    "entityId": "a1b2c3d4-...",
    "userId": "e5f6a7b8-...",
    "correlationId": "da490d7b-..."
  },
  "timestamp": "2025-06-15T10:30:00Z",
  "id": "unique-message-id"
}
```

Event types:

| Type | Payload | Destination |
|------|---------|-------------|
| `audit` | Action, entity type/ID, user ID | `audit_log` table |
| `error` | Error code, message, stack trace | `error_log` table |
| `notification` | Notification type, recipient, channels | Notification dispatch → `notification_log` table |
| `device_registration` | Device name, user ID, IP | Forwarded to ProfileCoreService |

### Consumer Side (UtilityCoreService)

`OutboxProcessorHostedService` runs as a background service, polling all 4 queues:

```
Poll interval → for each queue:
  1. RPOPLPUSH queue → queue:processing  (atomic move)
  2. Deserialize envelope
  3. Route by type:
     - "audit"        → IAuditLogService.AddAsync()
     - "error"        → IErrorLogService.AddAsync()
     - "notification" → INotificationDispatchService.DispatchAsync()
     - "device_registration" → Forward to ProfileCoreService
  4. Success → LREM queue:processing (remove)
  5. Failure → Move back to original queue for retry
```

The `RPOPLPUSH` pattern ensures at-least-once delivery — if the consumer crashes mid-processing, the message remains in the processing queue and can be recovered.

---

## Service Registration

All service clients and HTTP clients are registered in `ApplicationServiceExtensions.cs`:

```csharp
// HTTP client with Polly policies
services.AddHttpClient("WalletCoreService", client => { ... })
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddPolicyHandler(...)
    .AddTransientHttpErrorPolicy(...)
    .AddTransientHttpErrorPolicy(...);

// Typed service client
services.AddScoped<IWalletServiceClient, WalletServiceClient>();

// Service auth
services.AddScoped<IServiceAuthService, ServiceAuthService>();

// Outbox publisher
services.AddScoped<IOutboxService, OutboxService>();

// Correlation ID handler
services.AddTransient<CorrelationIdDelegatingHandler>();
```

---

## Summary: Adding a New Inter-Service Call

1. **Contract DTOs** — Create request/response classes in `Contracts/` folder of the caller
2. **Interface** — Add method to the typed service client interface (e.g. `IWalletServiceClient`)
3. **Implementation** — Follow the existing pattern: `AttachHeadersAsync` → HTTP call → `HandleDownstreamErrorAsync` → deserialize
4. **Registration** — HTTP client should already be registered; if new service, add `AddHttpClient` with Polly policies
5. **Test** — Mock the interface in unit tests; integration tests call the real service
