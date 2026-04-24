# Implementation Plan: Architecture Hardening

## Overview

Implement 7 cross-cutting architecture improvements across all 5 backend services (SecurityService, ProfileService, BillingService, WorkService, UtilityService). Tasks are grouped by component — each component is implemented across all 5 services together, then tested, before moving to the next. This ensures consistency and catches cross-service issues early.

All services follow identical patterns: `ControllerServiceExtensions.cs` for controller/filter config, `MiddlewarePipelineExtensions.cs` for middleware pipeline, `ErrorCodeResolverService.cs` for error code resolution, and `GlobalExceptionHandlerMiddleware.cs` for exception handling.

## Tasks

- [x] 1. JSON null suppression and NullBodyFilter across all 5 services
  - [x] 1.1 Configure JSON null suppression in ControllerServiceExtensions for all 5 services
    - Add `.AddJsonOptions(json => { json.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; })` to the `AddControllers()` call in each service's `ControllerServiceExtensions.AddApiControllers()`
    - Files: `SecurityService.Api/Extensions/ControllerServiceExtensions.cs`, `ProfileService.Api/Extensions/ControllerServiceExtensions.cs`, `BillingService.Api/Extensions/ControllerServiceExtensions.cs`, `WorkService.Api/Extensions/ControllerServiceExtensions.cs`, `UtilityService.Api/Extensions/ControllerServiceExtensions.cs`
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.2 Write property test for null field suppression (Property 1)
    - **Property 1: Null field suppression in serialized JSON**
    - Generate `ApiResponse<object>` instances with arbitrary combinations of null and non-null fields using FsCheck; serialize with the configured `JsonSerializerOptions`; assert every null field is absent and every non-null field is present in the JSON output
    - Test file: `SecurityService.Tests/Property/JsonNullSuppressionPropertyTests.cs`
    - **Validates: Requirements 1.2, 1.3**

  - [x] 1.3 Create NullBodyFilter in all 5 services
    - Create `Api/Filters/NullBodyFilter.cs` in each service inheriting `ActionFilterAttribute`, overriding `OnActionExecuting` to check for null `[FromBody]` parameters and return 422 `ApiResponse` with `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, `responseCode: "99"`
    - Register as a global filter in each service's `ControllerServiceExtensions.AddApiControllers()` via `options.Filters.Add<NullBodyFilter>()`
    - Files: `{Service}.Api/Filters/NullBodyFilter.cs` (5 new files), `{Service}.Api/Extensions/ControllerServiceExtensions.cs` (5 modified files)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 1.4 Write property test for NullBodyFilter (Property 2)
    - **Property 2: NullBodyFilter null-body gating**
    - Generate mock `ActionExecutingContext` with null and non-null `[FromBody]` parameters using FsCheck; assert `context.Result` is set to 422 `ApiResponse` if and only if the body parameter is null
    - Test file: `SecurityService.Tests/Property/NullBodyFilterPropertyTests.cs`
    - **Validates: Requirements 2.2, 2.3**

  - [x] 1.5 Write unit tests for NullBodyFilter edge cases
    - Test action with no `[FromBody]` parameters (pass-through)
    - Test action with multiple body parameters (first null caught)
    - Test `CorrelationId` included in response from `HttpContext.Items`
    - Test file: `SecurityService.Tests/Unit/Filters/NullBodyFilterTests.cs`
    - _Requirements: 2.2, 2.3_

- [x] 2. InvalidModelStateResponseFactory across all 5 services
  - [x] 2.1 Configure InvalidModelStateResponseFactory and flip SuppressModelStateInvalidFilter in all 5 services
    - Remove `SuppressModelStateInvalidFilter = true` block from each service's `Program.cs`
    - Add `services.Configure<ApiBehaviorOptions>()` in each service's `ControllerServiceExtensions` with `SuppressModelStateInvalidFilter = false` and the `InvalidModelStateResponseFactory` delegate that returns 422 with `responseCode: "96"`, field errors in `data` array, and `correlationId`
    - Files: `{Service}.Api/Program.cs` (5 modified), `{Service}.Api/Extensions/ControllerServiceExtensions.cs` (5 modified)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 2.2 Write property test for InvalidModelStateResponseFactory (Property 3)
    - **Property 3: InvalidModelStateResponseFactory structured output**
    - Generate random `ModelStateDictionary` with varying fields and errors using FsCheck; invoke the factory delegate; assert 422 `ObjectResult` with `responseCode: "96"`, `errorCode: "VALIDATION_ERROR"`, `errorValue: 1000`, correct `data` array with one `{ field, message }` per error, and `correlationId` from `HttpContext.Items`
    - Test file: `SecurityService.Tests/Property/InvalidModelStateResponseFactoryPropertyTests.cs`
    - **Validates: Requirements 3.2, 3.3, 3.4**

  - [x] 2.3 Write unit tests for InvalidModelStateResponseFactory edge cases
    - Test empty `ModelState` does not trigger the factory
    - Test single field with multiple errors produces multiple entries in `data`
    - Test file: `SecurityService.Tests/Unit/Filters/InvalidModelStateResponseFactoryTests.cs`
    - _Requirements: 3.2, 3.3_

- [x] 3. Checkpoint — Serialization and validation
  - Ensure all tests pass, ask the user if questions arise.
  - Verify that existing FluentValidation tests still pass after the `SuppressModelStateInvalidFilter` change
  - Verify JSON null suppression does not break existing test assertions that check for null fields in serialized output

- [x] 4. In-memory cache tier for ErrorCodeResolverService across all 5 services
  - [x] 4.1 Add RefreshCacheAsync and ClearMemoryCache to IErrorCodeResolverService interface in all 5 services
    - Add `Task RefreshCacheAsync(CancellationToken ct = default)` and `void ClearMemoryCache()` to `IErrorCodeResolverService` in each service's Domain layer
    - Files: `{Service}.Domain/Interfaces/Services/ErrorCodeResolver/IErrorCodeResolverService.cs` (5 modified)
    - _Requirements: 4.6_

  - [x] 4.2 Implement in-memory cache tier in ErrorCodeResolverService in all 5 services
    - Add `ConcurrentDictionary<string, (string ResponseCode, string ResponseDescription)>` as Tier 1 in-memory cache
    - Modify `ResolveAsync` to check in-memory first, then Redis, then HTTP, then static fallback
    - On Redis hit: populate in-memory cache before returning
    - On HTTP hit: populate both in-memory and Redis before returning
    - Implement `RefreshCacheAsync` to fetch all error codes from UtilityService and repopulate both caches
    - Implement `ClearMemoryCache` to clear the `ConcurrentDictionary`
    - Files: `{Service}.Infrastructure/Services/ErrorCodeResolver/ErrorCodeResolverService.cs` (5 modified)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 4.3 Write property test for tiered cache resolution with promotion (Property 4)
    - **Property 4: Tiered cache resolution with promotion**
    - Generate random error code strings; configure mock tiers with random hit/miss patterns using FsCheck; assert `ResolveAsync` returns from the highest-priority available tier and promotes to all higher-priority tiers that had a miss
    - Test file: `SecurityService.Tests/Property/ErrorCodeResolverCachePropertyTests.cs`
    - **Validates: Requirements 4.2, 4.3, 4.4**

  - [x] 4.4 Write property test for static fallback resolution (Property 5)
    - **Property 5: Static fallback resolution**
    - Generate random error code strings; configure all mock tiers to fail using FsCheck; assert `ResolveAsync` returns the same result as the static `MapErrorToResponseCode` method
    - Test file: `SecurityService.Tests/Property/ErrorCodeResolverFallbackPropertyTests.cs`
    - **Validates: Requirements 4.5**

- [x] 5. ErrorCodeCacheRefreshService across all 5 services
  - [x] 5.1 Create ErrorCodeCacheRefreshService BackgroundService in all 5 services
    - Create `Infrastructure/Services/BackgroundServices/ErrorCodeCacheRefreshService.cs` in each service as a `BackgroundService` that calls `IErrorCodeResolverService.RefreshCacheAsync` on startup and every 24 hours
    - Wrap refresh in try/catch — log warning on failure, continue loop
    - Register as hosted service in each service's infrastructure DI setup
    - Files: `{Service}.Infrastructure/Services/BackgroundServices/ErrorCodeCacheRefreshService.cs` (5 new files), DI registration files (5 modified)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [x] 5.2 Write unit tests for ErrorCodeCacheRefreshService
    - Test initial refresh on startup calls `RefreshCacheAsync`
    - Test failure during refresh logs warning and does not crash the service
    - Test coexistence with existing `ErrorCodeValidationHostedService`
    - Test file: `SecurityService.Tests/Services/ErrorCodeCacheRefreshServiceTests.cs`
    - _Requirements: 5.2, 5.4, 5.6_

- [x] 6. Checkpoint — Cache layer
  - Ensure all tests pass, ask the user if questions arise.
  - Verify `ErrorCodeResolverService` still resolves codes correctly through all 4 tiers
  - Verify `ErrorCodeCacheRefreshService` starts without conflicting with `ErrorCodeValidationHostedService`

- [x] 7. ErrorResponseLoggingMiddleware across all 5 services
  - [x] 7.1 Create ErrorResponseLoggingMiddleware in all 5 services
    - Create `Api/Middleware/ErrorResponseLoggingMiddleware.cs` in each service
    - After `_next(context)`, check if `StatusCode >= 500` and `ErrorLogged` flag is not set; if so, publish error log to `IOutboxService` with envelope containing `type: "error"`, `serviceName`, `errorCode: "HTTP_{statusCode}"`, `correlationId`, `tenantId`, `severity: "Error"`, and `timestamp`
    - Wrap outbox publish in try/catch — log error on failure, allow response to proceed
    - Set the `ServiceName` constant appropriately per service
    - Files: `{Service}.Api/Middleware/ErrorResponseLoggingMiddleware.cs` (5 new files)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.6_

  - [x] 7.2 Register ErrorResponseLoggingMiddleware in MiddlewarePipelineExtensions for all 5 services
    - Insert `app.UseMiddleware<ErrorResponseLoggingMiddleware>()` between `GlobalExceptionHandlerMiddleware` and `RateLimiterMiddleware` (or `Routing` for UtilityService which has no RateLimiter) in each service's `MiddlewarePipelineExtensions`
    - Update comment numbering to reflect the new pipeline order
    - Files: `{Service}.Api/Extensions/MiddlewarePipelineExtensions.cs` (5 modified)
    - _Requirements: 6.5_

  - [x] 7.3 Write property test for ErrorResponseLoggingMiddleware conditional publish (Property 6)
    - **Property 6: ErrorResponseLoggingMiddleware conditional publish**
    - Generate random HTTP status codes (1xx–5xx) and random `ErrorLogged` flag states using FsCheck; assert publish occurs if and only if status >= 500 AND `ErrorLogged` is not set; verify envelope fields
    - Test file: `SecurityService.Tests/Property/ErrorResponseLoggingMiddlewarePropertyTests.cs`
    - **Validates: Requirements 6.2, 6.3, 6.4**

  - [x] 7.4 Write property test for ErrorResponseLoggingMiddleware outbox failure resilience (Property 7)
    - **Property 7: ErrorResponseLoggingMiddleware outbox failure resilience**
    - Generate random exceptions from `IOutboxService.PublishAsync` mock using FsCheck; assert the middleware catches the exception, logs it, and allows the HTTP response to proceed unmodified
    - Test file: `SecurityService.Tests/Property/ErrorResponseLoggingResiliencePropertyTests.cs`
    - **Validates: Requirements 6.6**

- [x] 8. GlobalExceptionHandlerMiddleware outbox publishing across all 5 services
  - [x] 8.1 Add outbox publishing to GlobalExceptionHandlerMiddleware in all 5 services
    - Resolve `IOutboxService` from `HttpContext.RequestServices` in each handler method
    - In `HandleDomainExceptionAsync`: after writing response, publish error log with `severity: "Warning"`, exception's `errorCode`, `message`, `stackTrace`, `correlationId`, `tenantId`, `timestamp`; skip for `RateLimitExceededException`; set `ErrorLogged = true`
    - In `HandleUnhandledExceptionAsync`: after writing response, publish error log with `severity: "Error"`, `errorCode: "INTERNAL_ERROR"`, message including inner exception detail, `stackTrace`, `correlationId`, `tenantId`, `timestamp`; set `ErrorLogged = true`
    - In `HandleDbUpdateExceptionAsync`: apply same outbox publishing pattern for database errors
    - Add shared `PublishErrorLogAsync` helper method
    - Wrap all outbox publishing in try/catch — log error on failure, still return structured response
    - Files: `{Service}.Api/Middleware/GlobalExceptionHandlerMiddleware.cs` (5 modified)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

  - [x] 8.2 Write property test for GlobalExceptionHandler DomainException outbox publish (Property 8)
    - **Property 8: GlobalExceptionHandler DomainException outbox publish**
    - Generate random `DomainException` instances (varying `errorCode`, `message`, `stackTrace`) excluding `RateLimitExceededException` using FsCheck; assert outbox publish with `severity: "Warning"`, correct envelope fields, and `ErrorLogged = true`
    - Test file: `SecurityService.Tests/Property/GlobalExceptionHandlerDomainPropertyTests.cs`
    - **Validates: Requirements 7.2, 7.5, 7.7**

  - [x] 8.3 Write property test for GlobalExceptionHandler unhandled exception outbox publish (Property 9)
    - **Property 9: GlobalExceptionHandler unhandled exception outbox publish**
    - Generate random exceptions with varying inner exceptions using FsCheck; assert outbox publish with `errorCode: "INTERNAL_ERROR"`, `severity: "Error"`, message including inner exception detail, and `ErrorLogged = true`
    - Test file: `SecurityService.Tests/Property/GlobalExceptionHandlerUnhandledPropertyTests.cs`
    - **Validates: Requirements 7.3, 7.5, 7.7**

  - [x] 8.4 Write property test for GlobalExceptionHandler outbox failure resilience (Property 10)
    - **Property 10: GlobalExceptionHandler outbox failure resilience**
    - Generate random exceptions and configure `IOutboxService.PublishAsync` mock to throw using FsCheck; assert the middleware catches the publish failure, logs it, and still returns the structured `ApiResponse` error response
    - Test file: `SecurityService.Tests/Property/GlobalExceptionHandlerResiliencePropertyTests.cs`
    - **Validates: Requirements 7.6**

  - [x] 8.5 Write unit tests for RateLimitExceededException exclusion and ErrorLogged flag
    - Test that `RateLimitExceededException` does NOT trigger outbox publish
    - Test that `ErrorLogged` flag is set after successful outbox publish
    - Test that `ErrorLogged` flag is NOT set when outbox publish fails
    - Test file: `SecurityService.Tests/Unit/Middleware/GlobalExceptionHandlerOutboxTests.cs`
    - _Requirements: 7.4, 7.5, 7.6_

- [x] 9. Checkpoint — Middleware and outbox integration
  - Ensure all tests pass, ask the user if questions arise.
  - Verify the `ErrorLogged` flag prevents double-logging between `GlobalExceptionHandlerMiddleware` and `ErrorResponseLoggingMiddleware`
  - Verify middleware pipeline order is correct in all 5 services

- [x] 10. Final wiring and existing test fixes
  - [x] 10.1 Fix existing tests broken by JSON null suppression
    - Review and update any existing test assertions that check for null fields in serialized JSON output (e.g., `ApiResponseExtensionsPropertyTests.cs`)
    - Update tests that assert `"data": null` or `"errors": null` in response bodies — these fields will now be absent
    - Files: test files across all 5 services that assert on serialized `ApiResponse` JSON
    - _Requirements: 1.2, 1.3_

  - [x] 10.2 Write integration tests for middleware pipeline order and validation flow
    - Test null body request returns NullBodyFilter 422 response (not FluentValidation error)
    - Test invalid request returns 422 with field errors in `data` array from `InvalidModelStateResponseFactory`
    - Test that controller action is not invoked when `ModelState` is invalid
    - Test file: `SecurityService.Tests/Middleware/ValidationPipelineIntegrationTests.cs`
    - _Requirements: 2.5, 3.5_

- [x] 11. Final checkpoint — Full regression
  - Ensure all tests pass, ask the user if questions arise.
  - Run full test suite across all service test projects to confirm no regressions

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests use FsCheck.Xunit (already in SecurityService.Tests.csproj) with minimum 100 iterations
- All property tests are written against SecurityService as the reference implementation — the same code is replicated across all 5 services
- Checkpoints ensure incremental validation after each logical group
- The `SuppressModelStateInvalidFilter` flip (task 2.1) is the most likely change to break existing tests — checkpoint 3 specifically validates this
- UtilityService has no `RateLimiterMiddleware` — the `ErrorResponseLoggingMiddleware` insertion point differs slightly (between GlobalExceptionHandler and Routing)
