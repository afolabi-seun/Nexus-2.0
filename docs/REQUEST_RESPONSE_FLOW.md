# WEP Request-Response Flow Patterns

How requests flow through the platform layers, how each layer handles success and failure, and what the caller receives.

---

## Flow Overview

```
Client Request
    │
    ▼
┌─────────────────────────────────────────────┐
│  Middleware Pipeline                         │
│  CorrelationId → JWT → TokenBlacklist →     │
│  TenantScope → RateLimiter → ErrorHandler   │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  Controller                                  │
│  Extracts IDs from HttpContext.Items         │
│  Calls service method                        │
│  Returns result.ToActionResult()             │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│  Service                                     │
│  Business logic, validation                  │
│  Returns ServiceResult<T>                    │
│  Calls repositories + service clients        │
└──────────────────┬──────────────────────────┘
                   │
          ┌────────┴────────┐
          ▼                 ▼
┌──────────────┐   ┌──────────────────┐
│  Repository  │   │  Service Client  │
│  EF Core     │   │  HTTP to other   │
│  queries     │   │  services        │
└──────────────┘   └──────────────────┘
```

---

## 1. Controllers — Thin Delegation

Controllers do **no business logic**. They extract context from JWT claims, call a service method, and convert the result.

### Pattern

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _service.GetByIdAsync(GetTenantId(), id);
    return result.ToActionResult();
}
```

### How Context Is Extracted

| Source | Method | Example |
|--------|--------|---------|
| JWT → `HttpContext.Items["TenantId"]` | `GetTenantId()` | `Guid` |
| JWT → `HttpContext.Items["SmeId"]` | `GetSmeId()` | `Guid` |
| JWT → `HttpContext.Items["SmeUserId"]` | `GetSmeUserId()` | `Guid` |
| JWT → `HttpContext.Items["CustId"]` | `GetCustId()` | `Guid` |
| Route parameter | Method parameter | `Guid id` |
| Request body | `[FromBody]` | DTO |
| Query string | `[FromQuery]` | Filters |

### What Controllers Return

Controllers never construct `ApiResponse<T>` directly. `ToActionResult()` does it:

| ServiceResult State | HTTP Status | Response |
|---------------------|-------------|----------|
| `Ok(data)` | 200 | `{ success: true, data: {...} }` |
| `Created(data)` | 201 | `{ success: true, data: {...} }` |
| `NoContent()` | 204 | Empty |
| `NotFound(msg)` | 404 | `{ success: false, errorCode: "NOT_FOUND" }` |
| `Fail(code, msg)` | 400/422/etc | `{ success: false, errorCode: "..." }` |

### What Controllers Do NOT Do

- No try/catch (middleware handles exceptions)
- No response formatting (ServiceResult handles it)
- No business logic (services handle it)
- No direct repository calls

---

## 2. Services — Business Logic + ServiceResult\<T\>

Services contain all business logic and always return `ServiceResult<T>`. They never throw exceptions for expected failures — only for truly unexpected errors.

### Success Pattern

```csharp
public async Task<ServiceResult<CustomerResponse>> GetByIdAsync(Guid tenantId, Guid id)
{
    var entity = await _repo.FindByIdAsync(tenantId, id);
    if (entity == null)
        return ServiceResult<CustomerResponse>.NotFound($"Customer '{id}' not found.");

    return ServiceResult<CustomerResponse>.Ok(MapToResponse(entity));
}
```

### Failure Pattern (Expected)

```csharp
public async Task<ServiceResult<SubWalletResponse>> UpdateSpendingLimitAsync(...)
{
    var wallet = await GetEntityAsync(tenantId, subWalletId);

    // Business rule validation — returns failure, does NOT throw
    if (request.SpendingLimit.HasValue)
    {
        var parent = await _smeWalletRepo.FindByIdAsync(tenantId, wallet.SmeWalletId);
        if (parent?.GlobalSpendingLimit.HasValue == true &&
            request.SpendingLimit.Value > parent.GlobalSpendingLimit.Value)
            return ServiceResult<SubWalletResponse>.Fail(
                ErrorCodes.ValidationErrorValue, ErrorCodes.ValidationError,
                $"Spending limit cannot exceed SME global limit.",
                StatusCodes.Status422UnprocessableEntity);
    }

    var updated = await _repo.UpdateAsync(tenantId, subWalletId, w => { ... });
    return ServiceResult<SubWalletResponse>.Ok(MapToResponse(updated));
}
```

### Failure Pattern (Unexpected — DomainException)

For violations that should never happen in normal flow:

```csharp
private async Task<SmeSubWallet> GetEntityAsync(Guid tenantId, Guid id)
{
    return await _repo.FindByIdAsync(tenantId, id)
        ?? throw new KeyNotFoundException($"Sub-wallet '{id}' not found.");
}
```

`KeyNotFoundException` is caught by `GlobalExceptionHandlerMiddleware` → 404 response.

### Parallel Service Calls

Services use `Task.WhenAll` for independent calls:

```csharp
public async Task<ServiceResult<AdminDashboardResponse>> GetAdminDashboardAsync(...)
{
    var customerCountTask = _customerRepository.CountAsync(tenantId);
    var walletTask = _walletServiceClient.GetSmeWalletDashboardAsync(smeId);
    var txnTask = _transactionServiceClient.GetAdminDashboardDataAsync();

    await Task.WhenAll(customerCountTask, walletTask, txnTask);

    return ServiceResult<AdminDashboardResponse>.Ok(new AdminDashboardResponse
    {
        TotalCustomers = customerCountTask.Result,
        AvailableWalletBalance = walletTask.Result?.Balance,
        TransactionsToday = txnTask.Result?.Today ?? 0,
        // ...
    });
}
```

### Audit Logging

Services log significant actions via the outbox (async, non-blocking):

```csharp
await _audit.LogAsync("CustomerCreated", custId.ToString(),
    $"Customer '{firstName} {lastName}' created",
    tenantId: tenantId);
```

---

## 3. Inter-Service Communication — Service Clients

Service clients make HTTP calls to other services. They handle failures gracefully — a downstream service being unavailable should not crash the caller.

### Pattern: Graceful Degradation (Return Null)

Used when the data is optional (e.g., dashboard wallet balance):

```csharp
public async Task<decimal?> GetSmeWalletBalanceAsync(Guid smeId, CancellationToken ct = default)
{
    try
    {
        var client = _httpClientFactory.CreateClient("WalletCoreService");
        await AttachHeadersAsync(client);
        var response = await client.GetAsync($"/api/v1/sme-wallets/{smeId}", ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        // Parse and return...
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to fetch SME wallet balance for {SmeId}", smeId);
        return null;  // Caller handles null gracefully
    }
}
```

### Pattern: Propagate Downstream Error (Throw DomainException)

Used when the call is required (e.g., creating a customer wallet):

```csharp
public async Task<CreateCustomerWalletResponse> CreateCustomerWalletAsync(...)
{
    var response = await client.PostAsync(endpoint, content, ct);

    if (!response.IsSuccessStatusCode)
        await HandleDownstreamErrorAsync(response, endpoint);  // Throws DomainException

    var apiResponse = JsonSerializer.Deserialize<ApiResponse<CreateCustomerWalletResponse>>(json);
    return apiResponse?.Data ?? throw new DomainException(...);
}
```

### Pattern: Non-Fatal (Try/Catch, Log, Continue)

Used when the call is best-effort (e.g., auto-creating SME wallet during onboarding):

```csharp
try
{
    await _walletServiceClient.CreateSmeWalletAsync(sme.SmeId, sme.TenantId, walletName);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to auto-create SME wallet during onboarding");
    await _auditLog.LogAsync("SmeWalletAutoCreateFailed", ...);
    // Onboarding continues — wallet can be created later
}
```

### Authentication for Service-to-Service Calls

All inter-service calls use service JWT tokens:

```csharp
private async Task AttachHeadersAsync(HttpClient client)
{
    var token = await GetServiceTokenAsync();  // Cached, auto-refreshed
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString();
    if (!string.IsNullOrEmpty(tenantId))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", tenantId);
}
```

### Resilience (Polly Policies)

All HTTP clients have retry + circuit breaker:

```
Timeout: 10 seconds
Retry: 3 attempts with exponential backoff (1s, 2s, 4s)
Circuit Breaker: Opens after 5 failures, stays open 30 seconds
```

---

## 4. Repositories — Data Access

Repositories handle EF Core queries and return entities. They never return DTOs or ServiceResult.

### Pattern: TenantScopedRepository\<T\>

All queries are automatically scoped to the tenant via a global query filter:

```csharp
public class CustomerRepository : TenantScopedRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(ProfileDbContext context) : base(context, x => x.CustId) { }

    public async Task<Customer?> FindByPhoneAsync(Guid tenantId, string phoneNo)
    {
        SetTenant(tenantId);  // Sets the tenant filter
        return await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNo == phoneNo);
    }
}
```

### What Repositories Return

| Method | Returns | On Not Found |
|--------|---------|-------------|
| `FindByIdAsync` | `T?` | `null` |
| `FindAllAsync` | `List<T>` | Empty list |
| `CreateAsync` | `T` (created entity) | N/A |
| `UpdateAsync` | `T` (updated entity) | Throws `KeyNotFoundException` |
| `SoftDeleteAsync` | `void` | Throws `KeyNotFoundException` |
| Custom queries | `T?`, `List<T>`, `int`, `bool` | `null`, empty, 0, false |

### Constraint Violation Handling

`TenantScopedRepository` catches PostgreSQL constraint violations and converts them to `DomainException`:

| SQL State | PostgreSQL Error | Mapped To | HTTP |
|-----------|-----------------|-----------|------|
| `23505` | Unique violation | `CONFLICT` | 409 |
| `23503` | Foreign key violation | `NOT_FOUND` | 400 |

### Reference Code Auto-Generation

Entities implementing `IReferenceCodeEntity` get a reference code auto-generated on `CreateAsync`:

```
Format: {PREFIX}-{YYYYMMDD}-{8-char-hex}
Example: CUS-20260414-A1B2C3D4
```

---

## 5. The ApiResponse\<T\> Envelope

Every HTTP response uses the same envelope:

### Success Response (200/201)

```json
{
  "responseCode": "00",
  "responseDescription": "Request successful",
  "success": true,
  "data": { ... },
  "correlationId": "abc-123"
}
```

With null suppression enabled, null fields (`errorCode`, `errors`, etc.) are omitted.

### Error Response (4xx/5xx)

```json
{
  "responseCode": "07",
  "responseDescription": "Not found",
  "success": false,
  "errorCode": "NOT_FOUND",
  "errorValue": 3017,
  "message": "Customer not found.",
  "correlationId": "abc-123"
}
```

### Validation Error (422)

```json
{
  "responseCode": "96",
  "responseDescription": "Validation error",
  "success": false,
  "errorCode": "VALIDATION_ERROR",
  "errorValue": 1000,
  "message": "Validation error",
  "data": [
    { "field": "PhoneNo", "message": "PhoneNo is required." }
  ],
  "correlationId": "abc-123"
}
```

### Response Code Mapping

| Code | Category | Example Error Codes |
|------|----------|-------------------|
| `00` | Success | — |
| `01` | Authentication failed | `INVALID_CREDENTIALS`, `TOKEN_REVOKED` |
| `02` | Account locked | `ACCOUNT_LOCKED`, `TRANSACTION_PIN_LOCKED` |
| `03` | Authorization denied | `INSUFFICIENT_PERMISSIONS`, `TENANT_MISMATCH` |
| `06` | Duplicate/conflict | `CUSTOMER_ALREADY_EXISTS`, `DUPLICATE_PRIMARY_WALLET` |
| `07` | Not found | `TRANSACTION_NOT_FOUND`, `HOLD_NOT_FOUND` |
| `08` | Limit exceeded | `SPENDING_LIMIT_EXCEEDED`, `MAX_DEVICES_REACHED` |
| `09` | Wallet status error | `WALLET_SUSPENDED`, `INSUFFICIENT_BALANCE` |
| `96` | Validation error | `VALIDATION_ERROR` |
| `97` | Rate limit | `RATE_LIMIT_EXCEEDED` |
| `98` | Internal error | `INTERNAL_ERROR` |

---

## 6. Error Propagation Summary

| Layer | How Errors Are Handled | What Happens |
|-------|----------------------|-------------|
| **FluentValidation** | Auto-validation pipeline | 422 with field errors (before controller runs) |
| **Auth Attributes** | `[PlatformAdmin]`, `[StaffOnly]`, `[CustomerOnly]` | 401/403 (before controller runs) |
| **Controller** | No error handling — delegates to service | Passes through |
| **Service** | Returns `ServiceResult.Fail(...)` for expected errors | Controller converts to HTTP response |
| **Service** | Throws `DomainException` for business rule violations | Caught by `GlobalExceptionHandlerMiddleware` |
| **Repository** | Returns `null` for not found | Service decides how to handle |
| **Repository** | Throws `KeyNotFoundException` on update/delete miss | Caught by middleware → 404 |
| **Repository** | Catches constraint violations → `DomainException` | Caught by middleware → 409 |
| **Service Client** | Returns `null` for optional data | Service uses default/fallback |
| **Service Client** | Throws `DomainException` for required data | Caught by middleware |
| **Service Client** | Logs + swallows for best-effort calls | Operation continues |
| **Middleware** | `GlobalExceptionHandlerMiddleware` catches all unhandled | Structured JSON error response |
