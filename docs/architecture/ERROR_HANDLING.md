# Error Handling

How errors flow from business logic through the middleware pipeline to the API response.

## Error Flow Overview

```
Controller → Service → throws DomainException
                              ↓
              GlobalExceptionHandlerMiddleware catches it
                              ↓
              ErrorCodeResolverService resolves response code
                              ↓
              ApiResponse<T> envelope returned to client
```

Every error in Nexus follows this path. Services never return error objects — they throw typed exceptions. The middleware catches everything and produces a consistent `ApiResponse<T>` envelope.

## DomainException Base Class

Every service has an identical base exception in `{Service}.Domain/Exceptions/DomainException.cs`:

```csharp
public class DomainException : Exception
{
    public int ErrorValue { get; }        // Numeric code (e.g. 3005)
    public string ErrorCode { get; }      // String code (e.g. "ORGANIZATION_NAME_DUPLICATE")
    public HttpStatusCode StatusCode { get; }  // HTTP status (e.g. 409)
}
```

Constructor: `DomainException(int errorValue, string errorCode, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)`

## Typed Exception Pattern

Each business rule violation has its own exception class. Every typed exception:
- Extends `DomainException`
- Hardcodes its `ErrorValue` and `ErrorCode` from the service's `ErrorCodes` static class
- Hardcodes the appropriate `HttpStatusCode`
- Provides a default message

Example — `OrganizationNameDuplicateException`:

```csharp
public class OrganizationNameDuplicateException : DomainException
{
    public OrganizationNameDuplicateException(string message = "An organization with this name already exists.")
        : base(ErrorCodes.OrganizationNameDuplicateValue, ErrorCodes.OrganizationNameDuplicate, message, HttpStatusCode.Conflict)
    { }
}
```

Usage in service layer:

```csharp
var existing = await _deptRepo.GetByNameAsync(organizationId, req.DepartmentName, ct);
if (existing is not null)
    throw new DepartmentNameDuplicateException();
```

## Exception Hierarchy Per Service

| Service | Exception Count | Examples |
|---------|----------------|----------|
| SecurityService | 23 | `InvalidCredentialsException`, `AccountLockedException`, `RefreshTokenReuseException`, `OtpExpiredException` |
| ProfileService | 27 | `OrganizationNameDuplicateException`, `MemberNotFoundException`, `LastOrgAdminCannotDeactivateException` |
| WorkService | 55 | `StoryNotFoundException`, `InvalidStoryTransitionException`, `SprintNotInPlanningException`, `TimerAlreadyActiveException` |
| UtilityService | 15 | `AuditLogImmutableException`, `ErrorCodeDuplicateException`, `NotificationDispatchFailedException` |
| BillingService | 16 | `SubscriptionAlreadyExistsException`, `InvalidUpgradePathException`, `FeatureNotAvailableException` |

## Special Exception: RateLimitExceededException

This subclass adds a `RetryAfterSeconds` property. The middleware detects it and sets the `Retry-After` HTTP header:

```csharp
public class RateLimitExceededException : DomainException
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds, string message = "Rate limit exceeded.")
        : base(ErrorCodes.RateLimitExceededValue, ErrorCodes.RateLimitExceeded, message, (HttpStatusCode)429)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
```

Middleware handling:

```csharp
if (ex is RateLimitExceededException rle)
{
    context.Response.Headers["Retry-After"] = rle.RetryAfterSeconds.ToString();
}
```

## GlobalExceptionHandlerMiddleware

Located at `{Service}.Api/Middleware/GlobalExceptionHandlerMiddleware.cs` in all 5 services. Catches three exception types in order:

```csharp
try
{
    await _next(context);
}
catch (DomainException ex)        // 1. Business rule violations → typed error response
{
    await HandleDomainExceptionAsync(context, ex);
}
catch (DbUpdateException ex)      // 2. PostgreSQL constraint violations → 409 Conflict
{
    await HandleDbUpdateExceptionAsync(context, ex);
}
catch (Exception ex)              // 3. Everything else → 500 Internal Server Error
{
    await HandleUnhandledExceptionAsync(context, ex);
}
```

### Handler 1: DomainException

1. Logs at `Warning` level with `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`
2. Resolves the error code via `IErrorCodeResolverService` (see [ERROR_CODES.md](ERROR_CODES.md))
3. Returns `ApiResponse<object>` with the resolved response code
4. Sets `Retry-After` header if `RateLimitExceededException`

### Handler 2: DbUpdateException (PostgreSQL Constraints)

When `SaveChangesAsync()` hits a database constraint, EF Core throws `DbUpdateException` with a `PostgresException` inner exception. The middleware maps PostgreSQL SQL state codes to domain-level errors:

| SQL State | Constraint Type | Error Code | HTTP Status |
|-----------|----------------|------------|-------------|
| `23505` | Unique violation | `UNIQUE_CONSTRAINT_VIOLATION` (9001) | 409 Conflict |
| `23503` | Foreign key violation | `FOREIGN_KEY_VIOLATION` (9002) | 409 Conflict |
| Other | Unknown DB error | `INTERNAL_ERROR` (9999) | 500 |

The response message includes the constraint name for debugging:

```json
{
  "success": false,
  "errorCode": "UNIQUE_CONSTRAINT_VIOLATION",
  "errorValue": 9001,
  "message": "A record with this value already exists (constraint: uq_organizations_name).",
  "correlationId": "abc123"
}
```

Logged at `Warning` level with `SqlState`, `ConstraintName`, and `TableName`.

If the inner exception is not a `PostgresException`, it falls through to a generic 500 response.

This acts as a safety net — most duplicate checks are done in the service layer before hitting the DB. The constraint mapping catches race conditions where two concurrent requests pass the service-layer check.

### Handler 3: Unhandled Exceptions

1. Logs at `Error` level with full stack trace, `ExceptionType`, and `InnerExceptionType`
2. Returns generic 500 response — never leaks exception details to the client

```json
{
  "success": false,
  "errorCode": "INTERNAL_ERROR",
  "errorValue": 9999,
  "message": "An unexpected error occurred.",
  "correlationId": "abc123",
  "responseCode": "98"
}
```

## Adding a New Error

1. Add error code and value to `{Service}.Domain/Exceptions/ErrorCodes.cs`
2. Create typed exception class in `{Service}.Domain/Exceptions/`
3. Throw it from the service layer
4. Register the error code in UtilityService's error code registry (optional — enables dynamic resolution)

The middleware, ApiResponse envelope, and error code resolution handle everything else automatically.

## Related Docs

- [ERROR_CODES.md](ERROR_CODES.md) — Error code registry, per-service ranges, resolution tiers
- [API_RESPONSES.md](API_RESPONSES.md) — ApiResponse envelope, response code mapping
- [VALIDATION.md](VALIDATION.md) — FluentValidation and 400 responses
