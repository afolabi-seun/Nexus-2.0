# Requirements Document

## Introduction

This specification covers 7 cross-cutting architecture improvements applied uniformly across all 5 backend services (SecurityService, ProfileService, BillingService, WorkService, UtilityService) to align Nexus 2.0 with the WEP reference architecture documented in `/docs/`. The improvements span JSON serialization, request validation, error code resolution caching, background cache refresh, error response logging middleware, and error outbox publishing from the global exception handler.

## Glossary

- **Service**: One of the 5 backend services — SecurityService, ProfileService, BillingService, WorkService, UtilityService
- **Program_cs**: The `Program.cs` entry point file in each Service's Api project that configures the DI container and middleware pipeline
- **JSON_Serializer**: The `System.Text.Json` serializer configured in each Service's `Program.cs` via `JsonSerializerOptions`
- **ApiResponse**: The standard `ApiResponse<T>` envelope returned by every API endpoint, containing fields such as `responseCode`, `success`, `data`, `errorCode`, `errorValue`, `errors`, `message`, and `correlationId`
- **NullBodyFilter**: A global `ActionFilterAttribute` that intercepts requests with null `[FromBody]` parameters before they reach the controller action
- **InvalidModelStateResponseFactory**: A delegate on `ApiBehaviorOptions` that formats ASP.NET model state errors into the `ApiResponse` envelope
- **ErrorCodeResolverService**: The `IErrorCodeResolverService` implementation that resolves error code strings into `(ResponseCode, ResponseDescription)` tuples via a multi-tier cache
- **In_Memory_Cache**: A `ConcurrentDictionary<string, (string ResponseCode, string ResponseDescription)>` used as Tier 1 in the error code resolution pipeline
- **Redis_Cache**: The Redis hash at key `wep:error_codes_registry` used as Tier 2 in the error code resolution pipeline
- **ErrorCodeCacheRefreshService**: A `BackgroundService` that periodically refreshes the In_Memory_Cache and Redis_Cache by fetching all error codes from UtilityService
- **ErrorResponseLoggingMiddleware**: Middleware that detects 5xx responses produced by `ServiceResult` (not exceptions) and publishes error logs to UtilityService via the Redis outbox
- **GlobalExceptionHandlerMiddleware**: Middleware that catches `DomainException` and unhandled exceptions, formats structured `ApiResponse` error responses, and publishes error logs
- **IOutboxService**: The interface for publishing messages to the Redis outbox queue, already registered in all 5 services
- **ErrorLogged_Flag**: The `HttpContext.Items["ErrorLogged"]` boolean flag used to prevent double-logging between GlobalExceptionHandlerMiddleware and ErrorResponseLoggingMiddleware
- **MiddlewarePipelineExtensions**: The `UseXxxPipeline()` extension method in each Service that registers middleware in the correct order
- **DomainException**: The base exception class for expected business rule violations, carrying `ErrorCode`, `ErrorValue`, and `StatusCode`
- **RateLimitExceededException**: A `DomainException` subclass for rate limit violations; error logs are skipped for this exception type to avoid flooding
- **ControllerServiceExtensions**: The extension class in each Service that configures `AddControllers()` options including filters and `ApiBehaviorOptions`
- **FluentValidation**: The validation library already registered with auto-validation in all 5 services
- **ServiceResult**: The `ServiceResult<T>` pattern used by service methods to return business logic outcomes without throwing exceptions

## Requirements

### Requirement 1: Suppress Null Fields in JSON Responses

**User Story:** As an API consumer, I want null fields omitted from JSON responses, so that response payloads are smaller and cleaner.

#### Acceptance Criteria

1. THE JSON_Serializer SHALL be configured with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` in all 5 services' Program_cs.
2. WHEN the JSON_Serializer serializes an ApiResponse where `errorCode`, `errorValue`, `errors`, or `data` fields are null, THE JSON_Serializer SHALL omit those null fields from the serialized output.
3. WHEN the JSON_Serializer serializes an ApiResponse where all fields are non-null, THE JSON_Serializer SHALL include all fields in the serialized output.

### Requirement 2: Null Body Filter

**User Story:** As a developer, I want requests with missing or null `[FromBody]` parameters rejected at the filter level with a structured 422 response, so that null bodies never reach controller actions and cause 500 errors.

#### Acceptance Criteria

1. THE NullBodyFilter SHALL be implemented as a global `ActionFilterAttribute` at `Api/Filters/NullBodyFilter.cs` in each Service.
2. WHEN a request arrives with a null `[FromBody]` parameter, THE NullBodyFilter SHALL return HTTP 422 with an ApiResponse containing `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, `responseCode: "99"`, `responseDescription: "Validation failed"`, and `message: "Request body is required."`.
3. WHEN a request arrives with a non-null `[FromBody]` parameter, THE NullBodyFilter SHALL allow the request to proceed to the next filter or controller action.
4. THE NullBodyFilter SHALL be registered as a global filter in ControllerServiceExtensions in all 5 services.
5. THE NullBodyFilter SHALL execute before FluentValidation auto-validation in the filter pipeline.

### Requirement 3: Invalid Model State Response Factory

**User Story:** As an API consumer, I want model binding and validation errors returned in the standard `ApiResponse` envelope with field-level error details, so that validation error responses are consistent across all endpoints.

#### Acceptance Criteria

1. THE ControllerServiceExtensions SHALL set `SuppressModelStateInvalidFilter = false` in `ApiBehaviorOptions` in all 5 services.
2. THE ControllerServiceExtensions SHALL configure an `InvalidModelStateResponseFactory` that returns HTTP 422 with an ApiResponse containing `responseCode: "96"`, `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, `responseDescription: "Validation error"`, and `message: "Validation error"`.
3. WHEN FluentValidation or ASP.NET model binding adds errors to `ModelState`, THE InvalidModelStateResponseFactory SHALL extract field-level errors as an array of `{ field, message }` objects and place them in the ApiResponse `data` field.
4. THE InvalidModelStateResponseFactory SHALL include the `correlationId` from `HttpContext.Items["CorrelationId"]` in the ApiResponse.
5. WHEN the InvalidModelStateResponseFactory produces a response, THE controller action SHALL NOT be invoked.

### Requirement 4: In-Memory Cache Tier for Error Code Resolution

**User Story:** As a platform operator, I want error code resolution to check an in-memory cache before Redis or HTTP, so that resolution latency is near-zero for previously resolved codes.

#### Acceptance Criteria

1. THE ErrorCodeResolverService SHALL maintain a `ConcurrentDictionary<string, (string ResponseCode, string ResponseDescription)>` as Tier 1 (In_Memory_Cache) in all 5 services.
2. WHEN `ResolveAsync` is called, THE ErrorCodeResolverService SHALL check the In_Memory_Cache first; if a hit is found, THE ErrorCodeResolverService SHALL return the cached value without querying Redis_Cache or UtilityService.
3. WHEN a cache miss occurs in the In_Memory_Cache and a hit is found in Redis_Cache, THE ErrorCodeResolverService SHALL populate the In_Memory_Cache with the resolved value before returning.
4. WHEN a cache miss occurs in both In_Memory_Cache and Redis_Cache and UtilityService returns a successful response, THE ErrorCodeResolverService SHALL populate both the In_Memory_Cache and Redis_Cache with the resolved value before returning.
5. WHEN all tiers miss (In_Memory_Cache, Redis_Cache, and UtilityService), THE ErrorCodeResolverService SHALL fall back to the static `MapErrorToResponseCode` method and return a default response.
6. THE ErrorCodeResolverService SHALL expose a method to clear and repopulate the In_Memory_Cache for use by the ErrorCodeCacheRefreshService.

### Requirement 5: Background Error Code Cache Refresh Service

**User Story:** As a platform operator, I want the error code cache refreshed periodically in the background, so that newly added or updated error codes propagate to all services without restarts.

#### Acceptance Criteria

1. THE ErrorCodeCacheRefreshService SHALL be implemented as a `BackgroundService` in the Infrastructure layer of all 5 services.
2. WHEN the ErrorCodeCacheRefreshService starts, THE ErrorCodeCacheRefreshService SHALL perform an initial cache refresh by fetching all error codes from UtilityService and populating both the In_Memory_Cache and Redis_Cache.
3. WHILE the ErrorCodeCacheRefreshService is running, THE ErrorCodeCacheRefreshService SHALL repeat the cache refresh every 24 hours.
4. IF the cache refresh fails due to UtilityService being unavailable, THEN THE ErrorCodeCacheRefreshService SHALL log a warning and retry on the next 24-hour cycle without crashing.
5. THE ErrorCodeCacheRefreshService SHALL be registered as a hosted service in the DI container of all 5 services.
6. THE ErrorCodeCacheRefreshService SHALL coexist with the existing ErrorCodeValidationHostedService without conflict.

### Requirement 6: Error Response Logging Middleware

**User Story:** As a platform operator, I want 5xx responses that originate from `ServiceResult` (not exceptions) logged and published to UtilityService, so that non-exception server errors are captured for diagnostics.

#### Acceptance Criteria

1. THE ErrorResponseLoggingMiddleware SHALL be implemented at `Api/Middleware/ErrorResponseLoggingMiddleware.cs` in each Service.
2. WHEN a request completes with an HTTP status code of 500 or above and the ErrorLogged_Flag is not set in `HttpContext.Items`, THE ErrorResponseLoggingMiddleware SHALL publish an error log to UtilityService via IOutboxService with the envelope containing `type: "error"`, `serviceName`, `errorCode: "HTTP_{statusCode}"`, `message: "{method} {path} returned {statusCode}"`, `correlationId`, and `severity: "Error"`.
3. WHEN a request completes with an HTTP status code of 500 or above and the ErrorLogged_Flag is already set, THE ErrorResponseLoggingMiddleware SHALL skip publishing to avoid double-logging.
4. WHEN a request completes with an HTTP status code below 500, THE ErrorResponseLoggingMiddleware SHALL take no action.
5. THE ErrorResponseLoggingMiddleware SHALL be registered in MiddlewarePipelineExtensions between GlobalExceptionHandlerMiddleware (position #2) and the RateLimiter middleware (position #4) in all 5 services.
6. IF the IOutboxService publish fails (e.g., Redis is down), THEN THE ErrorResponseLoggingMiddleware SHALL log the failure locally and allow the response to proceed without modification.

### Requirement 7: Error Outbox Publishing from GlobalExceptionHandlerMiddleware

**User Story:** As a platform operator, I want `DomainException` and unhandled exception details published to UtilityService via the Redis outbox from the global exception handler, so that all exception-based errors are centrally logged for diagnostics.

#### Acceptance Criteria

1. THE GlobalExceptionHandlerMiddleware SHALL accept `IOutboxService` as a constructor dependency or resolve it from `HttpContext.RequestServices` in all 5 services.
2. WHEN a `DomainException` is caught (excluding RateLimitExceededException), THE GlobalExceptionHandlerMiddleware SHALL publish an error log to the Redis outbox with envelope containing `type: "error"`, `serviceName`, `errorCode` from the exception, `message` from the exception, `correlationId`, `severity: "Warning"`, and `stackTrace`.
3. WHEN an unhandled exception is caught, THE GlobalExceptionHandlerMiddleware SHALL publish an error log to the Redis outbox with envelope containing `type: "error"`, `serviceName`, `errorCode: "INTERNAL_ERROR"`, `message` including inner exception detail, `correlationId`, `severity: "Error"`, and `stackTrace`.
4. WHEN a `RateLimitExceededException` is caught, THE GlobalExceptionHandlerMiddleware SHALL skip outbox publishing to prevent log flooding.
5. AFTER publishing an error log to the outbox, THE GlobalExceptionHandlerMiddleware SHALL set `HttpContext.Items["ErrorLogged"] = true` to prevent double-logging by ErrorResponseLoggingMiddleware.
6. IF the outbox publish fails, THEN THE GlobalExceptionHandlerMiddleware SHALL log the failure locally and continue returning the structured error response to the client without modification.
7. THE outbox envelope SHALL include a `tenantId` field sourced from `HttpContext.Items["TenantId"]` and a `timestamp` field with the current UTC time.
