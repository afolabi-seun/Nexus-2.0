# API Responses

The `ApiResponse<T>` envelope, pagination, correlation ID flow, and HTTP status code determination.

## ApiResponse Envelope

Every API response is wrapped in `ApiResponse<T>`, defined in `{Service}.Application/DTOs/ApiResponse.cs`. Both success and error responses use the same shape.

```json
{
  "responseCode": "00",
  "responseDescription": "Request successful",
  "success": true,
  "data": { ... },
  "errorCode": null,
  "errorValue": null,
  "message": "Organization created successfully.",
  "correlationId": "a1b2c3d4e5f6",
  "errors": null
}
```

### Fields

| Field | Type | Success | Error | Description |
|-------|------|---------|-------|-------------|
| `responseCode` | string | `"00"` | `"06"`, `"07"`, etc. | Two-digit category code (see [ERROR_CODES.md](ERROR_CODES.md)) |
| `responseDescription` | string | `"Request successful"` | Error description | Human-readable status |
| `success` | bool | `true` | `false` | Quick check for clients |
| `data` | T? | Response payload | `null` | The actual response data |
| `errorCode` | string? | `null` | `"ORGANIZATION_NAME_DUPLICATE"` | Machine-readable error code |
| `errorValue` | int? | `null` | `3005` | Numeric error identifier |
| `message` | string? | Optional success message | Error message | Context for the response |
| `correlationId` | string? | Set by middleware | Set by middleware | Request trace ID |
| `errors` | ErrorDetail[]? | `null` | Validation errors | Field-level validation failures |

### Factory Methods

```csharp
// Success
ApiResponse<object>.Ok(data, "Organization created successfully.")

// Error (used internally by middleware)
ApiResponse<object>.Fail(3005, "ORGANIZATION_NAME_DUPLICATE", "An organization with this name already exists.")

// Validation error
ApiResponse<object>.ValidationFail("Validation failed.", errors)
```

## Controller Usage

Controllers use `ToActionResult()` extension method to convert `ApiResponse<T>` to an `IActionResult`:

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken ct)
{
    var result = await _organizationService.CreateAsync(request, ct);
    return ApiResponse<object>.Ok(result, "Organization created successfully.").ToActionResult(HttpContext, 201);
}
```

The second parameter (`201`) sets the success HTTP status code. If omitted, defaults to `200`.

For errors, controllers don't handle them — the service layer throws `DomainException` and the middleware handles the response.

## ToActionResult Extension

Located at `{Service}.Api/Extensions/ApiResponseExtensions.cs`:

1. Null guard — returns 500 if response is null
2. Injects `CorrelationId` from `HttpContext.Items`
3. Error path — determines HTTP status from `ErrorCode` (ignores custom status code)
4. Success path — uses provided status code or defaults to 200

## HTTP Status Code Determination

`DetermineStatusCodeFromErrorCode()` maps error codes to HTTP status codes:

| Error Code Pattern | HTTP Status | Meaning |
|-------------------|-------------|---------|
| `INVALID_CREDENTIALS`, `TOKEN_REVOKED`, `REFRESH_TOKEN_REUSE`, `SESSION_EXPIRED` | 401 | Unauthorized |
| `INSUFFICIENT_PERMISSIONS`, `DEPARTMENT_ACCESS_DENIED`, `ORGANIZATION_MISMATCH` | 403 | Forbidden |
| `*_NOT_FOUND` | 404 | Not Found |
| `*_ALREADY_EXISTS`, `*_DUPLICATE`, `*_CONFLICT` | 409 | Conflict |
| `ACCOUNT_LOCKED` | 423 | Locked |
| `RATE_LIMIT_EXCEEDED` | 429 | Too Many Requests |
| `INTERNAL_ERROR` | 500 | Internal Server Error |
| `PAYMENT_PROVIDER_ERROR` | 502 | Bad Gateway |
| `SERVICE_UNAVAILABLE` | 503 | Service Unavailable |
| Everything else | 400 | Bad Request |

## PaginatedResponse

List endpoints return data wrapped in `PaginatedResponse<T>` inside the `ApiResponse.Data` field:

```json
{
  "responseCode": "00",
  "success": true,
  "data": {
    "data": [ ... ],
    "totalCount": 47,
    "page": 2,
    "pageSize": 20,
    "totalPages": 3
  }
}
```

Defined in `{Service}.Application/DTOs/PaginatedResponse.cs`:

```csharp
public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

## Correlation ID Flow

The `CorrelationId` traces a request across all services it touches.

### Generation

`CorrelationIdMiddleware` (runs early in the pipeline):
1. Checks for incoming `X-Correlation-Id` header
2. If absent, generates a new one via `Guid.NewGuid().ToString("N")`
3. Stores in `HttpContext.Items["CorrelationId"]`
4. Adds to response headers via `OnStarting` callback

### Injection into Responses

`ToActionResult()` reads `HttpContext.Items["CorrelationId"]` and sets it on the `ApiResponse`.

`GlobalExceptionHandlerMiddleware` does the same for error responses.

### Propagation to Downstream Services

`CorrelationIdDelegatingHandler` (registered on all typed HTTP clients) forwards the correlation ID to inter-service calls:

```csharp
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
{
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationIdStr);
    if (httpContext?.Items.TryGetValue("OrganizationId", out var orgId) == true)
        request.Headers.TryAddWithoutValidation("X-Organization-Id", orgIdStr);
    return await base.SendAsync(request, ct);
}
```

This means a single user request that touches SecurityService → ProfileService → UtilityService will have the same `CorrelationId` in all three services' logs.

### Error Response Example

```json
{
  "responseCode": "06",
  "responseDescription": "An organization with this name already exists.",
  "success": false,
  "data": null,
  "errorCode": "ORGANIZATION_NAME_DUPLICATE",
  "errorValue": 3005,
  "message": "An organization with this name already exists.",
  "correlationId": "a1b2c3d4e5f6",
  "errors": null
}
```

## Related Docs

- [ERROR_HANDLING.md](ERROR_HANDLING.md) — How errors produce these responses
- [ERROR_CODES.md](ERROR_CODES.md) — Error code registry and response code mapping
- [VALIDATION.md](VALIDATION.md) — How validation errors populate the `errors` array
