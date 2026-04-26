# Implementation Plan: ProfileService

## Overview

Incremental implementation of the ProfileService microservice following Clean Architecture (.NET 8) with five projects: Domain, Application, Infrastructure, Api, and Tests. All projects live under `src/backend/ProfileService/` in the Nexus-2.0 monorepo. Tasks build from the innermost layer (Domain) outward, wiring everything together in Program.cs at the end. All code is C# targeting `net8.0`.

## Tasks

- [x] 1. Solution and project scaffolding
  - [x] 1.1 Create monorepo folder structure and .NET 8 projects with project references
    - Create `src/backend/ProfileService/` directory
    - Create `src/backend/ProfileService/ProfileService.Domain` (class library, net8.0, zero project references)
    - Create `src/backend/ProfileService/ProfileService.Application` (class library, net8.0, references Domain)
    - Create `src/backend/ProfileService/ProfileService.Infrastructure` (class library, net8.0, references Domain + Application)
    - Create `src/backend/ProfileService/ProfileService.Api` (web project, net8.0, references Application + Infrastructure)
    - Create `src/backend/ProfileService/ProfileService.Tests` (xUnit test project, net8.0, references Domain + Application + Infrastructure)
    - Add all five projects to `Nexus-2.0.sln`
    - _Requirements: 35.1, 35.2, 35.3, 35.4, 35.5, 35.6_

  - [x] 1.2 Add NuGet package references to each project
    - Domain: no external packages
    - Application: `FluentValidation` only
    - Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Microsoft.Extensions.Http.Polly`, `Polly`, `BCrypt.Net-Next`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Redis`, `DotNetEnv`
    - Api: `FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore`
    - Tests: `xunit`, `xunit.runner.visualstudio`, `FsCheck.Xunit`, `Moq`, `Microsoft.EntityFrameworkCore.InMemory`, `FluentAssertions`
    - _Requirements: 35.2, 35.3, 35.4, 35.5_

- [x] 2. Domain layer — Entities, exceptions, error codes, enums, helpers, interfaces
  - [x] 2.1 Create domain entities
    - Implement `Organization` entity with `OrganizationId`, `OrganizationName`, `StoryIdPrefix`, `Description`, `Website`, `LogoUrl`, `TimeZone`, `DefaultSprintDurationWeeks`, `SettingsJson`, `FlgStatus`, `DateCreated`, `DateUpdated`
    - Implement `Department` entity with `DepartmentId`, `OrganizationId`, `DepartmentName`, `DepartmentCode`, `Description`, `IsDefault`, `PreferencesJson`, `FlgStatus`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `TeamMember` entity with `TeamMemberId`, `OrganizationId`, `PrimaryDepartmentId`, `Email`, `Password`, `FirstName`, `LastName`, `DisplayName`, `AvatarUrl`, `Title`, `ProfessionalId`, `Skills`, `Availability`, `MaxConcurrentTasks`, `IsFirstTimeUser`, `FlgStatus`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `DepartmentMember` entity with `DepartmentMemberId`, `TeamMemberId`, `DepartmentId`, `OrganizationId`, `RoleId`, `DateJoined` — implements `IOrganizationEntity`
    - Implement `Role` entity with `RoleId`, `RoleName`, `Description`, `PermissionLevel`, `IsSystemRole`, `DateCreated`
    - Implement `Invite` entity with `InviteId`, `OrganizationId`, `DepartmentId`, `RoleId`, `InvitedByMemberId`, `FirstName`, `LastName`, `Email`, `Token`, `ExpiryDate`, `FlgStatus`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `Device` entity with `DeviceId`, `OrganizationId`, `TeamMemberId`, `DeviceName`, `DeviceType`, `IpAddress`, `UserAgent`, `IsPrimary`, `FlgStatus`, `DateCreated`, `LastActiveDate` — implements `IOrganizationEntity`
    - Implement `NotificationSetting` entity with `NotificationSettingId`, `NotificationTypeId`, `OrganizationId`, `TeamMemberId`, `IsEmail`, `IsPush`, `IsInApp` — implements `IOrganizationEntity`
    - Implement `NotificationType` entity with `NotificationTypeId`, `TypeName`, `Description`, `DateCreated`
    - Implement `UserPreferences` entity with `UserPreferencesId`, `OrganizationId`, `TeamMemberId`, `Theme`, `Language`, `TimezoneOverride`, `DefaultBoardView`, `DefaultBoardFilters`, `DashboardLayout`, `EmailDigestFrequency`, `KeyboardShortcutsEnabled`, `DateFormat`, `TimeFormat`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `PlatformAdmin` entity with `PlatformAdminId`, `Username`, `PasswordHash`, `Email`, `FirstName`, `LastName`, `IsFirstTimeUser`, `FlgStatus`, `DateCreated`, `DateUpdated`
    - Create `IOrganizationEntity` marker interface in `Common/` with `OrganizationId` property
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5, 24.6, 24.7, 24.8, 24.9, 24.10, 24.11, 16.1, 16.2, 36.2_

  - [x] 2.2 Create `ErrorCodes` static class and `DomainException` base class
    - Implement `ErrorCodes` with all constants (1000, 3001–3027, 9999) and their string/int pairs
    - Implement `DomainException` base class with `ErrorValue`, `ErrorCode`, `StatusCode`, `CorrelationId`
    - _Requirements: 23.1, 23.2, 26.1_

  - [x] 2.3 Create all concrete domain exception classes (3001–3027)
    - `EmailAlreadyRegisteredException` (3001, 409), `InviteExpiredOrInvalidException` (3002, 410), `MaxDevicesReachedException` (3003, 400), `LastOrgAdminCannotDeactivateException` (3004, 400), `OrganizationNameDuplicateException` (3005, 409), `StoryPrefixDuplicateException` (3006, 409), `StoryPrefixImmutableException` (3007, 400), `DepartmentNameDuplicateException` (3008, 409), `DepartmentCodeDuplicateException` (3009, 409), `DefaultDepartmentCannotDeleteException` (3010, 400), `MemberAlreadyInDepartmentException` (3011, 409), `MemberMustHaveDepartmentException` (3012, 400), `InvalidRoleAssignmentException` (3013, 400), `InviteEmailAlreadyMemberException` (3014, 409), `OrganizationMismatchException` (3015, 403), `RateLimitExceededException` (3016, 429 — include `RetryAfterSeconds`), `DepartmentHasActiveMembersException` (3017, 400), `MemberNotInDepartmentException` (3018, 400), `InvalidAvailabilityStatusException` (3019, 400), `StoryPrefixInvalidFormatException` (3020, 400), `NotFoundException` (3021, 404), `ConflictException` (3022, 409), `ServiceUnavailableException` (3023, 503), `DepartmentNotFoundException` (3024, 404), `MemberNotFoundException` (3025, 404), `InvalidPreferenceValueException` (3026, 400), `PreferenceKeyUnknownException` (3027, 400)
    - _Requirements: 23.1_

  - [x] 2.4 Create enums
    - `Availability` (Available, Busy, Away, Offline)
    - `Theme` (Light, Dark, System)
    - `DateFormatType` (ISO, US, EU)
    - `TimeFormatType` (H24, H12)
    - `DigestFrequency` (Realtime, Hourly, Daily, Off)
    - `BoardView` (Kanban, Sprint, Backlog)
    - `StoryPointScale` (Fibonacci, Linear, TShirt)
    - `AutoAssignmentStrategy` (LeastLoaded, RoundRobin)
    - _Requirements: 2.7, 4.5, 14.7, 15.6_

  - [x] 2.5 Create helper constants
    - `RoleNames` with `OrgAdmin`, `DeptLead`, `Member`, `Viewer` and their numeric values (100, 75, 50, 25)
    - `EntityStatuses` with `Active = "A"`, `Suspended = "S"`, `Deactivated = "D"`
    - `InviteStatuses` with `Active = "A"`, `Used = "U"`, `Expired = "E"`
    - `DepartmentTypes` with the 5 default department names and codes
    - `SystemDefaults` with all preference system defaults per Requirement 15.6
    - _Requirements: 7.1, 15.6, 22.1, 34.1_

  - [x] 2.6 Create domain service interfaces
    - `IOrganizationService` (CreateAsync, GetByIdAsync, UpdateAsync, UpdateStatusAsync, UpdateSettingsAsync, ListAllAsync, ProvisionAdminAsync)
    - `IDepartmentService` (CreateAsync, ListAsync, GetByIdAsync, UpdateAsync, UpdateStatusAsync, ListMembersAsync, GetPreferencesAsync, UpdatePreferencesAsync)
    - `ITeamMemberService` (ListAsync, GetByIdAsync, UpdateAsync, UpdateStatusAsync, UpdateAvailabilityAsync, AddToDepartmentAsync, RemoveFromDepartmentAsync, ChangeDepartmentRoleAsync, GetByEmailAsync, UpdatePasswordAsync)
    - `IRoleService` (ListAsync, GetByIdAsync)
    - `IInviteService` (CreateAsync, ListAsync, ValidateTokenAsync, AcceptAsync, CancelAsync)
    - `IDeviceService` (ListAsync, SetPrimaryAsync, RemoveAsync)
    - `INotificationSettingService` (GetSettingsAsync, UpdateSettingAsync, ListTypesAsync)
    - `IPreferenceService` (GetAsync, UpdateAsync)
    - `IPreferenceResolver` (ResolveAsync)
    - `IPlatformAdminService` (GetByUsernameAsync, UpdatePasswordAsync)
    - `IOutboxService` (PublishAsync)
    - `IErrorCodeResolverService` (ResolveAsync)
    - _Requirements: 1, 2, 3, 4, 5, 7, 8, 9, 10, 13, 14, 15, 16, 38, 39_

  - [x] 2.7 Create repository interfaces
    - `IOrganizationRepository` (GetByIdAsync, GetByNameAsync, GetByStoryIdPrefixAsync, AddAsync, UpdateAsync, ListAllAsync)
    - `IDepartmentRepository` (GetByIdAsync, GetByNameAsync, GetByCodeAsync, AddAsync, AddRangeAsync, UpdateAsync, ListByOrganizationAsync, GetActiveMemberCountAsync)
    - `ITeamMemberRepository` (GetByIdAsync, GetByEmailAsync, GetByEmailGlobalAsync, AddAsync, UpdateAsync, ListAsync, CountOrgAdminsAsync, GetNextSequentialNumberAsync)
    - `IDepartmentMemberRepository` (GetAsync, AddAsync, RemoveAsync, UpdateAsync, GetByMemberIdAsync, ListByDepartmentAsync)
    - `IRoleRepository` (GetByIdAsync, GetByNameAsync, ListAsync, AddRangeAsync, ExistsAsync)
    - `IInviteRepository` (GetByIdAsync, GetByTokenAsync, AddAsync, UpdateAsync, ListPendingAsync)
    - `IDeviceRepository` (GetByIdAsync, ListByMemberAsync, CountByMemberAsync, AddAsync, UpdateAsync, RemoveAsync, ClearPrimaryAsync)
    - `INotificationSettingRepository` (GetByMemberAsync, GetAsync, AddAsync, UpdateAsync)
    - `INotificationTypeRepository` (ListAsync, AddRangeAsync, ExistsAsync)
    - `IUserPreferencesRepository` (GetByMemberIdAsync, AddAsync, UpdateAsync)
    - `IPlatformAdminRepository` (GetByIdAsync, GetByUsernameAsync, UpdateAsync)
    - _Requirements: 24.12, 24.13_

- [x] 3. Application layer — DTOs, validators, contracts
  - [x] 3.1 Create `ApiResponse<T>` envelope, `ErrorDetail`, and `PaginatedResponse<T>` classes
    - `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `ErrorValue`, `Message`, `CorrelationId`, `Errors`
    - `ErrorDetail` with `Field`, `Message`
    - `PaginatedResponse<T>` with `Data`, `TotalCount`, `Page`, `PageSize`, `TotalPages`
    - _Requirements: 20.3, 26.1, 33.2_

  - [x] 3.2 Create request DTOs
    - Organizations: `CreateOrganizationRequest`, `UpdateOrganizationRequest`, `OrganizationSettingsRequest`, `StatusChangeRequest`, `ProvisionAdminRequest`
    - Departments: `CreateDepartmentRequest`, `UpdateDepartmentRequest`, `DepartmentPreferencesRequest`
    - TeamMembers: `UpdateTeamMemberRequest`, `AvailabilityRequest`, `AddDepartmentRequest`, `ChangeRoleRequest`
    - Invites: `CreateInviteRequest`, `AcceptInviteRequest`
    - NotificationSettings: `UpdateNotificationSettingRequest`
    - Preferences: `UserPreferencesRequest`
    - _Requirements: 1, 2, 3, 4, 5, 8, 10, 13, 14, 17_

  - [x] 3.3 Create response DTOs
    - Organizations: `OrganizationResponse`, `OrganizationSettingsResponse`
    - Departments: `DepartmentResponse`, `DepartmentPreferencesResponse`
    - TeamMembers: `TeamMemberResponse`, `TeamMemberDetailResponse`, `DepartmentMembershipResponse`, `TeamMemberInternalResponse`
    - Roles: `RoleResponse`
    - Invites: `InviteResponse`, `InviteValidationResponse`
    - Devices: `DeviceResponse`
    - NotificationSettings: `NotificationSettingResponse`, `NotificationTypeResponse`
    - Preferences: `UserPreferencesResponse`, `ResolvedPreferencesResponse`
    - PlatformAdmins: `PlatformAdminInternalResponse`
    - _Requirements: 1.3, 3.5, 4.2, 7.2, 8.5, 9.1, 10.1, 12.1, 14.1, 15.2, 16.3_

  - [x] 3.4 Create inter-service contract DTOs
    - `CredentialGenerateRequest` (MemberId, Email)
    - `ErrorCodeResponse` (ResponseCode, Description)
    - _Requirements: 25.1, 39.2, 44.1_

  - [x] 3.5 Create FluentValidation validators for all request DTOs
    - `CreateOrganizationRequestValidator` (OrganizationName required + max 200, StoryIdPrefix required + regex `^[A-Z0-9]{2,10}$`, TimeZone required, DefaultSprintDurationWeeks 1–4)
    - `UpdateOrganizationRequestValidator` (OrganizationName max 200 when present, DefaultSprintDurationWeeks 1–4 when present)
    - `OrganizationSettingsRequestValidator` (StoryIdPrefix regex when present, DefaultSprintDurationWeeks 1–4 when present, AuditRetentionDays > 0, DefaultWipLimit >= 0, enum validations for StoryPointScale/AutoAssignmentStrategy/DefaultBoardView/DigestFrequency)
    - `ProvisionAdminRequestValidator` (Email required + valid, FirstName/LastName required + max 100)
    - `CreateDepartmentRequestValidator` (DepartmentName required + max 100, DepartmentCode required + max 20 + regex `^[A-Z0-9_]+$`)
    - `UpdateDepartmentRequestValidator` (DepartmentName max 100 when present)
    - `DepartmentPreferencesRequestValidator` (MaxConcurrentTasksDefault > 0 when present)
    - `UpdateTeamMemberRequestValidator` (FirstName/LastName max 100 when present, MaxConcurrentTasks > 0 when present)
    - `AvailabilityRequestValidator` (Availability required + must be Available/Busy/Away/Offline)
    - `AddDepartmentRequestValidator` (DepartmentId required, RoleId required)
    - `ChangeRoleRequestValidator` (RoleId required)
    - `CreateInviteRequestValidator` (Email required + valid, FirstName/LastName required + max 100, DepartmentId/RoleId required)
    - `AcceptInviteRequestValidator` (OtpCode required + 6-digit numeric)
    - `UpdateNotificationSettingRequestValidator` (booleans — no additional validation)
    - `UserPreferencesRequestValidator` (Theme must be Light/Dark/System, Language max 10, DefaultBoardView must be Kanban/Sprint/Backlog, EmailDigestFrequency must be Realtime/Hourly/Daily/Off, DateFormat must be ISO/US/EU, TimeFormat must be H24/H12)
    - `StatusChangeRequestValidator` (Status required + must be A/S/D)
    - _Requirements: 2.2, 2.5, 4.5, 14.5, 14.6, 27.1, 27.2_

  - [x] 3.6 Create `OutboxMessage` class
    - `MessageId`, `MessageType`, `ServiceName`, `OrganizationId`, `UserId`, `Action`, `EntityType`, `EntityId`, `OldValue`, `NewValue`, `IpAddress`, `CorrelationId`, `Timestamp`, `RetryCount`
    - _Requirements: 38.1_

- [x] 4. Checkpoint — Verify Domain and Application layers compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Infrastructure layer — Data access (EF Core + PostgreSQL)
  - [x] 5.1 Create `ProfileDbContext` with entity configurations and global query filters
    - Configure all 11 entities with PKs, indexes, unique constraints, max lengths, JSON column types
    - Apply global query filters by `OrganizationId` on entities implementing `IOrganizationEntity` (Department, TeamMember, DepartmentMember, Invite, Device, NotificationSetting, UserPreferences)
    - No global query filters on Organization, Role, NotificationType, PlatformAdmin
    - Extract `organizationId` from `IHttpContextAccessor` for filter scoping
    - _Requirements: 24.1–24.13, 34.2, 34.3, 36.1, 36.2, 36.3_

  - [x] 5.2 Create `DatabaseMigrationHelper` for auto-migration on startup
    - Apply pending EF Core migrations automatically
    - Use `EnsureCreated()` for InMemory database (test environment)
    - _Requirements: 43.1, 43.2, 43.3_

  - [x] 5.3 Implement all 11 repository classes
    - `OrganizationRepository` — GetByIdAsync, GetByNameAsync, GetByStoryIdPrefixAsync, AddAsync, UpdateAsync, ListAllAsync
    - `DepartmentRepository` — GetByIdAsync, GetByNameAsync, GetByCodeAsync, AddAsync, AddRangeAsync, UpdateAsync, ListByOrganizationAsync, GetActiveMemberCountAsync
    - `TeamMemberRepository` — GetByIdAsync, GetByEmailAsync, GetByEmailGlobalAsync, AddAsync, UpdateAsync, ListAsync (with filters), CountOrgAdminsAsync, GetNextSequentialNumberAsync
    - `DepartmentMemberRepository` — GetAsync, AddAsync, RemoveAsync, UpdateAsync, GetByMemberIdAsync, ListByDepartmentAsync
    - `RoleRepository` — GetByIdAsync, GetByNameAsync, ListAsync, AddRangeAsync, ExistsAsync
    - `InviteRepository` — GetByIdAsync, GetByTokenAsync, AddAsync, UpdateAsync, ListPendingAsync
    - `DeviceRepository` — GetByIdAsync, ListByMemberAsync, CountByMemberAsync, AddAsync, UpdateAsync, RemoveAsync, ClearPrimaryAsync
    - `NotificationSettingRepository` — GetByMemberAsync, GetAsync, AddAsync, UpdateAsync
    - `NotificationTypeRepository` — ListAsync, AddRangeAsync, ExistsAsync
    - `UserPreferencesRepository` — GetByMemberIdAsync, AddAsync, UpdateAsync
    - `PlatformAdminRepository` — GetByIdAsync, GetByUsernameAsync, UpdateAsync
    - _Requirements: 24.12, 24.13, 34.2, 34.4_

  - [x] 5.4 Implement seed data logic
    - `SeedRolesAsync` — seed 4 system roles (OrgAdmin 100, DeptLead 75, Member 50, Viewer 25) with idempotent check
    - `SeedNotificationTypesAsync` — seed 8 notification types with idempotent check
    - `SeedDefaultDepartmentsAsync` — seed 5 default departments per organization (Engineering/ENG, QA/QA, DevOps/DEVOPS, Product/PROD, Design/DESIGN) with `IsDefault=true`
    - _Requirements: 22.1, 22.2, 22.3_

- [x] 6. Infrastructure layer — Configuration
  - [x] 6.1 Create `AppSettings` configuration class
    - `AppSettings.FromEnvironment()` loading from env vars via DotNetEnv
    - All configurable values: DB connection, Redis connection, JWT settings (SecretKey, Issuer, Audience), service URLs (SecurityService, UtilityService), allowed origins, service auth (ServiceId, ServiceName, ServiceSecret), invite settings (ExpiryHours, TokenLength), device settings (MaxDevicesPerUser)
    - `GetRequired()` throws `InvalidOperationException` for missing required vars
    - `GetOptionalInt()` with sensible defaults
    - _Requirements: 29.1, 29.2, 29.3_

  - [x] 6.2 Create `.env.example` with all required environment variables
    - Document all env vars with sensible defaults for local development
    - _Requirements: 29.1_

- [x] 7. Infrastructure layer — Redis services
  - [x] 7.1 Implement `OutboxService` (Redis LPUSH to `outbox:profile`)
    - `PublishAsync` — serialize `OutboxMessage` to JSON, LPUSH to `outbox:profile`
    - Retry up to 3 times with exponential backoff on failure
    - Move to `dlq:profile` after 3 failures
    - _Requirements: 38.1, 38.2, 38.3, 38.4_

  - [x] 7.2 Implement `ErrorCodeResolverService`
    - Check Redis cache at `error_code:{code}` first (24-hour TTL)
    - Call UtilityService on cache miss via `IUtilityServiceClient`
    - Fall back to static `MapErrorToResponseCode` mapping on failure
    - _Requirements: 39.1, 39.2, 39.3, 39.4_

- [x] 8. Infrastructure layer — Service implementations (Organization, Department, TeamMember, Role)
  - [x] 8.1 Implement `OrganizationService`
    - `CreateAsync` — validate name uniqueness, validate StoryIdPrefix uniqueness + format, create organization with `FlgStatus='A'`, seed 5 default departments, publish `OrganizationCreated` audit event, return 201
    - `GetByIdAsync` — return organization details with settings
    - `UpdateAsync` — validate name uniqueness if changed, update fields
    - `UpdateStatusAsync` — enforce `A → S → D` lifecycle transitions
    - `UpdateSettingsAsync` — validate StoryIdPrefix format/uniqueness/immutability, validate DefaultSprintDurationWeeks range, update SettingsJson, invalidate `org_settings:{orgId}` cache
    - `ListAllAsync` — paginated list of all organizations (for PlatformAdmin)
    - `ProvisionAdminAsync` — create TeamMember with OrgAdmin role in Engineering dept, generate professional ID, create DepartmentMember, call SecurityService `POST /api/v1/auth/credentials/generate`, publish `MemberCreated` audit event
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 17.1, 17.2, 17.3_

  - [x] 8.2 Implement `DepartmentService`
    - `CreateAsync` — validate name/code uniqueness within org, create with `IsDefault=false`, invalidate `dept_list:{orgId}` cache
    - `ListAsync` — paginated list cached at `dept_list:{orgId}` with 30-min TTL
    - `GetByIdAsync` — return department details with member count
    - `UpdateAsync` — validate name uniqueness if changed, invalidate cache
    - `UpdateStatusAsync` — check for active members before deactivation, enforce default department protection
    - `ListMembersAsync` — paginated list of department members with roles
    - `GetPreferencesAsync` — return department preferences
    - `UpdatePreferencesAsync` — update PreferencesJson, invalidate `dept_prefs:{deptId}` cache
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 13.1, 13.2, 13.3, 13.4, 13.5_

  - [x] 8.3 Implement `TeamMemberService`
    - `ListAsync` — paginated list filterable by department, role, status, availability
    - `GetByIdAsync` — full profile with department memberships, skills, availability, MaxConcurrentTasks
    - `UpdateAsync` — update profile fields, invalidate `member_profile:{memberId}` cache
    - `UpdateStatusAsync` — enforce last OrgAdmin guard, soft-delete lifecycle
    - `UpdateAvailabilityAsync` — validate availability value (Available/Busy/Away/Offline)
    - `AddToDepartmentAsync` — create DepartmentMember, check duplicate, invalidate caches
    - `RemoveFromDepartmentAsync` — enforce last department guard, remove DepartmentMember, invalidate caches
    - `ChangeDepartmentRoleAsync` — update role in specific department
    - `GetByEmailAsync` — internal endpoint for SecurityService user resolution
    - `UpdatePasswordAsync` — internal endpoint for SecurityService password update
    - Professional ID generation: `NXS-{DeptCode}-{SequentialNumber}` format
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 6.1, 6.2, 6.3, 11.1, 11.2, 11.3, 12.1, 12.2, 12.3, 12.4_

  - [x] 8.4 Implement `RoleService`
    - `ListAsync` — return all 4 system roles
    - `GetByIdAsync` — return role details
    - _Requirements: 7.1, 7.2_

- [x] 9. Infrastructure layer — Service implementations (Invite, Device, Notification, Preference, PlatformAdmin)
  - [x] 9.1 Implement `InviteService`
    - `CreateAsync` — generate cryptographic token (128 chars max), set 48-hour expiry, validate email not already member, scope DeptLead invites to own department, publish email notification to outbox
    - `ListAsync` — paginated pending invites (OrgAdmin sees all, DeptLead sees own department)
    - `ValidateTokenAsync` — return org name, dept name, role if token valid and not expired
    - `AcceptAsync` — validate token + expiry, check email not registered, create TeamMember, generate professional ID, create DepartmentMember, call SecurityService credential generation, update invite FlgStatus to 'U', invalidate caches, publish audit event
    - `CancelAsync` — cancel invite
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

  - [x] 9.2 Implement `DeviceService`
    - `ListAsync` — return all devices for authenticated user
    - `SetPrimaryAsync` — set device as primary, unset previous primary
    - `RemoveAsync` — remove device
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 9.3 Implement `NotificationSettingService`
    - `GetSettingsAsync` — return per-notification-type preferences with IsEmail/IsPush/IsInApp
    - `UpdateSettingAsync` — update preference for specific notification type
    - `ListTypesAsync` — return all 8 notification types
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [x] 9.4 Implement `PreferenceService` and `PreferenceResolver`
    - `PreferenceService.GetAsync` — return user preferences
    - `PreferenceService.UpdateAsync` — update user preferences, invalidate `user_prefs:{userId}` cache
    - `PreferenceResolver.ResolveAsync` — cascade resolution: check `resolved_prefs:{userId}` cache → load org settings (60-min TTL) → load dept prefs (30-min TTL) → load user prefs (15-min TTL) → merge User > Department > Organization > System Default → cache result at `resolved_prefs:{userId}` with 5-min TTL
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6_

  - [x] 9.5 Implement `PlatformAdminService`
    - `GetByUsernameAsync` — return PlatformAdmin record for SecurityService auth resolution
    - `UpdatePasswordAsync` — update PlatformAdmin password hash
    - _Requirements: 16.3, 16.4, 16.5, 16.6, 18.1, 18.4_

- [x] 10. Infrastructure layer — Service clients and DI registration
  - [x] 10.1 Create `ISecurityServiceClient` and `SecurityServiceClient` typed client
    - `GenerateCredentialsAsync` — call SecurityService `POST /api/v1/auth/credentials/generate` with service JWT
    - Automatic service token refresh when within 30 seconds of expiry
    - Propagate `X-Organization-Id` header from current request context
    - _Requirements: 25.1, 25.2, 41.1, 41.2, 41.3, 44.1_

  - [x] 10.2 Create `IUtilityServiceClient` and `UtilityServiceClient` typed client
    - `GetErrorCodeAsync` — call UtilityService error code registry endpoint
    - _Requirements: 39.2, 44.3_

  - [x] 10.3 Create `CorrelationIdDelegatingHandler`
    - Propagate `X-Correlation-Id` header on all outgoing HTTP calls
    - Propagate `X-Organization-Id` header when available
    - _Requirements: 25.5, 37.3_

  - [x] 10.4 Create `DependencyInjection` extension class for Infrastructure service registration
    - Register all services, repositories, typed HTTP clients with Polly policies (3 retries exponential, circuit breaker 5/30s, 10s timeout)
    - Register Redis `IConnectionMultiplexer`
    - Register `ProfileDbContext` with PostgreSQL
    - Register `IHttpContextAccessor`
    - _Requirements: 25.2, 25.3, 25.4_

- [x] 11. Checkpoint — Verify Infrastructure layer compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Api layer — Middleware pipeline
  - [x] 12.1 Implement `CorrelationIdMiddleware`
    - Generate or propagate `X-Correlation-Id` header, store in `HttpContext.Items["CorrelationId"]`, include in response headers
    - _Requirements: 37.1, 37.2, 37.4_

  - [x] 12.2 Implement `GlobalExceptionHandlerMiddleware`
    - Catch `DomainException` → resolve via `IErrorCodeResolverService` → return `ApiResponse<object>` with `application/problem+json`
    - Catch `RateLimitExceededException` → add `Retry-After` header
    - Catch unhandled exceptions → return 500 `INTERNAL_ERROR`, no stack trace leakage, publish error event to `outbox:profile`
    - _Requirements: 26.1, 26.2, 26.3, 26.4, 40.2, 40.3_

  - [x] 12.3 Implement `RateLimiterMiddleware`
    - Apply rate limiting on unauthenticated endpoints (invite validate/accept)
    - _Requirements: 23.1 (RATE_LIMIT_EXCEEDED 3016)_

  - [x] 12.4 Implement `JwtClaimsMiddleware`
    - Extract JWT claims (userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti) and store in `HttpContext.Items`
    - _Requirements: 18.2, 36.3_

  - [x] 12.5 Implement `TokenBlacklistMiddleware`
    - Check `blacklist:{jti}` in Redis for every authenticated request
    - Return 401 `TOKEN_REVOKED` if blacklisted
    - _Requirements: 21.1_

  - [x] 12.6 Implement `FirstTimeUserMiddleware`
    - Block all endpoints except `POST /api/v1/password/forced-change` when `IsFirstTimeUser=true`
    - Return 403 `FIRST_TIME_USER_RESTRICTED`
    - _Requirements: 18.5_

  - [x] 12.7 Implement `RoleAuthorizationMiddleware`
    - Extract `roleName` and `departmentId` from JWT claims
    - OrgAdmin → organization-wide access
    - DeptLead → department-scoped access (own department only)
    - PlatformAdmin → access to PlatformAdmin-decorated endpoints
    - Return 403 `INSUFFICIENT_PERMISSIONS` on failure
    - _Requirements: 17.6, 18.3_

  - [x] 12.8 Implement `OrganizationScopeMiddleware` with PlatformAdmin bypass
    - Extract `organizationId` from JWT claims, validate against route/query params
    - Skip for service-auth tokens
    - **Bypass for PlatformAdmin-authenticated requests** (PlatformAdmin JWT has `roleName = "PlatformAdmin"` and no `organizationId` claim)
    - Return 403 `ORGANIZATION_MISMATCH` (3015) on cross-org access
    - _Requirements: 18.3, 36.3, 36.4, 36.5, 40.5_

  - [x] 12.9 Create `PlatformAdminAttribute` custom authorization attribute
    - Mark endpoints that require PlatformAdmin role
    - Used by `RoleAuthorizationMiddleware` to enforce PlatformAdmin access
    - _Requirements: 17.6_

  - [x] 12.10 Create `ServiceAuthAttribute` for service-to-service endpoint protection
    - Validate service JWT on endpoints marked with `[ServiceAuth]`
    - Return 403 `SERVICE_NOT_AUTHORIZED` if invalid
    - _Requirements: 12.3, 16.5_

  - [x] 12.11 Create `MiddlewarePipelineExtensions` to register middleware in correct order
    - CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → FirstTimeUserGuard → RoleAuthorization → OrganizationScope → Controllers
    - _Requirements: 40.1_

- [x] 13. Api layer — Controllers
  - [x] 13.1 Implement `OrganizationController`
    - `POST /api/v1/organizations` — OrgAdmin, PlatformAdmin — CreateOrganizationRequest → 201 OrganizationResponse
    - `GET /api/v1/organizations` — PlatformAdmin — Paginated list of all organizations (cross-org)
    - `GET /api/v1/organizations/{id}` — Bearer — OrganizationResponse
    - `PUT /api/v1/organizations/{id}` — OrgAdmin — UpdateOrganizationRequest → OrganizationResponse
    - `PATCH /api/v1/organizations/{id}/status` — OrgAdmin, PlatformAdmin — StatusChangeRequest → 200
    - `PUT /api/v1/organizations/{id}/settings` — OrgAdmin — OrganizationSettingsRequest → OrganizationSettingsResponse
    - `POST /api/v1/organizations/{id}/provision-admin` — PlatformAdmin — ProvisionAdminRequest → 201 TeamMemberDetailResponse
    - All responses wrapped in `ApiResponse<T>` with CorrelationId
    - _Requirements: 1, 2, 17.1, 17.2, 17.5, 20.1_

  - [x] 13.2 Implement `DepartmentController`
    - `POST /api/v1/departments` — OrgAdmin — CreateDepartmentRequest → 201 DepartmentResponse
    - `GET /api/v1/departments` — Bearer — Paginated DepartmentResponse list
    - `GET /api/v1/departments/{id}` — Bearer — DepartmentResponse (with member count)
    - `PUT /api/v1/departments/{id}` — OrgAdmin, DeptLead (own) — UpdateDepartmentRequest → DepartmentResponse
    - `PATCH /api/v1/departments/{id}/status` — OrgAdmin — StatusChangeRequest → 200
    - `GET /api/v1/departments/{id}/members` — Bearer — Paginated TeamMemberResponse list
    - `GET /api/v1/departments/{id}/preferences` — Bearer — DepartmentPreferencesResponse
    - `PUT /api/v1/departments/{id}/preferences` — OrgAdmin, DeptLead (own) — DepartmentPreferencesRequest → DepartmentPreferencesResponse
    - _Requirements: 3, 13, 20.1_

  - [x] 13.3 Implement `TeamMemberController`
    - `GET /api/v1/team-members` — Bearer — Paginated TeamMemberResponse list (filterable)
    - `GET /api/v1/team-members/{id}` — Bearer — TeamMemberDetailResponse
    - `PUT /api/v1/team-members/{id}` — OrgAdmin, DeptLead, Self — UpdateTeamMemberRequest → TeamMemberDetailResponse
    - `PATCH /api/v1/team-members/{id}/status` — OrgAdmin — StatusChangeRequest → 200
    - `PATCH /api/v1/team-members/{id}/availability` — Bearer, Self — AvailabilityRequest → 200
    - `POST /api/v1/team-members/{id}/departments` — OrgAdmin — AddDepartmentRequest → 200
    - `DELETE /api/v1/team-members/{id}/departments/{deptId}` — OrgAdmin — 200
    - `PATCH /api/v1/team-members/{id}/departments/{deptId}/role` — OrgAdmin — ChangeRoleRequest → 200
    - `GET /api/v1/team-members/by-email/{email}` — [ServiceAuth] — TeamMemberInternalResponse
    - `PATCH /api/v1/team-members/{id}/password` — [ServiceAuth] — {passwordHash} → 200
    - _Requirements: 4, 5, 6, 11, 12, 20.1_

  - [x] 13.4 Implement `RoleController`
    - `GET /api/v1/roles` — Bearer — List of RoleResponse
    - `GET /api/v1/roles/{id}` — Bearer — RoleResponse
    - _Requirements: 7, 20.1_

  - [x] 13.5 Implement `InviteController`
    - `POST /api/v1/invites` — OrgAdmin, DeptLead — CreateInviteRequest → 201 InviteResponse
    - `GET /api/v1/invites` — OrgAdmin, DeptLead — Paginated InviteResponse list
    - `GET /api/v1/invites/{token}/validate` — None — InviteValidationResponse
    - `POST /api/v1/invites/{token}/accept` — None — AcceptInviteRequest → 200
    - `DELETE /api/v1/invites/{id}` — OrgAdmin, DeptLead — 200
    - _Requirements: 8, 20.1_

  - [x] 13.6 Implement `DeviceController`
    - `GET /api/v1/devices` — Bearer — List of DeviceResponse
    - `PATCH /api/v1/devices/{id}/primary` — Bearer — 200
    - `DELETE /api/v1/devices/{id}` — Bearer — 200
    - _Requirements: 9, 20.1_

  - [x] 13.7 Implement `NotificationSettingController`
    - `GET /api/v1/notification-settings` — Bearer — List of NotificationSettingResponse
    - `PUT /api/v1/notification-settings/{typeId}` — Bearer — UpdateNotificationSettingRequest → 200
    - `GET /api/v1/notification-types` — Bearer — List of NotificationTypeResponse
    - _Requirements: 10, 20.1_

  - [x] 13.8 Implement `PreferenceController`
    - `GET /api/v1/preferences` — Bearer — UserPreferencesResponse
    - `PUT /api/v1/preferences` — Bearer — UserPreferencesRequest → UserPreferencesResponse
    - `GET /api/v1/preferences/resolved` — Bearer — ResolvedPreferencesResponse
    - _Requirements: 14, 15, 20.1_

  - [x] 13.9 Implement `PlatformAdminController`
    - `GET /api/v1/platform-admins/by-username/{username}` — [ServiceAuth] — PlatformAdminInternalResponse
    - `PATCH /api/v1/platform-admins/{id}/password` — [ServiceAuth] — {passwordHash} → 200
    - _Requirements: 16, 18, 20.1_

- [x] 14. Api layer — Program.cs, extensions, and Dockerfile
  - [x] 14.1 Create `Program.cs` with full DI registration and middleware pipeline
    - Load `.env` via DotNetEnv, build `AppSettings`
    - Register Infrastructure services via `DependencyInjection` extension
    - Register FluentValidation validators (auto-discovery), suppress `ModelStateInvalidFilter`
    - Register JWT Bearer authentication
    - Register CORS with `AllowedOrigins`
    - Register health checks (PostgreSQL + Redis)
    - Register Swagger (Development mode only)
    - Apply `DatabaseMigrationHelper` on startup (seed roles + notification types)
    - Build middleware pipeline in correct order via `MiddlewarePipelineExtensions`
    - Map controllers, health check endpoints (`/health`, `/ready`)
    - _Requirements: 27.3, 28.1, 28.2, 29.1, 30.1, 31.1, 40.1, 42.1, 43.1_

  - [x] 14.2 Create `ControllerServiceExtensions` for controller-specific DI
    - Register controllers with `ApiResponse<T>` envelope conventions
    - _Requirements: 20.3_

  - [x] 14.3 Create `SwaggerServiceExtensions`
    - Configure Swagger with JWT Bearer auth support, API info
    - Development mode only
    - _Requirements: 31.1, 31.2_

  - [x] 14.4 Create `HealthCheckExtensions`
    - Register PostgreSQL and Redis health checks
    - Map `/health` (liveness) and `/ready` (readiness) endpoints
    - _Requirements: 28.1, 28.2, 28.3, 28.4_

  - [x] 14.5 Create `Dockerfile` and `.env.example`
    - Multi-stage Dockerfile for ProfileService.Api
    - `.env.example` documenting all environment variables
    - _Requirements: 29.1_

  - [x] 14.6 Configure structured logging conventions
    - Ensure `GlobalExceptionHandlerMiddleware` logs DomainExceptions with: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`
    - Ensure unhandled exception logs include: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`
    - Ensure downstream call failure logs include: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`
    - _Requirements: 32.1, 32.2, 32.3_

- [x] 15. Checkpoint — Full build verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

- [x] 16. SecurityService cross-cutting updates — PlatformAdmin support
  - [x] 16.1 Add `PlatformAdmin` to SecurityService `RoleNames` static class
    - Add `PlatformAdmin` constant (no PermissionLevel — PlatformAdmin is not part of the organization-scoped role hierarchy)
    - _Requirements: 19.1_

  - [x] 16.2 Create `PlatformAdminAttribute` in SecurityService Api `Attributes/` folder
    - Custom authorization attribute for PlatformAdmin-only endpoints
    - _Requirements: 19.3_

  - [x] 16.3 Update SecurityService `RoleAuthorizationMiddleware` for PlatformAdmin
    - Recognize `roleName = "PlatformAdmin"` from JWT claims
    - Grant access to endpoints decorated with `PlatformAdminAttribute`
    - _Requirements: 19.2_

  - [x] 16.4 Update SecurityService `OrganizationScopeMiddleware` for PlatformAdmin bypass
    - Skip organization scope enforcement for PlatformAdmin-authenticated requests
    - _Requirements: 19.5_

  - [x] 16.5 Update SecurityService `AuthService` login flow for PlatformAdmin
    - When login request uses username (not email), resolve via ProfileService `GET /api/v1/platform-admins/by-username/{username}`
    - Issue JWT with `userId = PlatformAdminId`, `roleName = "PlatformAdmin"`, no `organizationId` or `departmentId` claims
    - _Requirements: 19.4_

  - [x] 16.6 Update SecurityService `ProfileServiceClient` for PlatformAdmin endpoints
    - Add `GetPlatformAdminByUsernameAsync` method calling ProfileService `GET /api/v1/platform-admins/by-username/{username}`
    - Add `UpdatePlatformAdminPasswordAsync` method calling ProfileService `PATCH /api/v1/platform-admins/{id}/password`
    - _Requirements: 19.4, 19.6_

  - [x] 16.7 Update SecurityService `PasswordService` for PlatformAdmin password management
    - Support forced password change and password reset flows for PlatformAdmin using ProfileService's PlatformAdmin password endpoint
    - _Requirements: 19.6_

- [x] 17. Testing
  - [x] 17.1 Set up test project infrastructure
    - Create `TestDbContextFactory` helper using InMemory database
    - Create FsCheck generators: `OrganizationGenerator`, `DepartmentGenerator`, `TeamMemberGenerator`, `InviteGenerator`, `PreferenceGenerator`
    - _Requirements: 35.6_

  - [x] 17.2 Write property tests for organization management
    - **Property 1: Organization create-read round trip**
    - **Validates: Requirements 1.1, 1.3**
    - **Property 2: Organization name uniqueness**
    - **Validates: Requirements 1.2**
    - **Property 3: StoryIdPrefix format validation**
    - **Validates: Requirements 2.2**
    - **Property 4: StoryIdPrefix uniqueness across organizations**
    - **Validates: Requirements 2.3**
    - **Property 5: Organization settings update round trip**
    - **Validates: Requirements 2.1, 2.6**
    - **Property 37: Organization status lifecycle transitions**
    - **Validates: Requirements 1.5**
    - **Property 38: DefaultSprintDurationWeeks range validation**
    - **Validates: Requirements 2.5**

  - [x] 17.3 Write property tests for department management
    - **Property 6: Department uniqueness constraints**
    - **Validates: Requirements 3.2, 3.3**
    - **Property 7: Custom department creation sets IsDefault to false**
    - **Validates: Requirements 3.1**
    - **Property 8: Default department deletion prevention**
    - **Validates: Requirements 3.8**
    - **Property 9: Department deactivation with active members prevention**
    - **Validates: Requirements 3.7**

  - [x] 17.4 Write property tests for team member management
    - **Property 10: Team member filtering**
    - **Validates: Requirements 4.1**
    - **Property 11: Last OrgAdmin deactivation prevention**
    - **Validates: Requirements 4.4**
    - **Property 12: Availability validation**
    - **Validates: Requirements 4.5**
    - **Property 13: Multi-department membership with different roles**
    - **Validates: Requirements 5.5**
    - **Property 14: Department membership duplicate prevention**
    - **Validates: Requirements 5.2**
    - **Property 15: Last department removal prevention**
    - **Validates: Requirements 5.3**
    - **Property 16: Professional ID format and uniqueness**
    - **Validates: Requirements 6.1, 6.2**
    - **Property 17: Professional ID immutability on department transfer**
    - **Validates: Requirements 6.3**

  - [x] 17.5 Write property tests for invitation system
    - **Property 18: Invite token generation and validation round trip**
    - **Validates: Requirements 8.1, 8.5**
    - **Property 19: Invite acceptance creates all required artifacts**
    - **Validates: Requirements 8.6**
    - **Property 20: Expired or used invite rejection**
    - **Validates: Requirements 8.7**
    - **Property 21: Invite email duplicate prevention**
    - **Validates: Requirements 8.2**
    - **Property 39: DeptLead invite scoping**
    - **Validates: Requirements 8.3**

  - [x] 17.6 Write property tests for device management
    - **Property 22: Device limit enforcement**
    - **Validates: Requirements 9.2**
    - **Property 23: Primary device exclusivity**
    - **Validates: Requirements 9.3**

  - [x] 17.7 Write property tests for preference cascade and user preferences
    - **Property 24: Preference cascade resolution order**
    - **Validates: Requirements 15.1, 15.2**
    - **Property 25: Preference update round trip**
    - **Validates: Requirements 14.1, 14.2**
    - **Property 26: Invalid preference value rejection**
    - **Validates: Requirements 14.5, 14.6**
    - **Property 27: Cache invalidation on entity update**
    - **Validates: Requirements 2.6, 3.10, 4.6, 5.6, 13.3, 14.3, 21.1**

  - [x] 17.8 Write property tests for cross-cutting concerns
    - **Property 28: Soft delete preserves records**
    - **Validates: Requirements 34.1, 34.2**
    - **Property 29: Organization isolation via global query filters**
    - **Validates: Requirements 36.1, 36.2**
    - **Property 30: Pagination metadata correctness**
    - **Validates: Requirements 33.1, 33.2, 33.3**
    - **Property 31: DomainException produces correct ApiResponse**
    - **Validates: Requirements 20.3, 26.1, 26.4**
    - **Property 32: FluentValidation produces 422 with field errors**
    - **Validates: Requirements 20.4, 27.1, 27.2**
    - **Property 33: Seed data idempotency**
    - **Validates: Requirements 22.1, 22.3**
    - **Property 36: Outbox message contains required fields**
    - **Validates: Requirements 1.6, 38.1**

  - [x] 17.9 Write property tests for PlatformAdmin
    - **Property 34: PlatformAdmin lookup round trip**
    - **Validates: Requirements 16.3, 18.1**
    - **Property 35: PlatformAdmin bypasses organization scope**
    - **Validates: Requirements 17.5, 17.6, 18.3**

  - [x] 17.10 Write unit tests for FluentValidation validators
    - Test each validator with valid and invalid inputs
    - Verify StoryIdPrefix regex (2–10 uppercase alphanumeric)
    - Verify availability enum values
    - Verify preference enum values (Theme, BoardView, DigestFrequency, DateFormat, TimeFormat)
    - Verify OTP code format (6-digit numeric)
    - **Validates: Requirements 2.2, 4.5, 14.5, 14.6, 27.1, 27.2**

  - [x] 17.11 Write unit tests for service layer business logic
    - Test OrganizationService: create with seed departments, name uniqueness, StoryIdPrefix validation
    - Test DepartmentService: default department protection, active member guard
    - Test TeamMemberService: last OrgAdmin guard, professional ID generation, multi-department roles
    - Test InviteService: token generation, expiry check, acceptance flow
    - Test DeviceService: max 5 device limit, primary device toggle
    - Test PreferenceResolver: cascade merge with all levels populated, partial levels, system defaults only
    - Test PlatformAdminService: username lookup, password update
    - **Validates: Requirements 1, 3, 4, 5, 6, 8, 9, 15, 16**

  - [x] 17.12 Write unit tests for middleware
    - Test GlobalExceptionHandlerMiddleware returns correct ApiResponse for DomainException and unhandled exceptions
    - Test OrganizationScopeMiddleware bypasses for PlatformAdmin and service-auth tokens
    - Test TokenBlacklistMiddleware rejects blacklisted tokens
    - Test FirstTimeUserMiddleware blocks non-password-change endpoints
    - Test RoleAuthorizationMiddleware grants PlatformAdmin access to PlatformAdmin-decorated endpoints
    - **Validates: Requirements 18.3, 26.1, 26.2, 40.1, 40.5**

- [x] 18. Final checkpoint — Full integration verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

## Notes

- All ProfileService projects live under `src/backend/ProfileService/` in the monorepo
- Tests are co-located at `src/backend/ProfileService/ProfileService.Tests/`
- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The implementation language is C# (.NET 8) as specified in the design document
- Checkpoints ensure incremental validation at layer boundaries
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
- Task 16 modifies the existing SecurityService codebase to add PlatformAdmin support (Requirement 19)
- Organization, Role, NotificationType, and PlatformAdmin entities are NOT organization-scoped — no global query filters
- All other entities implement `IOrganizationEntity` and are filtered by `OrganizationId`
