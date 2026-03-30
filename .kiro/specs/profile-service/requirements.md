# Requirements Document — ProfileService

## Introduction

This document defines the complete requirements for the Nexus-2.0 ProfileService — the identity, organization, and profile management microservice of the Enterprise Agile Platform. ProfileService runs on port 5002 with database `nexus_profile` and follows Clean Architecture (.NET 8) with Domain / Application / Infrastructure / Api layers.

ProfileService is the single source of truth for all TeamMember records and user identity. It manages the organization hierarchy, department structure, team member profiles, role assignments, invitation system, device management, notification settings, user preferences, department preferences, preference cascade resolution, and story ID prefix configuration. Other services (SecurityService, WorkService) call ProfileService to resolve user identity and organization settings.

ProfileService also manages PlatformAdmin entities — platform-level administrators who exist above the organization scope and can create organizations, provision the first OrgAdmin, and manage organization lifecycle. PlatformAdmin is a separate entity from TeamMember, with its own login flow and JWT claims (no `organizationId` or `departmentId`).

All requirements are derived from the platform documentation:
- `docs/nexus-2.0-backend-requirements.md` (REQ-021 – REQ-035, REQ-086 – REQ-108)
- `docs/nexus-2.0-backend-specification.md` (ProfileService specification, sections 5.1–5.13)
- `docs/platform-specification.md` (predecessor WEP PlatformAdmin patterns)
- `.kiro/specs/security-service/requirements.md` (SecurityService integration points)

## Glossary

- **ProfileService**: Microservice (port 5002, database `nexus_profile`) responsible for organization management, department management, team member profiles, role management, invitation system, device management, notification settings, user preferences, department preferences, preference cascade resolution, story ID prefix configuration, and PlatformAdmin management.
- **SecurityService**: Microservice (port 5001) responsible for authentication, JWT issuance, session management, and RBAC. ProfileService calls SecurityService for credential generation; SecurityService calls ProfileService for user identity resolution.
- **Organization**: Top-level tenant entity. All team member data is scoped to an organization via `OrganizationId`. Equivalent to "Tenant" in the predecessor WEP platform.
- **Department**: Functional unit within an organization (e.g., Engineering, QA, DevOps, Product, Design). Five predefined departments are seeded on organization creation; custom departments can be added.
- **TeamMember**: A user within an organization, assigned to one or more departments with department-scoped roles. Equivalent to "SmeUser" in the predecessor WEP platform. ProfileService is the source of truth for all TeamMember records.
- **PlatformAdmin**: A platform-level administrator who exists above the organization scope. PlatformAdmin is NOT a TeamMember — it is a separate entity with its own login flow, JWT claims (`userId`, `roleName="PlatformAdmin"`, no `organizationId`, no `departmentId`), and lifecycle. Modeled after the WEP PlatformAdmin role.
- **DepartmentMember**: Join entity linking a TeamMember to a Department with a specific Role. A team member can belong to multiple departments with different roles.
- **Role**: Department-scoped permission level — OrgAdmin (100), DeptLead (75), Member (50), Viewer (25). Roles are system-seeded and marked `IsSystemRole=true`.
- **Invite**: An invitation record containing a cryptographic token (128 chars max) with 48-hour expiry, used to onboard new team members into an organization and department.
- **Device**: A registered device for a team member, with a maximum of 5 devices per user. One device can be marked as primary.
- **NotificationSetting**: Per-user, per-notification-type preference controlling which channels (Email, Push, InApp) are enabled.
- **NotificationType**: A predefined notification category. Eight types are seeded: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching.
- **OrganizationSettings**: Typed class deserialized from the `SettingsJson` JSON column on the Organization entity. Contains all organization-level preferences (general, workflow, board, notification, data settings).
- **DepartmentPreferences**: Typed class deserialized from the `PreferencesJson` JSON column on the Department entity. Contains department-level preference overrides.
- **UserPreferences**: Entity storing per-user preferences with typed fields, linked to `TeamMemberId`. Overrides organization and department defaults.
- **Preference_Cascade**: Resolution order for preferences: User → Department → Organization → System Default. Highest priority wins.
- **StoryIdPrefix**: A 2–10 uppercase alphanumeric string configured per organization, used as the prefix for professional story IDs (e.g., `NEXUS`, `ACME`). Must be unique across all organizations and immutable after stories exist.
- **Professional_ID**: Team member identifier in the format `NXS-{DeptCode}-{SequentialNumber}` (e.g., `NXS-ENG-001`). Unique within the organization, sequential within the department, and immutable after creation.
- **FlgStatus**: Soft-delete lifecycle field — `A` (Active), `S` (Suspended), `D` (Deactivated).
- **Invite_FlgStatus**: Invite-specific lifecycle field — `A` (Active), `U` (Used), `E` (Expired).
- **ApiResponse**: Standardized JSON envelope `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, `Errors` fields.
- **DomainException**: Base exception class for business rule violations, containing `ErrorValue`, `ErrorCode`, `StatusCode`, and `CorrelationId`.
- **CorrelationId**: End-to-end trace identifier (`X-Correlation-Id` header) propagated across all service calls.
- **Outbox**: Redis-based async messaging pattern. ProfileService publishes audit events and notifications to `outbox:profile`. UtilityService polls and processes the queue.
- **IOrganizationEntity**: Marker interface for entities scoped to an organization, enabling EF Core global query filters by `OrganizationId`.
- **Polly**: .NET resilience library used for retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s) on inter-service calls.
- **BCrypt**: Password hashing algorithm used for all password storage.
- **Clean_Architecture**: Four-layer architecture — Domain (entities, interfaces), Application (DTOs, validators), Infrastructure (EF Core, Redis, HTTP clients), Api (controllers, middleware).
- **Service_JWT**: Short-lived JWT for inter-service communication, issued by SecurityService and cached in Redis. Used by ProfileService when calling SecurityService and by SecurityService when calling ProfileService.

## Requirements

### Requirement 1: Organization Management (REQ-021)

**User Story:** As an OrgAdmin, I want to create and manage organizations so that the platform can support multiple tenants.

#### Acceptance Criteria

1. WHEN an authorized user calls `POST /api/v1/organizations` with valid data, THE ProfileService SHALL create the organization with `FlgStatus = 'A'`, seed 5 default departments (Engineering/ENG, QA/QA, DevOps/DEVOPS, Product/PROD, Design/DESIGN with `IsDefault=true`), and return HTTP 201.
2. WHEN the organization name already exists, THE ProfileService SHALL return HTTP 409 with `ORGANIZATION_NAME_DUPLICATE` (3005).
3. WHEN `GET /api/v1/organizations/{id}` is called with a valid Bearer token, THE ProfileService SHALL return the organization details including `OrganizationName`, `StoryIdPrefix`, `TimeZone`, `DefaultSprintDurationWeeks`, `Description`, `Website`, `LogoUrl`, `SettingsJson`, `FlgStatus`, `DateCreated`, and `DateUpdated`.
4. WHEN `PUT /api/v1/organizations/{id}` is called by an OrgAdmin, THE ProfileService SHALL update the organization fields and return HTTP 200.
5. WHEN `PATCH /api/v1/organizations/{id}/status` is called by an OrgAdmin or PlatformAdmin, THE ProfileService SHALL transition the organization through the `A → S → D` lifecycle and return HTTP 200.
6. WHEN an organization is created, THE ProfileService SHALL publish an audit event to `outbox:profile` with action `OrganizationCreated`.

### Requirement 2: Organization Settings (REQ-022)

**User Story:** As an OrgAdmin, I want to configure organization-level settings so that the platform adapts to our workflow.

#### Acceptance Criteria

1. WHEN `PUT /api/v1/organizations/{id}/settings` is called by an OrgAdmin, THE ProfileService SHALL update the organization settings stored as a JSON column `SettingsJson` on the Organization entity, deserialized into a typed `OrganizationSettings` class, and return HTTP 200.
2. WHEN the `StoryIdPrefix` is set, THE ProfileService SHALL validate it matches `^[A-Z0-9]{2,10}$` (2–10 uppercase alphanumeric characters). IF the format is invalid, THEN THE ProfileService SHALL return HTTP 400 with `STORY_PREFIX_INVALID_FORMAT` (3020).
3. WHEN the `StoryIdPrefix` already exists for another organization, THE ProfileService SHALL return HTTP 409 with `STORY_PREFIX_DUPLICATE` (3006).
4. WHEN the `StoryIdPrefix` is changed after stories exist (checked via WorkService), THE ProfileService SHALL return HTTP 400 with `STORY_PREFIX_IMMUTABLE` (3007).
5. WHEN `DefaultSprintDurationWeeks` is set, THE ProfileService SHALL validate it is between 1 and 4 (inclusive).
6. WHEN organization settings are updated, THE ProfileService SHALL invalidate the Redis cache at `org_settings:{organizationId}`.
7. THE ProfileService SHALL support the following `OrganizationSettings` fields with their defaults:

| Category | Field | Type | Default |
|----------|-------|------|---------|
| General | `StoryIdPrefix` | string (2–10 uppercase alphanumeric) | (required) |
| General | `TimeZone` | IANA timezone string | `"UTC"` |
| General | `DefaultSprintDurationWeeks` | int (1–4) | `2` |
| General | `WorkingDays` | string[] | `["Monday"..."Friday"]` |
| General | `WorkingHoursStart` | string | `"09:00"` |
| General | `WorkingHoursEnd` | string | `"17:00"` |
| General | `LogoUrl` | string? | `null` |
| General | `PrimaryColor` | hex string? | `null` |
| Workflow | `StoryPointScale` | enum | `Fibonacci` |
| Workflow | `RequiredFieldsByStoryType` | Dictionary | `{}` |
| Workflow | `AutoAssignmentEnabled` | bool | `false` |
| Workflow | `AutoAssignmentStrategy` | enum | `LeastLoaded` |
| Board | `DefaultBoardView` | enum | `Kanban` |
| Board | `WipLimitsEnabled` | bool | `false` |
| Board | `DefaultWipLimit` | int | `0` |
| Notification | `DefaultNotificationChannels` | string | `"Email,Push,InApp"` |
| Notification | `DigestFrequency` | enum | `Realtime` |
| Data | `AuditRetentionDays` | int | `90` |

### Requirement 3: Department Management (REQ-023)

**User Story:** As an OrgAdmin, I want to manage departments so that the organization structure reflects our team composition.

#### Acceptance Criteria

1. WHEN `POST /api/v1/departments` is called by an OrgAdmin with valid data, THE ProfileService SHALL create a custom department with `IsDefault=false` and return HTTP 201.
2. WHEN the department name already exists within the organization, THE ProfileService SHALL return HTTP 409 with `DEPARTMENT_NAME_DUPLICATE` (3008).
3. WHEN the department code already exists within the organization, THE ProfileService SHALL return HTTP 409 with `DEPARTMENT_CODE_DUPLICATE` (3009).
4. WHEN `GET /api/v1/departments` is called with a valid Bearer token, THE ProfileService SHALL return all departments for the organization (paginated), cached in Redis at `dept_list:{organizationId}` with 30-minute TTL.
5. WHEN `GET /api/v1/departments/{id}` is called, THE ProfileService SHALL return department details including member count.
6. WHEN `PUT /api/v1/departments/{id}` is called by an OrgAdmin or DeptLead (own department only), THE ProfileService SHALL update the department and return HTTP 200.
7. WHEN `PATCH /api/v1/departments/{id}/status` is called to deactivate a department with active members, THE ProfileService SHALL return HTTP 400 with `DEPARTMENT_HAS_ACTIVE_MEMBERS` (3017).
8. WHEN a predefined department (Engineering, QA, DevOps, Product, Design) is targeted for deletion, THE ProfileService SHALL return HTTP 400 with `DEFAULT_DEPARTMENT_CANNOT_DELETE` (3010).
9. WHEN `GET /api/v1/departments/{id}/members` is called, THE ProfileService SHALL return all team members in that department with their roles.
10. WHEN a department is created or updated, THE ProfileService SHALL invalidate the Redis cache at `dept_list:{organizationId}`.

### Requirement 4: Team Member Management (REQ-024)

**User Story:** As an OrgAdmin or DeptLead, I want to manage team members so that the right people are in the right departments with the right roles.

#### Acceptance Criteria

1. WHEN `GET /api/v1/team-members` is called with a valid Bearer token, THE ProfileService SHALL return a paginated list of team members, filterable by department, role, status, and availability.
2. WHEN `GET /api/v1/team-members/{id}` is called, THE ProfileService SHALL return the full team member profile including all department memberships with roles, skills, availability, `MaxConcurrentTasks`, and current active task count.
3. WHEN `PUT /api/v1/team-members/{id}` is called by an OrgAdmin, DeptLead (for members in their department), or the member themselves, THE ProfileService SHALL update the profile and return HTTP 200.
4. WHEN `PATCH /api/v1/team-members/{id}/status` is called by an OrgAdmin to deactivate the last OrgAdmin in the organization, THE ProfileService SHALL return HTTP 400 with `LAST_ORGADMIN_CANNOT_DEACTIVATE` (3004).
5. WHEN `PATCH /api/v1/team-members/{id}/availability` is called by the member, THE ProfileService SHALL update availability to one of: `Available`, `Busy`, `Away`, `Offline`. IF an invalid value is provided, THEN THE ProfileService SHALL return HTTP 400 with `INVALID_AVAILABILITY_STATUS` (3019).
6. WHEN a team member profile is updated, THE ProfileService SHALL invalidate the Redis cache at `member_profile:{memberId}`.

### Requirement 5: Team Member Department Assignment (REQ-025)

**User Story:** As an OrgAdmin, I want to assign team members to multiple departments with different roles so that cross-functional collaboration is supported.

#### Acceptance Criteria

1. WHEN `POST /api/v1/team-members/{id}/departments` is called by an OrgAdmin with `{departmentId, roleId}`, THE ProfileService SHALL create a `DepartmentMember` record and return HTTP 200.
2. WHEN the member is already in the target department, THE ProfileService SHALL return HTTP 409 with `MEMBER_ALREADY_IN_DEPARTMENT` (3011).
3. WHEN `DELETE /api/v1/team-members/{id}/departments/{deptId}` is called to remove the member's last department, THE ProfileService SHALL return HTTP 400 with `MEMBER_MUST_HAVE_DEPARTMENT` (3012).
4. WHEN `PATCH /api/v1/team-members/{id}/departments/{deptId}/role` is called by an OrgAdmin, THE ProfileService SHALL update the member's role in that specific department and return HTTP 200.
5. WHEN a team member belongs to multiple departments, THE ProfileService SHALL allow different roles in each department (e.g., DeptLead in Engineering, Member in QA).
6. WHEN a department membership is added or removed, THE ProfileService SHALL invalidate the Redis cache at `member_profile:{memberId}` and `dept_list:{organizationId}`.

### Requirement 6: Professional ID System for Team Members (REQ-026)

**User Story:** As the platform, I want team members to have professional IDs so that they are easily identifiable across the organization.

#### Acceptance Criteria

1. WHEN a team member is created, THE ProfileService SHALL generate a professional ID in the format `NXS-{DeptCode}-{SequentialNumber}` (e.g., `NXS-ENG-001` for the first Engineering member).
2. WHEN the professional ID is generated, THE ProfileService SHALL ensure it is unique within the organization and sequential within the department.
3. WHEN a team member transfers departments, THE ProfileService SHALL retain the original professional ID unchanged (it reflects the original department assignment).

### Requirement 7: Role Management (REQ-027)

**User Story:** As a team member, I want to understand the role hierarchy so that I know what permissions I have.

#### Acceptance Criteria

1. WHEN `GET /api/v1/roles` is called with a valid Bearer token, THE ProfileService SHALL return all roles: OrgAdmin (PermissionLevel 100), DeptLead (75), Member (50), Viewer (25).
2. WHEN `GET /api/v1/roles/{id}` is called, THE ProfileService SHALL return role details including `RoleName`, `Description`, `PermissionLevel`, and `IsSystemRole`.
3. WHEN the ProfileService database is initialized, THE ProfileService SHALL seed the 4 system roles and mark each as `IsSystemRole=true`.

### Requirement 8: Invitation System (REQ-028)

**User Story:** As an OrgAdmin or DeptLead, I want to invite new team members to join the organization so that onboarding is streamlined.

#### Acceptance Criteria

1. WHEN `POST /api/v1/invites` is called with `{email, firstName, lastName, departmentId, roleId}` by an OrgAdmin or DeptLead, THE ProfileService SHALL generate a cryptographic token (128 chars max), set a 48-hour expiry, publish an email notification to `outbox:profile`, and return HTTP 201.
2. WHEN the invitee's email is already registered as a member in the organization, THE ProfileService SHALL return HTTP 409 with `INVITE_EMAIL_ALREADY_MEMBER` (3014).
3. WHEN a DeptLead creates an invite, THE ProfileService SHALL scope the invite to the DeptLead's own department only.
4. WHEN `GET /api/v1/invites` is called by an OrgAdmin or DeptLead, THE ProfileService SHALL return all pending invites (OrgAdmin sees all; DeptLead sees only their department's invites).
5. WHEN `GET /api/v1/invites/{token}/validate` is called (no auth required), THE ProfileService SHALL return invite details (organization name, department name, role) if the token is valid and not expired.
6. WHEN `POST /api/v1/invites/{token}/accept` is called with OTP verification (no auth required), THE ProfileService SHALL create a TeamMember, create a DepartmentMember with the specified role, generate a professional ID, call SecurityService `POST /api/v1/auth/credentials/generate` to generate initial credentials, update the invite `FlgStatus` to `U` (Used), and return HTTP 200.
7. WHEN the invite token is expired or already used, THE ProfileService SHALL return HTTP 410 with `INVITE_EXPIRED_OR_INVALID` (3002).
8. WHEN `DELETE /api/v1/invites/{id}` is called by an OrgAdmin or DeptLead, THE ProfileService SHALL cancel the invite and return HTTP 200.

### Requirement 9: Device Management (REQ-029)

**User Story:** As a team member, I want to manage my registered devices so that I can control which devices have access.

#### Acceptance Criteria

1. WHEN `GET /api/v1/devices` is called with a valid Bearer token, THE ProfileService SHALL return all devices for the authenticated user with `DeviceName`, `DeviceType` (Desktop/Mobile/Tablet), `IsPrimary`, `IpAddress`, `UserAgent`, `LastActiveDate`, and `FlgStatus`.
2. WHEN a new device is registered and the user already has 5 devices, THE ProfileService SHALL return HTTP 400 with `MAX_DEVICES_REACHED` (3003).
3. WHEN `PATCH /api/v1/devices/{id}/primary` is called, THE ProfileService SHALL set the specified device as primary and unset the previous primary device.
4. WHEN `DELETE /api/v1/devices/{id}` is called, THE ProfileService SHALL remove the device and revoke its associated session.

### Requirement 10: Notification Settings (REQ-030)

**User Story:** As a team member, I want to configure my notification preferences so that I only receive relevant notifications.

#### Acceptance Criteria

1. WHEN `GET /api/v1/notification-settings` is called with a valid Bearer token, THE ProfileService SHALL return per-notification-type preferences with `IsEmail`, `IsPush`, and `IsInApp` toggles.
2. WHEN `PUT /api/v1/notification-settings/{typeId}` is called, THE ProfileService SHALL update the preference for that notification type and return HTTP 200.
3. WHEN `GET /api/v1/notification-types` is called, THE ProfileService SHALL return all 8 notification types: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching.
4. WHEN notification channel preferences are resolved, THE ProfileService SHALL cascade in this order: User preference → Department override (`NotificationChannelOverrides`) → Organization default (`DefaultNotificationChannels`) → System default (all channels enabled).

### Requirement 11: Team Member Capacity and Availability Tracking (REQ-031)

**User Story:** As a DeptLead, I want to see team member capacity and availability so that I can make informed task assignment decisions.

#### Acceptance Criteria

1. WHEN a team member profile is retrieved, THE ProfileService SHALL include `MaxConcurrentTasks` (default 5), `Availability` (Available/Busy/Away/Offline), and current active task count.
2. WHEN `MaxConcurrentTasks` is updated, THE ProfileService SHALL validate it is a positive integer.
3. WHEN a team member's availability is `Away` or `Offline`, THE ProfileService SHALL expose this status so that the auto-assignment system in WorkService can exclude the member from suggestions.

### Requirement 12: ProfileService Internal Endpoints (REQ-032)

**User Story:** As a backend service, I want internal endpoints for user resolution so that SecurityService and WorkService can look up team members.

#### Acceptance Criteria

1. WHEN SecurityService calls `GET /api/v1/team-members/by-email/{email}` with a valid service JWT, THE ProfileService SHALL return the team member record including `TeamMemberId`, `PasswordHash`, `FlgStatus`, `IsFirstTimeUser`, `OrganizationId`, `PrimaryDepartmentId`, and `RoleName`.
2. WHEN SecurityService calls `PATCH /api/v1/team-members/{id}/password` with a valid service JWT, THE ProfileService SHALL update the password hash and return HTTP 200.
3. WHEN these internal endpoints are called without a valid service JWT, THE ProfileService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED`.
4. WHEN the email does not match any team member, THE ProfileService SHALL return HTTP 404 with `MEMBER_NOT_FOUND` (3025).

### Requirement 13: Department Preferences (REQ-106)

**User Story:** As a DeptLead, I want to configure department-specific preferences so that my team's workflow is optimized.

#### Acceptance Criteria

1. WHEN `GET /api/v1/departments/{id}/preferences` is called with a valid Bearer token, THE ProfileService SHALL return the department's preferences.
2. WHEN `PUT /api/v1/departments/{id}/preferences` is called by an OrgAdmin or DeptLead (own department only), THE ProfileService SHALL update the department's preferences and return HTTP 200.
3. WHEN department preferences are updated, THE ProfileService SHALL invalidate the Redis cache at `dept_prefs:{departmentId}`.
4. WHEN a department preference is not set, THE ProfileService SHALL fall back to the organization-level default (cascading: Department → Organization → System default).
5. THE ProfileService SHALL support the following `DepartmentPreferences` fields stored as a JSON column `PreferencesJson` on the Department entity:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `DefaultTaskTypes` | string[] | `[]` | Task type codes relevant to this department |
| `CustomWorkflowOverrides` | JsonDocument? | `null` | Custom status names and transitions |
| `WipLimitPerStatus` | Dictionary<string, int>? | `null` | Per-column WIP limits |
| `DefaultAssigneeId` | Guid? | `null` | Default assignee for unassigned tasks |
| `NotificationChannelOverrides` | JsonDocument? | `null` | Department-specific notification channel defaults |
| `MaxConcurrentTasksDefault` | int | `5` | Default MaxConcurrentTasks for new members |

### Requirement 14: User Preferences (REQ-107)

**User Story:** As a team member, I want to configure my personal preferences so that the platform adapts to my workflow.

#### Acceptance Criteria

1. WHEN `GET /api/v1/preferences` is called with a valid Bearer token, THE ProfileService SHALL return the authenticated user's preferences.
2. WHEN `PUT /api/v1/preferences` is called with a valid Bearer token, THE ProfileService SHALL update the authenticated user's preferences and return HTTP 200.
3. WHEN user preferences are updated, THE ProfileService SHALL invalidate the Redis cache at `user_prefs:{userId}`.
4. WHEN a user preference field is not set (null), THE ProfileService SHALL fall back to the organization-level default (cascading: User → Organization → System default).
5. WHEN an invalid preference value is provided (e.g., invalid enum value), THE ProfileService SHALL return HTTP 400 with `INVALID_PREFERENCE_VALUE` (3026).
6. WHEN an unknown preference key is provided, THE ProfileService SHALL return HTTP 400 with `PREFERENCE_KEY_UNKNOWN` (3027).
7. THE ProfileService SHALL support the following `UserPreferences` fields:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Theme` | enum | `System` | `Light`, `Dark`, `System` |
| `Language` | string | `"en"` | ISO 639-1 language code |
| `TimezoneOverride` | string? | `null` | IANA timezone, overrides org timezone |
| `DefaultBoardView` | enum? | `null` | `Kanban`, `Sprint`, `Backlog` |
| `DefaultBoardFilters` | JSON? | `null` | Saved default board filters |
| `DashboardLayout` | JSON? | `null` | Widget configuration and ordering |
| `EmailDigestFrequency` | enum? | `null` | `Realtime`, `Hourly`, `Daily`, `Off` |
| `KeyboardShortcutsEnabled` | bool | `true` | Enable keyboard shortcuts |
| `DateFormat` | enum | `ISO` | `ISO`, `US`, `EU` |
| `TimeFormat` | enum | `H24` | `H24`, `H12` |

### Requirement 15: Preference Cascade Resolution (REQ-108)

**User Story:** As the platform, I want preferences to cascade from system defaults through organization and department to user level so that configuration is flexible but consistent.

#### Acceptance Criteria

1. WHEN a preference value is needed, THE ProfileService SHALL resolve it in this order (highest priority first): User → Department → Organization → System Default.
2. WHEN `GET /api/v1/preferences/resolved` is called with a valid Bearer token, THE ProfileService SHALL return the fully resolved preferences for the authenticated user, merging all levels.
3. WHEN any level's preferences are updated, THE ProfileService SHALL invalidate only that level's cache — downstream resolution happens at read time.
4. WHEN the resolved preferences are computed, THE ProfileService SHALL cache the result in Redis at `resolved_prefs:{userId}` with 5-minute TTL.
5. WHEN any upstream preference level (organization, department) is updated, THE ProfileService SHALL NOT proactively invalidate `resolved_prefs:{userId}` for affected users — the cache expires naturally via the short 5-minute TTL.
6. THE ProfileService SHALL use the following system defaults as the final fallback:

| Preference | Default Value |
|-----------|---------------|
| Theme | `System` |
| Language | `en` |
| Timezone | `UTC` |
| DefaultBoardView | `Kanban` |
| DigestFrequency | `Realtime` |
| NotificationChannels | `Email,Push,InApp` |
| KeyboardShortcutsEnabled | `true` |
| DateFormat | `ISO` |
| TimeFormat | `H24` |
| StoryPointScale | `Fibonacci` |
| AutoAssignmentEnabled | `false` |
| AutoAssignmentStrategy | `LeastLoaded` |
| WipLimitsEnabled | `false` |
| DefaultWipLimit | `0` |
| AuditRetentionDays | `90` |
| MaxConcurrentTasksDefault | `5` |

### Requirement 16: PlatformAdmin Entity Management

**User Story:** As the platform, I want a PlatformAdmin entity that exists above the organization scope so that platform-level operations (creating organizations, provisioning the first OrgAdmin) have a dedicated administrative role.

#### Acceptance Criteria

1. THE ProfileService SHALL maintain a `platform_admin` table with columns: `PlatformAdminId` (Guid, PK), `Username` (string, required, unique), `PasswordHash` (string, required), `Email` (string, required, unique), `FirstName` (string, required), `LastName` (string, required), `IsFirstTimeUser` (bool, default true), `FlgStatus` (string, default `A`), `DateCreated` (DateTime), `DateUpdated` (DateTime).
2. THE ProfileService SHALL NOT scope PlatformAdmin entities by `OrganizationId` — PlatformAdmin is a platform-level entity that does not implement `IOrganizationEntity`.
3. WHEN SecurityService calls `GET /api/v1/platform-admins/by-username/{username}` with a valid service JWT, THE ProfileService SHALL return the PlatformAdmin record including `PlatformAdminId`, `PasswordHash`, `FlgStatus`, `IsFirstTimeUser`, and `Email`.
4. WHEN SecurityService calls `PATCH /api/v1/platform-admins/{id}/password` with a valid service JWT, THE ProfileService SHALL update the PlatformAdmin's password hash and return HTTP 200.
5. WHEN these internal PlatformAdmin endpoints are called without a valid service JWT, THE ProfileService SHALL return HTTP 403 with `SERVICE_NOT_AUTHORIZED`.
6. WHEN the username does not match any PlatformAdmin, THE ProfileService SHALL return HTTP 404 with `NOT_FOUND` (3021).

### Requirement 17: PlatformAdmin Organization Provisioning

**User Story:** As a PlatformAdmin, I want to create organizations and provision the first OrgAdmin so that new tenants can be onboarded onto the platform.

#### Acceptance Criteria

1. WHEN a PlatformAdmin calls `POST /api/v1/organizations` with valid data, THE ProfileService SHALL create the organization, seed 5 default departments, and return HTTP 201.
2. WHEN a PlatformAdmin calls `POST /api/v1/organizations/{id}/provision-admin` with `{email, firstName, lastName}`, THE ProfileService SHALL create a TeamMember with OrgAdmin role in the Engineering department (as primary department), generate a professional ID, call SecurityService `POST /api/v1/auth/credentials/generate` to generate initial credentials, and return HTTP 201.
3. WHEN the email provided for the first OrgAdmin is already registered in the target organization, THE ProfileService SHALL return HTTP 409 with `EMAIL_ALREADY_REGISTERED` (3001).
4. WHEN a PlatformAdmin calls `PATCH /api/v1/organizations/{id}/status`, THE ProfileService SHALL transition the organization through the `A → S → D` lifecycle.
5. WHEN a PlatformAdmin calls `GET /api/v1/organizations` (without an organization scope), THE ProfileService SHALL return a paginated list of all organizations across the platform for monitoring purposes.
6. THE ProfileService SHALL require the `PlatformAdmin` role (validated via `PlatformAdminAttribute` or `RoleAuthorizationMiddleware`) for all PlatformAdmin-specific endpoints.

### Requirement 18: PlatformAdmin Login Support

**User Story:** As a PlatformAdmin, I want a separate login flow so that platform-level authentication is distinct from organization-scoped team member authentication.

#### Acceptance Criteria

1. WHEN SecurityService needs to authenticate a PlatformAdmin, THE ProfileService SHALL expose `GET /api/v1/platform-admins/by-username/{username}` (Service auth) returning the PlatformAdmin record for credential verification.
2. WHEN a PlatformAdmin JWT is issued by SecurityService, THE ProfileService SHALL expect the JWT to contain claims: `userId` (PlatformAdminId), `roleName = "PlatformAdmin"` — with no `organizationId` and no `departmentId` claims.
3. WHEN a PlatformAdmin makes requests to ProfileService, THE ProfileService SHALL bypass `OrganizationScopeMiddleware` for PlatformAdmin-authenticated requests (since PlatformAdmin has no organization scope).
4. WHEN SecurityService calls `PATCH /api/v1/platform-admins/{id}/password` with a valid service JWT, THE ProfileService SHALL update the PlatformAdmin's password hash (for forced password change and password reset flows).
5. WHEN a PlatformAdmin's `IsFirstTimeUser` flag is `true`, THE ProfileService SHALL expose this flag to SecurityService so that `FirstTimeUserMiddleware` can enforce the forced password change flow.

### Requirement 19: PlatformAdmin Cross-Cutting SecurityService Updates

**User Story:** As a developer, I want the SecurityService to support PlatformAdmin authentication and authorization so that the PlatformAdmin role is fully integrated into the platform security model.

#### Acceptance Criteria

1. THE SecurityService SHALL add `PlatformAdmin` to the `RoleNames` static class (no PermissionLevel — PlatformAdmin is not part of the organization-scoped role hierarchy).
2. THE SecurityService SHALL update `RoleAuthorizationMiddleware` to recognize `roleName = "PlatformAdmin"` from JWT claims and grant access to endpoints decorated with `PlatformAdminAttribute`.
3. THE SecurityService SHALL add a `PlatformAdminAttribute` custom authorization attribute in the Api layer's `Attributes/` folder.
4. WHEN a PlatformAdmin calls `POST /api/v1/auth/login` with username and password, THE SecurityService SHALL resolve the user via ProfileService `GET /api/v1/platform-admins/by-username/{username}` (instead of the team member endpoint), verify credentials, and issue a JWT with `userId = PlatformAdminId`, `roleName = "PlatformAdmin"`, and no `organizationId` or `departmentId` claims.
5. WHEN a PlatformAdmin JWT is validated, THE SecurityService SHALL skip `OrganizationScopeMiddleware` enforcement (since PlatformAdmin has no organization scope).
6. THE SecurityService SHALL support PlatformAdmin password management (forced change, reset) using the same flows as TeamMember but calling ProfileService's PlatformAdmin password endpoint (`PATCH /api/v1/platform-admins/{id}/password`).

### Requirement 20: ProfileService API Endpoints (REQ-033)

**User Story:** As a developer, I want a complete set of profile endpoints so that all organization and team management flows are supported.

#### Acceptance Criteria

1. THE ProfileService SHALL expose the following endpoints:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/organizations` | OrgAdmin, PlatformAdmin | Create organization |
| GET | `/api/v1/organizations` | PlatformAdmin | List all organizations (cross-org) |
| GET | `/api/v1/organizations/{id}` | Bearer | Get organization |
| PUT | `/api/v1/organizations/{id}` | OrgAdmin | Update organization |
| PATCH | `/api/v1/organizations/{id}/status` | OrgAdmin, PlatformAdmin | Activate/deactivate |
| PUT | `/api/v1/organizations/{id}/settings` | OrgAdmin | Update settings |
| POST | `/api/v1/organizations/{id}/provision-admin` | PlatformAdmin | Provision first OrgAdmin |
| POST | `/api/v1/departments` | OrgAdmin | Create department |
| GET | `/api/v1/departments` | Bearer | List departments |
| GET | `/api/v1/departments/{id}` | Bearer | Get department |
| PUT | `/api/v1/departments/{id}` | OrgAdmin, DeptLead | Update department |
| PATCH | `/api/v1/departments/{id}/status` | OrgAdmin | Activate/deactivate |
| GET | `/api/v1/departments/{id}/members` | Bearer | List department members |
| GET | `/api/v1/departments/{id}/preferences` | Bearer | Get department preferences |
| PUT | `/api/v1/departments/{id}/preferences` | OrgAdmin, DeptLead | Update department preferences |
| GET | `/api/v1/team-members` | Bearer | List team members |
| GET | `/api/v1/team-members/{id}` | Bearer | Get team member |
| PUT | `/api/v1/team-members/{id}` | OrgAdmin, DeptLead, Self | Update profile |
| PATCH | `/api/v1/team-members/{id}/status` | OrgAdmin | Activate/deactivate |
| PATCH | `/api/v1/team-members/{id}/availability` | Bearer, Self | Update availability |
| POST | `/api/v1/team-members/{id}/departments` | OrgAdmin | Add to department |
| DELETE | `/api/v1/team-members/{id}/departments/{deptId}` | OrgAdmin | Remove from department |
| PATCH | `/api/v1/team-members/{id}/departments/{deptId}/role` | OrgAdmin | Change department role |
| GET | `/api/v1/team-members/by-email/{email}` | Service | Internal: fetch member for auth |
| PATCH | `/api/v1/team-members/{id}/password` | Service | Internal: update password hash |
| GET | `/api/v1/roles` | Bearer | List roles |
| GET | `/api/v1/roles/{id}` | Bearer | Get role |
| POST | `/api/v1/invites` | OrgAdmin, DeptLead | Create invite |
| GET | `/api/v1/invites` | OrgAdmin, DeptLead | List pending invites |
| GET | `/api/v1/invites/{token}/validate` | None | Validate invite |
| POST | `/api/v1/invites/{token}/accept` | None | Accept invite |
| DELETE | `/api/v1/invites/{id}` | OrgAdmin, DeptLead | Cancel invite |
| GET | `/api/v1/devices` | Bearer | List devices |
| PATCH | `/api/v1/devices/{id}/primary` | Bearer | Set primary device |
| DELETE | `/api/v1/devices/{id}` | Bearer | Remove device |
| GET | `/api/v1/notification-settings` | Bearer | Get preferences |
| PUT | `/api/v1/notification-settings/{typeId}` | Bearer | Update preference |
| GET | `/api/v1/notification-types` | Bearer | List notification types |
| GET | `/api/v1/preferences` | Bearer | Get user preferences |
| PUT | `/api/v1/preferences` | Bearer | Update user preferences |
| GET | `/api/v1/preferences/resolved` | Bearer | Get resolved preferences |
| GET | `/api/v1/platform-admins/by-username/{username}` | Service | Internal: fetch PlatformAdmin for auth |
| PATCH | `/api/v1/platform-admins/{id}/password` | Service | Internal: update PlatformAdmin password |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

2. THE ProfileService SHALL use URL path versioning with prefix `/api/v1/`.
3. THE ProfileService SHALL return all responses in the `ApiResponse<T>` envelope format with `CorrelationId`.
4. WHEN a request fails FluentValidation, THE ProfileService SHALL return HTTP 422 with error code `VALIDATION_ERROR` (1000) and the list of validation errors.

### Requirement 21: ProfileService Redis Key Patterns (REQ-034)

**User Story:** As a developer, I want well-defined Redis key patterns for ProfileService caching so that data access is consistent and predictable.

#### Acceptance Criteria

1. THE ProfileService SHALL use the following Redis key patterns with their specified TTLs:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `org_settings:{organizationId}` | Cached organization settings | 60 min |
| `dept_list:{organizationId}` | Cached department list | 30 min |
| `member_profile:{memberId}` | Cached team member profile | 15 min |
| `dept_prefs:{departmentId}` | Cached department preferences | 30 min |
| `user_prefs:{userId}` | Cached user preferences | 15 min |
| `resolved_prefs:{userId}` | Cached resolved preferences (all levels merged) | 5 min |
| `outbox:profile` | Outbox queue for audit events and notifications | Until processed |
| `blacklist:{jti}` | Token deny list (shared with SecurityService) | Remaining token TTL |

2. THE ProfileService SHALL use consistent key naming with colon-separated segments.
3. THE ProfileService SHALL set appropriate TTLs on all Redis keys to prevent unbounded memory growth.

### Requirement 22: ProfileService Seed Data (REQ-035)

**User Story:** As a developer, I want predefined seed data so that the system is ready to use after deployment.

#### Acceptance Criteria

1. WHEN the ProfileService database is initialized, THE ProfileService SHALL seed the following data:
   - 4 system roles: OrgAdmin (PermissionLevel 100), DeptLead (75), Member (50), Viewer (25) — all marked `IsSystemRole=true`.
   - 8 notification types: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching.
2. WHEN an organization is created, THE ProfileService SHALL seed 5 default departments for that organization: Engineering (ENG), QA (QA), DevOps (DEVOPS), Product (PROD), Design (DESIGN) — all marked `IsDefault=true`.
3. THE ProfileService SHALL use idempotent seeding — running the seed logic multiple times produces the same result without duplicating data.

### Requirement 23: ProfileService Error Codes (3001–3027)

**User Story:** As a developer, I want well-defined error codes so that all ProfileService error responses are consistent and actionable.

#### Acceptance Criteria

1. THE ProfileService SHALL use the following error codes:

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| VALIDATION_ERROR | 1000 | 422 | FluentValidation failure |
| EMAIL_ALREADY_REGISTERED | 3001 | 409 | Duplicate email within organization |
| INVITE_EXPIRED_OR_INVALID | 3002 | 410 | Bad or expired invite token |
| MAX_DEVICES_REACHED | 3003 | 400 | 5 device limit per user |
| LAST_ORGADMIN_CANNOT_DEACTIVATE | 3004 | 400 | Must keep at least one OrgAdmin |
| ORGANIZATION_NAME_DUPLICATE | 3005 | 409 | Duplicate organization name |
| STORY_PREFIX_DUPLICATE | 3006 | 409 | Duplicate story ID prefix |
| STORY_PREFIX_IMMUTABLE | 3007 | 400 | Cannot change prefix after stories exist |
| DEPARTMENT_NAME_DUPLICATE | 3008 | 409 | Duplicate department name in organization |
| DEPARTMENT_CODE_DUPLICATE | 3009 | 409 | Duplicate department code in organization |
| DEFAULT_DEPARTMENT_CANNOT_DELETE | 3010 | 400 | Cannot delete predefined department |
| MEMBER_ALREADY_IN_DEPARTMENT | 3011 | 409 | Member already assigned to department |
| MEMBER_MUST_HAVE_DEPARTMENT | 3012 | 400 | Cannot remove last department from member |
| INVALID_ROLE_ASSIGNMENT | 3013 | 400 | Role not valid for context |
| INVITE_EMAIL_ALREADY_MEMBER | 3014 | 409 | Invitee is already a member |
| ORGANIZATION_MISMATCH | 3015 | 403 | Cross-organization access attempt |
| RATE_LIMIT_EXCEEDED | 3016 | 429 | Rate limit exceeded |
| DEPARTMENT_HAS_ACTIVE_MEMBERS | 3017 | 400 | Cannot deactivate department with active members |
| MEMBER_NOT_IN_DEPARTMENT | 3018 | 400 | Member not assigned to target department |
| INVALID_AVAILABILITY_STATUS | 3019 | 400 | Unknown availability value |
| STORY_PREFIX_INVALID_FORMAT | 3020 | 400 | Prefix must be 2–10 uppercase alphanumeric |
| NOT_FOUND | 3021 | 404 | Entity not found |
| CONFLICT | 3022 | 409 | Duplicate or state conflict |
| SERVICE_UNAVAILABLE | 3023 | 503 | Downstream timeout or circuit open |
| DEPARTMENT_NOT_FOUND | 3024 | 404 | Department does not exist |
| MEMBER_NOT_FOUND | 3025 | 404 | Team member does not exist |
| INVALID_PREFERENCE_VALUE | 3026 | 400 | Preference value is invalid for the field type |
| PREFERENCE_KEY_UNKNOWN | 3027 | 400 | Unknown preference key |

2. THE ProfileService SHALL define all error codes as constants in the `ErrorCodes` static class within the Domain layer.

### Requirement 24: ProfileService Data Models

**User Story:** As a developer, I want well-defined data models so that the ProfileService database schema is clear and supports all profile management operations.

#### Acceptance Criteria

1. THE ProfileService SHALL maintain an `organization` table with columns: `OrganizationId` (Guid, PK, auto), `OrganizationName` (string, required, unique), `StoryIdPrefix` (string, required, max 10, unique), `Description` (string?), `Website` (string?), `LogoUrl` (string?), `TimeZone` (string, default `"UTC"`), `DefaultSprintDurationWeeks` (int, default 2), `SettingsJson` (string?, JSON column), `FlgStatus` (string, default `"A"`), `DateCreated` (DateTime), `DateUpdated` (DateTime).
2. THE ProfileService SHALL maintain a `department` table with columns: `DepartmentId` (Guid, PK, auto), `OrganizationId` (Guid, required, FK), `DepartmentName` (string, required), `DepartmentCode` (string, required, max 20), `Description` (string?), `IsDefault` (bool), `PreferencesJson` (string?, JSON column), `FlgStatus` (string, default `"A"`), `DateCreated` (DateTime), `DateUpdated` (DateTime). Unique index on `(OrganizationId, DepartmentName)` and `(OrganizationId, DepartmentCode)`.
3. THE ProfileService SHALL maintain a `team_member` table with columns: `TeamMemberId` (Guid, PK, auto), `OrganizationId` (Guid, required, FK), `PrimaryDepartmentId` (Guid, required, FK), `Email` (string, required), `Password` (string, required), `FirstName` (string, required), `LastName` (string, required), `DisplayName` (string?), `AvatarUrl` (string?), `Title` (string?), `ProfessionalId` (string, required), `Skills` (string?, JSON array), `Availability` (string, default `"Available"`), `MaxConcurrentTasks` (int, default 5), `IsFirstTimeUser` (bool, default true), `FlgStatus` (string, default `"A"`), `DateCreated` (DateTime), `DateUpdated` (DateTime). Unique index on `(OrganizationId, Email)`.
4. THE ProfileService SHALL maintain a `department_member` table with columns: `DepartmentMemberId` (Guid, PK), `TeamMemberId` (Guid, FK), `DepartmentId` (Guid, FK), `OrganizationId` (Guid, FK), `RoleId` (Guid, FK), `DateJoined` (DateTime). Unique index on `(OrganizationId, TeamMemberId, DepartmentId)`.
5. THE ProfileService SHALL maintain a `role` table with columns: `RoleId` (Guid, PK, auto), `RoleName` (string, required, unique), `Description` (string?), `PermissionLevel` (int), `IsSystemRole` (bool), `DateCreated` (DateTime).
6. THE ProfileService SHALL maintain an `invite` table with columns: `InviteId` (Guid, PK, auto), `OrganizationId` (Guid, required, FK), `DepartmentId` (Guid, required, FK), `RoleId` (Guid, required, FK), `InvitedByMemberId` (Guid, required, FK), `FirstName` (string, required), `LastName` (string, required), `Email` (string, required), `Token` (string, required, max 128), `ExpiryDate` (DateTime, required), `FlgStatus` (string, default `"A"`), `DateCreated` (DateTime).
7. THE ProfileService SHALL maintain a `device` table with columns: `DeviceId` (Guid, PK, auto), `OrganizationId` (Guid, required), `TeamMemberId` (Guid, required, FK), `DeviceName` (string?), `DeviceType` (string, required), `IpAddress` (string?), `UserAgent` (string?), `IsPrimary` (bool), `FlgStatus` (string, default `"A"`), `DateCreated` (DateTime), `LastActiveDate` (DateTime).
8. THE ProfileService SHALL maintain a `notification_setting` table with columns: `NotificationSettingId` (Guid, PK, auto), `NotificationTypeId` (Guid, required, FK), `OrganizationId` (Guid, required), `TeamMemberId` (Guid, required, FK), `IsEmail` (bool), `IsPush` (bool), `IsInApp` (bool).
9. THE ProfileService SHALL maintain a `notification_type` table with columns: `NotificationTypeId` (Guid, PK, auto), `TypeName` (string, required, unique), `Description` (string?), `DateCreated` (DateTime).
10. THE ProfileService SHALL maintain a `user_preferences` table with columns: `UserPreferencesId` (Guid, PK, auto), `OrganizationId` (Guid, required, FK), `TeamMemberId` (Guid, required, FK, unique), `Theme` (string, default `"System"`), `Language` (string, default `"en"`), `TimezoneOverride` (string?), `DefaultBoardView` (string?), `DefaultBoardFilters` (string?, JSON), `DashboardLayout` (string?, JSON), `EmailDigestFrequency` (string?), `KeyboardShortcutsEnabled` (bool, default true), `DateFormat` (string, default `"ISO"`), `TimeFormat` (string, default `"H24"`), `DateCreated` (DateTime), `DateUpdated` (DateTime).
11. THE ProfileService SHALL maintain a `platform_admin` table with columns: `PlatformAdminId` (Guid, PK), `Username` (string, required, unique), `PasswordHash` (string, required), `Email` (string, required, unique), `FirstName` (string, required), `LastName` (string, required), `IsFirstTimeUser` (bool, default true), `FlgStatus` (string, default `"A"`), `DateCreated` (DateTime), `DateUpdated` (DateTime).
12. THE ProfileService SHALL use EF Core with PostgreSQL (Npgsql) and apply auto-migrations via `DatabaseMigrationHelper` on startup.
13. THE ProfileService SHALL apply EF Core global query filters by `OrganizationId` on all entities implementing `IOrganizationEntity`.

### Requirement 25: Inter-Service Resilience (REQ-091)

**User Story:** As a developer, I want typed service clients with Polly resilience policies so that inter-service communication from ProfileService is reliable and fault-tolerant.

#### Acceptance Criteria

1. WHEN ProfileService communicates with SecurityService, THE ProfileService SHALL use a typed service client interface (`ISecurityServiceClient`).
2. WHEN the typed client makes an HTTP call, THE ProfileService SHALL apply Polly resilience policies: 3 retries with exponential backoff (1s, 2s, 4s), circuit breaker (5 failures → 30s open), and 10s timeout per request.
3. WHEN a downstream service returns 4xx or 5xx, THE ProfileService SHALL attempt to deserialize the response as `ApiResponse<object>` and throw a `DomainException` with the downstream error code. IF deserialization fails, THEN THE ProfileService SHALL throw a `DomainException` with `SERVICE_UNAVAILABLE` (3023).
4. WHEN the circuit breaker opens, THE ProfileService SHALL throw a `DomainException` with `SERVICE_UNAVAILABLE` (3023).
5. WHEN an inter-service call is made, THE ProfileService SHALL propagate the `X-Correlation-Id` header via `CorrelationIdDelegatingHandler`.
6. WHEN a downstream call fails, THE ProfileService SHALL log at Warning level with structured properties: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

### Requirement 26: Standardized Error Handling (REQ-088)

**User Story:** As a developer, I want all errors handled consistently so that clients receive predictable error responses from ProfileService.

#### Acceptance Criteria

1. WHEN a `DomainException` is thrown, THE ProfileService SHALL catch it via `GlobalExceptionHandlerMiddleware` and return an `ApiResponse<object>` with `application/problem+json` content type, including the error's `ErrorCode`, `ErrorValue`, `Message`, and `CorrelationId`.
2. WHEN an unhandled exception is thrown, THE ProfileService SHALL return HTTP 500 with `ErrorCode = "INTERNAL_ERROR"`, `Message = "An unexpected error occurred."`, and `CorrelationId`. THE ProfileService SHALL not leak stack traces or internal details.
3. WHEN a `RateLimitExceededException` is thrown, THE ProfileService SHALL add a `Retry-After` header to the error response.
4. WHEN any error response is returned, THE ProfileService SHALL include the `CorrelationId` from `HttpContext.Items["CorrelationId"]`.

### Requirement 27: FluentValidation Pipeline (REQ-090)

**User Story:** As a developer, I want automatic request validation so that invalid data is rejected before reaching ProfileService business logic.

#### Acceptance Criteria

1. WHEN a request DTO has a corresponding FluentValidation validator, THE ProfileService SHALL auto-discover and execute the validator before the controller action.
2. WHEN validation fails, THE ProfileService SHALL return HTTP 422 with `ErrorCode = "VALIDATION_ERROR"`, `ErrorValue = 1000`, and per-field errors in the `Errors` array as `{ field, message }` objects.
3. WHEN ASP.NET Core's built-in `ModelStateInvalidFilter` is configured, THE ProfileService SHALL disable it via `SuppressModelStateInvalidFilter = true` to let FluentValidation handle all validation.

### Requirement 28: Health Checks (REQ-095)

**User Story:** As a DevOps engineer, I want health check endpoints so that I can monitor ProfileService availability and readiness.

#### Acceptance Criteria

1. WHEN `GET /health` is called, THE ProfileService SHALL return HTTP 200 if the process is running (liveness probe).
2. WHEN `GET /ready` is called, THE ProfileService SHALL check PostgreSQL connectivity and Redis connectivity.
3. WHEN both PostgreSQL and Redis are healthy, THE ProfileService SHALL return HTTP 200 for the readiness probe.
4. IF either PostgreSQL or Redis is unhealthy, THEN THE ProfileService SHALL return a non-200 status for the readiness probe.

### Requirement 29: Configuration via Environment Variables (REQ-101)

**User Story:** As a DevOps engineer, I want all ProfileService configuration via environment variables so that the service is 12-factor compliant.

#### Acceptance Criteria

1. WHEN ProfileService starts, THE ProfileService SHALL load configuration from a `.env` file via `DotNetEnv` and populate an `AppSettings` singleton.
2. WHEN a required environment variable is missing, THE ProfileService SHALL throw `InvalidOperationException` at startup with a clear message identifying the missing variable.
3. WHEN optional environment variables are missing, THE ProfileService SHALL use sensible defaults.

### Requirement 30: CORS Configuration (REQ-102)

**User Story:** As a developer, I want CORS configured so that the frontend can communicate with ProfileService.

#### Acceptance Criteria

1. WHEN ProfileService starts, THE ProfileService SHALL configure CORS with allowed origins from the `ALLOWED_ORIGINS` environment variable (comma-separated list).
2. WHEN a preflight request is received, THE ProfileService SHALL respond with appropriate CORS headers.

### Requirement 31: Swagger Documentation (REQ-103)

**User Story:** As a developer, I want Swagger UI so that I can explore and test ProfileService API endpoints.

#### Acceptance Criteria

1. WHILE ProfileService is running in Development mode, THE ProfileService SHALL serve Swagger UI at `/swagger`.
2. WHEN Swagger is configured, THE ProfileService SHALL include JWT Bearer authentication support for testing authenticated endpoints.

### Requirement 32: Structured Logging (REQ-098)

**User Story:** As a developer, I want structured logging so that ProfileService logs are searchable and correlatable across requests.

#### Acceptance Criteria

1. WHEN a `DomainException` is logged, THE ProfileService SHALL include structured properties: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`.
2. WHEN an unhandled exception is logged, THE ProfileService SHALL include structured properties: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`.
3. WHEN a downstream call fails, THE ProfileService SHALL include structured properties: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

### Requirement 33: Pagination (REQ-096)

**User Story:** As a developer, I want consistent pagination on ProfileService list endpoints so that large datasets are handled efficiently.

#### Acceptance Criteria

1. WHEN any list endpoint is called (team members, departments, invites, devices, organizations), THE ProfileService SHALL support `page` (default 1) and `pageSize` (default 20, max 100) query parameters.
2. WHEN the response is paginated, THE ProfileService SHALL include `TotalCount`, `Page`, `PageSize`, `TotalPages`, and the `Data` array.
3. WHEN `pageSize` exceeds 100, THE ProfileService SHALL cap the value at 100.

### Requirement 34: Soft Delete Pattern (REQ-097)

**User Story:** As the platform, I want soft deletes so that ProfileService data is never permanently lost and can be recovered if needed.

#### Acceptance Criteria

1. WHEN an entity is "deleted" in ProfileService, THE ProfileService SHALL set the entity's `FlgStatus` to `D` (Deactivated) instead of physically removing the record.
2. WHEN entities are queried, THE ProfileService SHALL apply EF Core global query filters to exclude entities with `FlgStatus = 'D'` by default.
3. WHEN an admin query requires access to deleted entities, THE ProfileService SHALL support bypassing the query filter via `.IgnoreQueryFilters()`.
4. THE ProfileService SHALL never perform physical deletion of records.

### Requirement 35: Clean Architecture Layer Enforcement (REQ-086)

**User Story:** As a developer, I want strict layer boundaries enforced across ProfileService projects so that architectural integrity is maintained and dependencies flow inward only.

#### Acceptance Criteria

1. THE ProfileService SHALL be structured as four .NET projects: `ProfileService.Domain`, `ProfileService.Application`, `ProfileService.Infrastructure`, and `ProfileService.Api`.
2. THE ProfileService.Domain project SHALL have zero `ProjectReference` entries and zero ASP.NET Core or EF Core package references.
3. THE ProfileService.Application project SHALL reference only `ProfileService.Domain` and contain no infrastructure packages (only FluentValidation is allowed as an external package).
4. THE ProfileService.Infrastructure project SHALL reference `ProfileService.Domain` and `ProfileService.Application`.
5. THE ProfileService.Api project SHALL reference `ProfileService.Application` and `ProfileService.Infrastructure` and serve as the composition root for dependency injection registration.
6. THE ProfileService SHALL target `net8.0` across all four projects.

### Requirement 36: Organization Isolation via Global Query Filters (REQ-087)

**User Story:** As the platform, I want all ProfileService database queries to be automatically scoped to the current organization so that data isolation is enforced at the database level.

#### Acceptance Criteria

1. WHEN EF Core queries are executed on organization-scoped entities, THE ProfileService SHALL apply global query filters that automatically scope all queries by `OrganizationId`.
2. WHEN an entity implements `IOrganizationEntity`, THE ProfileService SHALL apply the global query filter for that entity.
3. WHEN `OrganizationScopeMiddleware` processes a request, THE ProfileService SHALL extract `organizationId` from JWT claims and store it in `HttpContext.Items["OrganizationId"]`.
4. WHEN inter-service calls are made, THE ProfileService SHALL propagate the `X-Organization-Id` header via `CorrelationIdDelegatingHandler`.
5. WHEN a PlatformAdmin makes a request, THE ProfileService SHALL bypass the organization scope filter (PlatformAdmin operates across all organizations).

### Requirement 37: CorrelationId Propagation (REQ-092)

**User Story:** As a developer, I want end-to-end request tracing so that I can debug issues across services involving ProfileService.

#### Acceptance Criteria

1. WHEN a request enters ProfileService, THE ProfileService SHALL extract `X-Correlation-Id` from the request header via `CorrelationIdMiddleware` or generate a new GUID if not present.
2. WHEN the correlation ID is established, THE ProfileService SHALL store it in `HttpContext.Items["CorrelationId"]` and add it to the response header.
3. WHEN an inter-service call is made to SecurityService, THE ProfileService SHALL attach the `X-Correlation-Id` header to the outgoing request via `CorrelationIdDelegatingHandler`.
4. WHEN any error response is returned, THE ProfileService SHALL include the `CorrelationId` in the `ApiResponse` body.

### Requirement 38: Redis Outbox Message Format (REQ-093)

**User Story:** As a developer, I want a standardized outbox message format so that audit events and notifications published by ProfileService are consistently structured and reliably delivered.

#### Acceptance Criteria

1. WHEN ProfileService publishes an event to `outbox:profile`, THE ProfileService SHALL use a JSON message containing: `MessageId` (Guid), `MessageType` (string: "AuditEvent" or "NotificationRequest"), `ServiceName` ("ProfileService"), `OrganizationId` (Guid, nullable for platform-level events), `UserId` (Guid, nullable), `Action` (string, e.g., "OrganizationCreated", "MemberInvited", "MemberDeactivated", "InviteAccepted", "SettingsUpdated"), `EntityType` (string), `EntityId` (string), `OldValue` (string, nullable), `NewValue` (string, nullable), `IpAddress` (string, nullable), `CorrelationId` (string), `Timestamp` (DateTime UTC), and `RetryCount` (int, default 0).
2. WHEN publishing an outbox message fails, THE ProfileService SHALL retry the publish operation up to 3 times with exponential backoff.
3. IF the outbox message fails to publish after 3 retry attempts, THEN THE ProfileService SHALL move the message to a dead-letter queue at `dlq:profile`.
4. THE ProfileService SHALL set `RetryCount` to 0 on initial publish and increment it on each retry attempt.

### Requirement 39: Error Code Resolver Service

**User Story:** As a developer, I want error codes resolved to standardized response codes and descriptions so that all error responses from ProfileService are consistent and centrally managed.

#### Acceptance Criteria

1. WHEN `GlobalExceptionHandlerMiddleware` catches a `DomainException`, THE ProfileService SHALL use `IErrorCodeResolverService` to resolve the error code to its `ResponseCode` and `ResponseDescription`.
2. WHEN `IErrorCodeResolverService` resolves an error code, THE ProfileService SHALL check Redis cache at `error_code:{code}` first (24-hour TTL), then call UtilityService's error code registry endpoint (`GET /api/v1/error-codes/{code}`) via a typed service client with Polly resilience policies.
3. IF `IErrorCodeResolverService` cannot reach UtilityService, THEN THE ProfileService SHALL fall back to a local static mapping.
4. THE ProfileService SHALL map error codes to response codes following the same static mapping pattern used by SecurityService (e.g., permission errors → `03`, not found → `07`, duplicate/conflict → `06`, validation → `96`, rate limit → `08`, internal → `98`).

### Requirement 40: ProfileService Middleware Pipeline

**User Story:** As the platform, I want a well-defined middleware pipeline for ProfileService so that security and cross-cutting concerns are enforced in the correct order.

#### Acceptance Criteria

1. WHEN a request enters ProfileService, THE ProfileService SHALL execute middleware in this exact order: CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → FirstTimeUserGuard → RoleAuthorization → OrganizationScope → Controllers.
2. WHEN `GlobalExceptionHandlerMiddleware` catches a `DomainException`, THE ProfileService SHALL return the appropriate HTTP status code with `application/problem+json` content type and the `ApiResponse<T>` envelope.
3. WHEN `GlobalExceptionHandlerMiddleware` catches an unhandled exception, THE ProfileService SHALL return HTTP 500 with a generic error message and publish an error event to `outbox:profile`.
4. THE ProfileService SHALL generate or propagate a `CorrelationId` on every request via `CorrelationIdMiddleware`.
5. WHEN a PlatformAdmin-authenticated request is processed, THE ProfileService SHALL allow `OrganizationScopeMiddleware` to be bypassed (PlatformAdmin has no organization scope).

### Requirement 41: Service-to-Service JWT Token Management (REQ-099)

**User Story:** As a developer, I want automatic service JWT management in ProfileService's typed clients so that inter-service auth is seamless.

#### Acceptance Criteria

1. WHEN ProfileService's typed service client (`ISecurityServiceClient`) makes a call, THE ProfileService SHALL automatically attach a service JWT via `Authorization: Bearer {token}`.
2. WHEN the cached service token is within 30 seconds of expiry, THE ProfileService SHALL automatically refresh it by calling SecurityService `POST /api/v1/service-tokens/issue`.
3. WHEN the `X-Organization-Id` header is available in the current request context, THE ProfileService SHALL propagate it to the downstream call.

### Requirement 42: API Versioning (REQ-100)

**User Story:** As a developer, I want API versioning so that breaking changes can be introduced without affecting existing clients.

#### Acceptance Criteria

1. WHEN any ProfileService endpoint is defined, THE ProfileService SHALL use URL path versioning: `/api/v1/...`.
2. WHEN a new version is needed, THE ProfileService SHALL add it as `/api/v2/...` without removing the v1 endpoints.

### Requirement 43: Database Migrations (REQ-094)

**User Story:** As a developer, I want database migrations to auto-apply on startup so that ProfileService deployment is simplified.

#### Acceptance Criteria

1. WHEN ProfileService starts, THE ProfileService SHALL call `DatabaseMigrationHelper.ApplyMigrations(app)` to check for pending EF Core migrations and apply them.
2. WHEN the database is InMemory (test environment), THE ProfileService SHALL call `EnsureCreated()` instead of `Migrate()`.
3. WHEN no pending migrations exist, THE ProfileService SHALL proceed with startup without database changes.

### Requirement 44: Inter-Service Communication Map (REQ-105)

**User Story:** As a developer, I want a clear map of ProfileService's inter-service dependencies so that I understand the communication topology.

#### Acceptance Criteria

1. THE ProfileService SHALL follow this inter-service communication map:

| Direction | Caller | Callee | Purpose |
|-----------|--------|--------|---------|
| Outbound | ProfileService | SecurityService | Credential generation for invited members (`POST /api/v1/auth/credentials/generate`) |
| Inbound | SecurityService | ProfileService | User identity resolution (`GET /api/v1/team-members/by-email/{email}`), password update (`PATCH /api/v1/team-members/{id}/password`), PlatformAdmin resolution (`GET /api/v1/platform-admins/by-username/{username}`), PlatformAdmin password update (`PATCH /api/v1/platform-admins/{id}/password`) |
| Inbound | WorkService | ProfileService | Organization settings, team member lookup, department member lists |
| Async | ProfileService | UtilityService | Via outbox (`outbox:profile`) for audit events and notifications |

2. THE ProfileService SHALL use typed service client interfaces for all synchronous inter-service calls.
3. THE ProfileService SHALL use the Redis outbox pattern for all asynchronous communication with UtilityService.
