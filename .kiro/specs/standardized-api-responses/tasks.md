# Implementation Plan: Standardized API Responses

## Overview

Introduce `ToActionResult()` and `ToBadRequest()` extension methods on `ApiResponse<T>` across all 5 backend services, migrate all 46 controllers to use them, remove the private `Wrap()` methods, and validate with existing + new tests. The rollout is service-by-service: create the extension, migrate controllers, remove Wrap, run existing tests, then add new property-based and unit tests.

## Tasks

- [x] 1. SecurityService — Extension and Controller Migration
  - [x] 1.1 Create `ApiResponseExtensions.cs` in `SecurityService.Api/Extensions/`
    - Implement `ToActionResult<T>`, `ToBadRequest`, and `DetermineStatusCodeFromErrorCode` as defined in the design
    - Use `SecurityService.Application.DTOs` namespace for `ApiResponse<T>`
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 1.2 Migrate `AuthController` to use `ToActionResult()`
    - Replace inline `ApiResponse<T>.Ok(...); response.CorrelationId = ...; return Ok(apiResponse);` pattern with `.ToActionResult(HttpContext)`
    - Applies to Login, Logout, Refresh, RequestOtp, VerifyOtp, GenerateCredentials actions
    - Add `using SecurityService.Api.Extensions;`
    - _Requirements: 8.1, 8.3, 5.2_

  - [x] 1.3 Migrate `PasswordController`, `SessionController`, and `ServiceTokenController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` calls with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods from each controller
    - Add `using SecurityService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4, 5.2_

  - [x]* 1.4 Write property-based tests for SecurityService `ApiResponseExtensions`
    - Create `SecurityService.Tests/Property/ApiResponseExtensionsPropertyTests.cs`
    - **Property 1: Response body preserves all ApiResponse properties**
    - **Validates: Requirements 1.3, 9.2**
    - **Property 2: ErrorCode-to-HTTP-status mapping is correct**
    - **Validates: Requirements 2.1–2.11, 10.2, 10.3**
    - **Property 3: Success responses use custom or default status code**
    - **Validates: Requirements 1.1, 1.2, 4.1, 4.2**
    - **Property 4: Custom status code is ignored for error responses**
    - **Validates: Requirements 4.3**
    - **Property 5: CorrelationId injection from HttpContext**
    - **Validates: Requirements 5.2, 5.3, 6.3**
    - **Property 6: ToBadRequest produces correct structure**
    - **Validates: Requirements 6.1, 6.2**

  - [x]* 1.5 Write unit tests for SecurityService `ApiResponseExtensions`
    - Create `SecurityService.Tests/Unit/Extensions/ApiResponseExtensionsTests.cs`
    - Test null response → 500 with INTERNAL_ERROR, ErrorValue 9999
    - Test each exact-match ErrorCode (INVALID_CREDENTIALS → 401, ACCOUNT_LOCKED → 423, etc.)
    - Test ToBadRequest with null HttpContext → CorrelationId is null
    - _Requirements: 3.1, 3.2, 2.1–2.11, 6.1, 6.2, 6.3_

- [x] 2. Checkpoint — SecurityService
  - Ensure all SecurityService.Tests pass, ask the user if questions arise.

- [x] 3. ProfileService — Extension and Controller Migration
  - [x] 3.1 Create `ApiResponseExtensions.cs` in `ProfileService.Api/Extensions/`
    - Same implementation as SecurityService but using `ProfileService.Application.DTOs` namespace
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 3.2 Migrate `OrganizationController` and `DepartmentController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods
    - Add `using ProfileService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4_

  - [x] 3.3 Migrate `TeamMemberController`, `InviteController`, `DeviceController`, `NotificationSettingController`, `PreferenceController`, `RoleController`, and `PlatformAdminController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods from each controller
    - Add `using ProfileService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

  - [x]* 3.4 Write property-based tests for ProfileService `ApiResponseExtensions`
    - Create `ProfileService.Tests/Property/ApiResponseExtensionsPropertyTests.cs`
    - **Property 1: Response body preserves all ApiResponse properties**
    - **Validates: Requirements 1.3, 9.2**
    - **Property 2: ErrorCode-to-HTTP-status mapping is correct**
    - **Validates: Requirements 2.1–2.11, 10.2, 10.3**
    - **Property 3: Success responses use custom or default status code**
    - **Validates: Requirements 1.1, 1.2, 4.1, 4.2**
    - **Property 4: Custom status code is ignored for error responses**
    - **Validates: Requirements 4.3**
    - **Property 5: CorrelationId injection from HttpContext**
    - **Validates: Requirements 5.2, 5.3, 6.3**
    - **Property 6: ToBadRequest produces correct structure**
    - **Validates: Requirements 6.1, 6.2**

  - [x]* 3.5 Write unit tests for ProfileService `ApiResponseExtensions`
    - Create `ProfileService.Tests/Unit/Extensions/ApiResponseExtensionsTests.cs`
    - Test null response → 500, each exact-match ErrorCode, ToBadRequest with null HttpContext
    - _Requirements: 3.1, 3.2, 2.1–2.11, 6.1, 6.2, 6.3_

- [x] 4. Checkpoint — ProfileService
  - Ensure all ProfileService.Tests pass, ask the user if questions arise.

- [x] 5. WorkService — Extension and Controller Migration
  - [x] 5.1 Create `ApiResponseExtensions.cs` in `WorkService.Api/Extensions/`
    - Same implementation as SecurityService but using `WorkService.Application.DTOs` namespace
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 5.2 Migrate `StoryController` and `TaskController` to use `ToActionResult()`
    - Replace `Wrap()` / `Wrap<T>()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` and `Wrap<T>()` methods
    - Add `using WorkService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4_

  - [x] 5.3 Migrate `SprintController`, `ProjectController`, `BoardController`, `CommentController`, `LabelController`, and `SearchController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods from each controller
    - Add `using WorkService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

  - [x] 5.4 Migrate `AnalyticsController`, `ReportController`, `TimeEntryController`, `CostRateController`, `TimePolicyController`, `WorkflowController`, `RiskRegisterController`, and `SavedFilterController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods from each controller
    - Add `using WorkService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

  - [x]* 5.5 Write property-based tests for WorkService `ApiResponseExtensions`
    - Create `WorkService.Tests/Property/ApiResponseExtensionsPropertyTests.cs`
    - **Property 1: Response body preserves all ApiResponse properties**
    - **Validates: Requirements 1.3, 9.2**
    - **Property 2: ErrorCode-to-HTTP-status mapping is correct**
    - **Validates: Requirements 2.1–2.11, 10.2, 10.3**
    - **Property 3: Success responses use custom or default status code**
    - **Validates: Requirements 1.1, 1.2, 4.1, 4.2**
    - **Property 4: Custom status code is ignored for error responses**
    - **Validates: Requirements 4.3**
    - **Property 5: CorrelationId injection from HttpContext**
    - **Validates: Requirements 5.2, 5.3, 6.3**
    - **Property 6: ToBadRequest produces correct structure**
    - **Validates: Requirements 6.1, 6.2**

  - [x]* 5.6 Write unit tests for WorkService `ApiResponseExtensions`
    - Create `WorkService.Tests/Unit/Extensions/ApiResponseExtensionsTests.cs`
    - Test null response → 500, each exact-match ErrorCode, ToBadRequest with null HttpContext
    - _Requirements: 3.1, 3.2, 2.1–2.11, 6.1, 6.2, 6.3_

- [x] 6. Checkpoint — WorkService
  - Ensure all WorkService.Tests pass, ask the user if questions arise.

- [x] 7. BillingService — Extension and Controller Migration
  - [x] 7.1 Create `ApiResponseExtensions.cs` in `BillingService.Api/Extensions/`
    - Same implementation as SecurityService but using `BillingService.Application.DTOs` namespace
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 7.2 Migrate `SubscriptionController`, `PlanController`, and `FeatureGateController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods
    - Add `using BillingService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4_

  - [x] 7.3 Migrate `AdminBillingController`, `AdminPlanController`, `StripeWebhookController`, and `UsageController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods from each controller
    - Add `using BillingService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

  - [x]* 7.4 Write property-based tests for BillingService `ApiResponseExtensions`
    - Create `BillingService.Tests/Property/ApiResponseExtensionsPropertyTests.cs`
    - **Property 1: Response body preserves all ApiResponse properties**
    - **Validates: Requirements 1.3, 9.2**
    - **Property 2: ErrorCode-to-HTTP-status mapping is correct**
    - **Validates: Requirements 2.1–2.11, 10.2, 10.3**
    - **Property 3: Success responses use custom or default status code**
    - **Validates: Requirements 1.1, 1.2, 4.1, 4.2**
    - **Property 4: Custom status code is ignored for error responses**
    - **Validates: Requirements 4.3**
    - **Property 5: CorrelationId injection from HttpContext**
    - **Validates: Requirements 5.2, 5.3, 6.3**
    - **Property 6: ToBadRequest produces correct structure**
    - **Validates: Requirements 6.1, 6.2**

  - [x]* 7.5 Write unit tests for BillingService `ApiResponseExtensions`
    - Create `BillingService.Tests/Unit/Extensions/ApiResponseExtensionsTests.cs`
    - Test null response → 500, each exact-match ErrorCode, ToBadRequest with null HttpContext
    - _Requirements: 3.1, 3.2, 2.1–2.11, 6.1, 6.2, 6.3_

- [x] 8. Checkpoint — BillingService
  - Ensure all BillingService.Tests pass, ask the user if questions arise.

- [x] 9. UtilityService — Extension and Controller Migration
  - [x] 9.1 Create `ApiResponseExtensions.cs` in `UtilityService.Api/Extensions/`
    - Same implementation as SecurityService but using `UtilityService.Application.DTOs` namespace
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 9.2 Migrate `AuditLogController`, `ErrorLogController`, `ErrorCodeController`, `NotificationController`, and `ReferenceDataController` to use `ToActionResult()`
    - Replace `Wrap()` + `Ok()`/`StatusCode()` with `.ToActionResult(HttpContext)` or `.ToActionResult(HttpContext, 201)`
    - Remove private `Wrap()` methods from each controller
    - Add `using UtilityService.Api.Extensions;`
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

  - [x]* 9.3 Write property-based tests for UtilityService `ApiResponseExtensions`
    - Create `UtilityService.Tests/Property/ApiResponseExtensionsPropertyTests.cs`
    - **Property 1: Response body preserves all ApiResponse properties**
    - **Validates: Requirements 1.3, 9.2**
    - **Property 2: ErrorCode-to-HTTP-status mapping is correct**
    - **Validates: Requirements 2.1–2.11, 10.2, 10.3**
    - **Property 3: Success responses use custom or default status code**
    - **Validates: Requirements 1.1, 1.2, 4.1, 4.2**
    - **Property 4: Custom status code is ignored for error responses**
    - **Validates: Requirements 4.3**
    - **Property 5: CorrelationId injection from HttpContext**
    - **Validates: Requirements 5.2, 5.3, 6.3**
    - **Property 6: ToBadRequest produces correct structure**
    - **Validates: Requirements 6.1, 6.2**

  - [x]* 9.4 Write unit tests for UtilityService `ApiResponseExtensions`
    - Create `UtilityService.Tests/Unit/Extensions/ApiResponseExtensionsTests.cs`
    - Test null response → 500, each exact-match ErrorCode, ToBadRequest with null HttpContext
    - _Requirements: 3.1, 3.2, 2.1–2.11, 6.1, 6.2, 6.3_

- [x] 10. Checkpoint — UtilityService
  - Ensure all UtilityService.Tests pass, ask the user if questions arise.

- [x] 11. Final Regression Gate
  - Run all test suites across all 5 services: SecurityService.Tests, ProfileService.Tests, WorkService.Tests, BillingService.Tests, UtilityService.Tests
  - Confirm all existing controller, middleware, service, and property tests pass green
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation per service
- Property tests validate the 6 correctness properties from the design using FsCheck.Xunit
- Unit tests validate specific examples and edge cases
- Phase 1 keeps `ApiResponse<object>` to avoid test churn — existing controller tests should pass without modification
- The `DetermineStatusCodeFromErrorCode` mapping is identical across all 5 services
