# Requirements Document

## Introduction

This feature introduces a `ToActionResult()` extension method on the existing `ApiResponse<T>` class to centralize and standardize all API response handling across the five Nexus 2.0 backend services (SecurityService, ProfileService, WorkService, BillingService, UtilityService). Currently, every controller manually wraps responses using a private `Wrap()` method, manually sets CorrelationId, and manually selects HTTP status codes (`Ok()`, `StatusCode(201, ...)`). This feature replaces that scattered pattern with a single `response.ToActionResult()` call that handles success, null responses, business errors, empty results, validation failures, and HTTP status code mapping — all derived from the existing `ApiResponse<T>` structure and error code system.

## Glossary

- **ApiResponse**: The existing generic `ApiResponse<T>` class present in each service's `Application/DTOs/ApiResponse.cs`, containing ResponseCode, ResponseDescription, Success, Data, ErrorCode, ErrorValue, Message, CorrelationId, and Errors properties.
- **ToActionResult_Extension**: A static extension method on `ApiResponse<T>` that converts the response into an appropriate `IActionResult` with the correct HTTP status code, eliminating manual status code selection in controllers.
- **ToBadRequest_Extension**: A static extension method that creates a standardized 400 Bad Request `IActionResult` from a message string, for simple validation rejection scenarios.
- **ErrorDetail**: The existing class (Field, Message) used in `ApiResponse<T>.Errors` for validation error details.
- **DomainException**: The existing base exception class with ErrorValue, ErrorCode, StatusCode, and CorrelationId, caught by GlobalExceptionHandlerMiddleware.
- **GlobalExceptionHandlerMiddleware**: The existing middleware in each service that catches DomainException and unhandled exceptions, producing `ApiResponse<object>` error responses.
- **Wrap_Method**: The private method currently present in each controller that creates a success `ApiResponse<object>`, sets CorrelationId from HttpContext, and returns it. This is the pattern being replaced.
- **ErrorValue**: The integer error code in `ApiResponse<T>` and `ErrorCodes.cs` that identifies specific error conditions using service-scoped ranges (Security=2000s, Profile=3000s, Work=4000s, Billing=5000s, Utility=6000s, Shared=1000, Internal=9999).
- **ResponseCode**: The string code in `ApiResponse<T>` (e.g., "00" for success, "96" for validation, "98" for internal error) mapped from ErrorCode via `MapErrorToResponseCode`.
- **CorrelationId**: A request-scoped identifier set by CorrelationIdMiddleware and stored in `HttpContext.Items["CorrelationId"]`, used for distributed tracing.
- **Controller**: Any ASP.NET Core controller class inheriting from `ControllerBase` across the five backend services.

## Requirements

### Requirement 1: ToActionResult Extension Method for Success Responses

**User Story:** As a backend developer, I want a single `ToActionResult()` call on `ApiResponse<T>` that returns the correct HTTP 200 OK response for successful results, so that controllers no longer need to manually call `Ok(Wrap(result))`.

#### Acceptance Criteria

1. WHEN `ApiResponse<T>.Success` is true and `ApiResponse<T>.Data` is not null, THE ToActionResult_Extension SHALL return an `OkObjectResult` (HTTP 200) containing the ApiResponse.
2. WHEN `ApiResponse<T>.Success` is true and `ApiResponse<T>.Data` is null, THE ToActionResult_Extension SHALL return an `OkObjectResult` (HTTP 200) containing the ApiResponse with null Data.
3. THE ToActionResult_Extension SHALL preserve all existing properties of the ApiResponse (ResponseCode, ResponseDescription, Success, Data, ErrorCode, ErrorValue, Message, CorrelationId, Errors) in the returned response body.

### Requirement 2: ToActionResult Extension Method for Error Responses

**User Story:** As a backend developer, I want `ToActionResult()` to automatically map business errors in `ApiResponse<T>` to the correct HTTP status codes, so that controllers no longer need to manually select status codes for error scenarios.

#### Acceptance Criteria

1. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` contains "NOT_FOUND", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 404 status code.
2. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "VALIDATION_ERROR", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 400 status code.
3. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` contains "ALREADY_EXISTS" or "DUPLICATE" or "CONFLICT", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 409 status code.
4. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "INSUFFICIENT_PERMISSIONS" or "DEPARTMENT_ACCESS_DENIED" or "ORGANIZATION_MISMATCH", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 403 status code.
5. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "INVALID_CREDENTIALS" or "TOKEN_REVOKED" or "REFRESH_TOKEN_REUSE" or "SESSION_EXPIRED", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 401 status code.
6. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "ACCOUNT_LOCKED", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 423 status code.
7. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "RATE_LIMIT_EXCEEDED", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 429 status code.
8. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "INTERNAL_ERROR", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 500 status code.
9. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` is "SERVICE_UNAVAILABLE", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 503 status code.
10. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` contains "PAYMENT_PROVIDER_ERROR", THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 502 status code.
11. WHEN `ApiResponse<T>.Success` is false and `ApiResponse<T>.ErrorCode` does not match any defined mapping, THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 400 status code as the default error status.

### Requirement 3: ToActionResult Extension Method for Null Response Handling

**User Story:** As a backend developer, I want `ToActionResult()` to handle null `ApiResponse<T>` inputs gracefully, so that unexpected null returns from service methods produce a consistent 500 error instead of a NullReferenceException.

#### Acceptance Criteria

1. WHEN the `ApiResponse<T>` instance is null, THE ToActionResult_Extension SHALL return an `ObjectResult` with HTTP 500 status code containing an ApiResponse with ErrorCode "INTERNAL_ERROR", ErrorValue 9999, and Message "An unexpected null response was received."
2. WHEN the `ApiResponse<T>` instance is null, THE ToActionResult_Extension SHALL set Success to false and ResponseCode to "98" in the returned ApiResponse.

### Requirement 4: ToActionResult with Custom HTTP Status Code Override

**User Story:** As a backend developer, I want to optionally specify a custom HTTP status code (e.g., 201 for resource creation) when calling `ToActionResult()`, so that I can handle cases like POST endpoints that return 201 Created without losing the centralized response logic.

#### Acceptance Criteria

1. WHEN a custom status code parameter is provided and `ApiResponse<T>.Success` is true, THE ToActionResult_Extension SHALL return an `ObjectResult` with the specified HTTP status code instead of the default 200.
2. WHEN a custom status code parameter is not provided and `ApiResponse<T>.Success` is true, THE ToActionResult_Extension SHALL return an `OkObjectResult` (HTTP 200).
3. WHEN a custom status code parameter is provided and `ApiResponse<T>.Success` is false, THE ToActionResult_Extension SHALL ignore the custom status code and use the error-code-derived HTTP status code.

### Requirement 5: Automatic CorrelationId Injection

**User Story:** As a backend developer, I want `ToActionResult()` to automatically set the CorrelationId from the current HttpContext, so that controllers no longer need to manually assign `response.CorrelationId = HttpContext.Items["CorrelationId"]`.

#### Acceptance Criteria

1. THE ToActionResult_Extension SHALL accept an `HttpContext` parameter or use `IHttpContextAccessor` to access the current request context.
2. WHEN `HttpContext.Items["CorrelationId"]` contains a value, THE ToActionResult_Extension SHALL set `ApiResponse<T>.CorrelationId` to that value before returning the result.
3. WHEN `HttpContext.Items["CorrelationId"]` is null or absent, THE ToActionResult_Extension SHALL leave `ApiResponse<T>.CorrelationId` unchanged.

### Requirement 6: ToBadRequest Extension Method

**User Story:** As a backend developer, I want a `ToBadRequest()` extension method that creates a standardized 400 Bad Request response from a simple message string, so that controllers can reject invalid requests without constructing a full `ApiResponse<T>` manually.

#### Acceptance Criteria

1. THE ToBadRequest_Extension SHALL accept a message string parameter and return an `ObjectResult` with HTTP 400 status code.
2. THE ToBadRequest_Extension SHALL return an `ApiResponse<object>` with Success set to false, ResponseCode set to "96", ErrorCode set to "VALIDATION_ERROR", ErrorValue set to 1000, and Message set to the provided message string.
3. WHEN an `HttpContext` is available, THE ToBadRequest_Extension SHALL set the CorrelationId from `HttpContext.Items["CorrelationId"]`.

### Requirement 7: Extension Method Placement per Service

**User Story:** As a backend developer, I want the `ToActionResult()` and `ToBadRequest()` extension methods to be available in each service's API layer, so that all controllers in all five services can use the standardized response handling.

#### Acceptance Criteria

1. THE ToActionResult_Extension SHALL be implemented as a static class in each service's `Api/Extensions` directory (SecurityService.Api, ProfileService.Api, WorkService.Api, BillingService.Api, UtilityService.Api).
2. THE ToActionResult_Extension SHALL operate on the existing `ApiResponse<T>` class specific to each service's namespace without requiring changes to the `ApiResponse<T>` class itself.
3. THE ToActionResult_Extension SHALL use the same error-code-to-HTTP-status mapping logic across all five services.

### Requirement 8: Controller Migration to ToActionResult

**User Story:** As a backend developer, I want all existing controllers across all five services to be refactored to use `ToActionResult()` instead of the manual `Wrap()` + `Ok()`/`StatusCode()` pattern, so that the codebase has a single consistent response handling approach.

#### Acceptance Criteria

1. WHEN a controller action currently calls `Ok(Wrap(result))` or `Ok(Wrap(result, message))`, THE Controller SHALL be refactored to call `ApiResponse<T>.Ok(result, message).ToActionResult(HttpContext)`.
2. WHEN a controller action currently calls `StatusCode(201, Wrap(result, message))`, THE Controller SHALL be refactored to call `ApiResponse<T>.Ok(result, message).ToActionResult(HttpContext, 201)`.
3. WHEN a controller action currently manually constructs an `ApiResponse<T>` and sets CorrelationId before returning, THE Controller SHALL be refactored to use `ToActionResult(HttpContext)` which handles CorrelationId injection automatically.
4. WHEN a controller currently defines a private `Wrap()` method, THE Controller SHALL remove the `Wrap()` method after migration to ToActionResult_Extension.
5. THE Controller migration SHALL cover all controllers across SecurityService (AuthController, PasswordController, SessionController, ServiceTokenController), ProfileService (OrganizationController, DepartmentController, TeamMemberController, InviteController, DeviceController, NotificationSettingController, PreferenceController, RoleController, PlatformAdminController), WorkService (StoryController, TaskController, SprintController, ProjectController, BoardController, CommentController, LabelController, SearchController, AnalyticsController, ReportController, TimeEntryController, CostRateController, TimePolicyController, WorkflowController, RiskRegisterController, SavedFilterController), BillingService (SubscriptionController, PlanController, FeatureGateController, AdminBillingController, AdminPlanController, StripeWebhookController, UsageController), and UtilityService (AuditLogController, ErrorLogController, ErrorCodeController, NotificationController, ReferenceDataController).

### Requirement 9: Compatibility with GlobalExceptionHandlerMiddleware

**User Story:** As a backend developer, I want the `ToActionResult()` extension to coexist with the existing `GlobalExceptionHandlerMiddleware` without conflict, so that DomainExceptions continue to be handled by the middleware while `ToActionResult()` handles service-layer `ApiResponse<T>` results.

#### Acceptance Criteria

1. THE ToActionResult_Extension SHALL handle only `ApiResponse<T>` objects returned from service methods and SHALL NOT intercept or modify DomainException handling.
2. WHILE GlobalExceptionHandlerMiddleware is active, THE ToActionResult_Extension SHALL produce responses with the same `ApiResponse<T>` JSON structure that the middleware produces, ensuring consistent response format for API consumers.
3. THE ToActionResult_Extension SHALL use the same ResponseCode mapping conventions (e.g., "00" for success, "96" for validation, "98" for internal error) that the existing `ApiResponse<T>.MapErrorToResponseCode` method uses.

### Requirement 10: Error Code to HTTP Status Mapping Consistency

**User Story:** As a backend developer, I want the error-code-to-HTTP-status mapping in `ToActionResult()` to be consistent with the HTTP status codes already used by `DomainException.StatusCode` in the middleware, so that the same error produces the same HTTP status regardless of whether it flows through the middleware or through `ToActionResult()`.

#### Acceptance Criteria

1. THE ToActionResult_Extension SHALL map ErrorCode values to HTTP status codes using a centralized `DetermineStatusCodeFromErrorCode` method that can be referenced by both the extension and the middleware.
2. THE ToActionResult_Extension SHALL map ErrorValue ranges to HTTP status codes as follows: 1000 (validation) to 400, 2001-2003 and 2012-2013 and 2024 (auth/token) to 401, 2002 (account locked) to 423, 2010 and 3016 (rate limit) to 429, 2011 and 2020 and 2025 and 3015 and 4030-4032 and 5016 and 6006 (permissions/org mismatch) to 403, any ErrorCode containing "NOT_FOUND" to 404, any ErrorCode containing "ALREADY_EXISTS" or "DUPLICATE" or "CONFLICT" to 409, 5010 (payment provider) to 502, any ErrorCode equal to "SERVICE_UNAVAILABLE" to 503, 9999 (internal) to 500.
3. IF an ErrorCode does not match any defined mapping rule, THEN THE ToActionResult_Extension SHALL default to HTTP 400 status code.
