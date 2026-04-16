# Inter-Service Communication

Service clients, Polly resilience policies, correlation ID propagation, error propagation, and the Redis outbox pattern.

## Overview

Services communicate via REST over HTTP. Each service has typed HTTP clients for the services it depends on:

| Calling Service | Calls | Client Class |
|----------------|-------|-------------|
| SecurityService | ProfileService, UtilityService | `ProfileServiceClient`, `UtilityServiceClient` |
| ProfileService | SecurityService, UtilityService | `SecurityServiceClient`, `UtilityServiceClient` |
| WorkService | ProfileService, SecurityService, UtilityService | `ProfileServiceClient`, `SecurityServiceClient`, `UtilityServiceClient` |
| BillingService | ProfileService, SecurityService | `ProfileServiceClient`, `SecurityServiceClient` |
| UtilityService | — (source of truth, no outbound calls) | — |

## Typed HTTP Clients

Registered in `{Service}.Infrastructure/Configuration/DependencyInjection.cs` with `IHttpClientFactory`:

```csharp
services.AddHttpClient("ProfileService", c =>
    c.BaseAddress = new Uri(appSettings.ProfileServiceUrl))
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));
```

## Polly Resilience Policies

Every inter-service HTTP client has three Polly policies applied in order:

| Policy | Configuration | Behavior |
|--------|--------------|----------|
| Retry | 3 attempts, exponential backoff (2s, 4s, 8s) | Retries on transient HTTP errors (5xx, network failures) |
| Circuit Breaker | 5 failures → open for 30 seconds | Stops calling a failing service, fails fast |
| Timeout | 10 seconds | Prevents hanging on slow responses |

These are applied to all typed HTTP clients uniformly.

## CorrelationIdDelegatingHandler

Registered as an HTTP message handler on every typed client. Forwards context from the incoming request to outbound inter-service calls:

```csharp
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
{
    var httpContext = _httpContextAccessor.HttpContext;

    // Forward correlation ID
    if (httpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationIdStr);

    // Forward organization ID
    if (httpContext?.Items.TryGetValue("OrganizationId", out var orgId) == true)
        request.Headers.TryAddWithoutValidation("X-Organization-Id", orgIdStr);

    return await base.SendAsync(request, ct);
}
```

This ensures:
- The same `CorrelationId` appears in logs across all services for a single user request
- The `OrganizationId` propagates for tenant-scoped operations

## Service-to-Service Authentication

Each service client attaches a service JWT token to outbound requests. The token is cached in memory with a 30-second expiry buffer:

```csharp
private async Task AttachServiceTokenAsync(HttpClient client)
{
    if (_cachedToken is not null && DateTime.UtcNow.AddSeconds(30) < _tokenExpiry)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _cachedToken);
        return;
    }

    var result = await _serviceTokenService.IssueTokenAsync("ProfileService", "ProfileService");
    _cachedToken = result.Token;
    _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresInSeconds);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _cachedToken);
}
```

See [AUTHENTICATION_AND_SECURITY.md](AUTHENTICATION_AND_SECURITY.md) for full service token details.

## Error Propagation

When a downstream service returns an error, the calling service's client deserializes the `ApiResponse` and re-throws it as a `DomainException`:

```csharp
private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken ct)
{
    var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(ct);
    if (errorResponse is not null && !string.IsNullOrEmpty(errorResponse.ErrorCode))
    {
        throw new DomainException(
            errorResponse.ErrorValue ?? ErrorCodes.ServiceUnavailableValue,
            errorResponse.ErrorCode,
            errorResponse.Message ?? "Downstream service error.",
            response.StatusCode);
    }
    throw new ServiceUnavailableException("Downstream service is unavailable.");
}
```

This means:
- A `MEMBER_NOT_FOUND` from ProfileService becomes a `DomainException` with the same error code in SecurityService
- The original error code, message, and HTTP status propagate back to the client
- If the response can't be deserialized, it falls back to `ServiceUnavailableException` (503)

## Redis Outbox Pattern

Services publish audit events and notifications asynchronously via Redis lists. This decouples the publishing service from UtilityService availability.

### Publishing (All Services)

Each service has an `OutboxService` that pushes JSON messages to a Redis list:

```
Key: outbox:{service}    (e.g., outbox:profile, outbox:work, outbox:billing)
Operation: LPUSH
```

```csharp
await db.ListLeftPushAsync("outbox:profile", serializedMessage);
```

### Retry with Dead-Letter Queue

Publishing retries 3 times with exponential backoff (1s, 2s, 4s). If all retries fail, the message is moved to a dead-letter queue:

```
DLQ Key: dlq:{service}   (e.g., dlq:profile)
```

```csharp
for (var attempt = 0; attempt < MaxRetries; attempt++)
{
    try { await db.ListLeftPushAsync(QueueKey, serialized); return; }
    catch (RedisException) { await Task.Delay(BackoffSecondsPerRetry[attempt]); }
}
await db.ListLeftPushAsync(DlqKey, serialized);  // dead-letter
```

### Consuming (UtilityService)

UtilityService runs a background service that polls all outbox queues via `RPOP` and processes messages (audit log entries, notification dispatches).

### Message Shape

```json
{
  "messageType": "NotificationRequest",
  "action": "SprintStarted",
  "entityType": "Sprint",
  "entityId": "sprint-uuid",
  "organizationId": "org-uuid",
  "notificationType": "SprintStarted"
}
```

## Request Flow Example

A user creates a story in WorkService, which needs to validate the assignee via ProfileService:

```
Client → WorkService (POST /api/v1/stories)
  ├── CorrelationId: abc123 (generated by CorrelationIdMiddleware)
  ├── OrganizationId: org-uuid (from JWT via OrganizationScopeMiddleware)
  │
  ├── WorkService → ProfileService (GET /api/v1/team-members/{id})
  │     ├── X-Correlation-Id: abc123 (forwarded by CorrelationIdDelegatingHandler)
  │     ├── X-Organization-Id: org-uuid (forwarded)
  │     ├── Authorization: Bearer <service-token>
  │     └── Polly: retry 3x → circuit breaker → timeout 10s
  │
  ├── WorkService → Redis LPUSH outbox:work (audit event)
  │
  └── Response → Client (ApiResponse with correlationId: abc123)
```

## Related Docs

- [AUTHENTICATION_AND_SECURITY.md](AUTHENTICATION_AND_SECURITY.md) — Service token issuance and validation
- [API_RESPONSES.md](API_RESPONSES.md) — ApiResponse envelope and correlation ID
- [ERROR_HANDLING.md](ERROR_HANDLING.md) — How propagated errors are caught by middleware
