# Nexus-2.0 — Enterprise Agile Platform — Backend Requirements

> **Version:** 1.0  
> **Architecture:** Clean Architecture (.NET 8) — 4 Microservices  
> **Format:** User Stories with WHEN/THEN Acceptance Criteria  
> **Requirement Numbering:** Sequential across all services (1–N)

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Glossary](#2-glossary)
3. [Error Code Registry](#3-error-code-registry)
4. [SecurityService Requirements (REQ-001 – REQ-020)](#4-securityservice-requirements)
5. [ProfileService Requirements (REQ-021 – REQ-035)](#5-profileservice-requirements)
6. [WorkService Requirements (REQ-036 – REQ-070)](#6-workservice-requirements)
7. [UtilityService Requirements (REQ-071 – REQ-085)](#7-utilityservice-requirements)
8. [Cross-Cutting Requirements (REQ-086 – REQ-105)](#8-cross-cutting-requirements)
9. [Preference & Settings Requirements (REQ-106 – REQ-108)](#9-preference--settings-requirements)

---

## 1. Introduction

### 1.1 Purpose

This document defines the complete backend requirements for the Nexus-2.0 Enterprise Agile Platform. It is a standalone, actionable reference — a development team should be able to build the entire backend from this document combined with the platform specification (`nexus-2.0-backend-specification.md`).

### 1.2 Platform Summary

Nexus-2.0 is a microservices platform implementing story-driven Agile development workflow with:
- Professional story ID systems (e.g., `NEXUS-1234`)
- Department-based task assignment and RBAC
- Complete story-task traceability across departments
- Sprint planning, kanban boards, and velocity tracking

### 1.3 Service Decomposition

| Service | Port | Database | Responsibility |
|---------|------|----------|----------------|
| SecurityService.API | 5001 | `nexus_security` | Authentication, JWT, sessions, department-aware RBAC, OTP, rate limiting, anomaly detection, service-to-service auth |
| ProfileService.API | 5002 | `nexus_profile` | Organizations, departments, team members, roles, invites, devices, notification settings |
| WorkService.API | 5003 | `nexus_work` | Stories, tasks, sprints, boards, comments, labels, search, activity feeds |
| UtilityService.API | 5200 | `nexus_utility` | Audit logs, error logs, notifications, error code registry, reference data, retention archival |

### 1.4 Architecture Patterns (Proven from WEP)

All services follow these established patterns:
- **Clean Architecture:** Domain / Application / Infrastructure / Api layers per service
- **Organization isolation:** EF Core global query filters by `OrganizationId`, `OrganizationScopeMiddleware`, `IOrganizationEntity` marker interface
- **Typed service clients:** Interface + implementation per downstream dependency with Polly resilience (3 retries exponential, circuit breaker 5/30s, 10s timeout)
- **CorrelationIdDelegatingHandler:** Propagates `X-Correlation-Id` on all inter-service calls
- **Standardized error handling:** `DomainException` hierarchy, `GlobalExceptionHandlerMiddleware`, `application/problem+json` content type
- **ApiResponse\<T\> envelope:** With `CorrelationId`, `ErrorCode`, `ErrorValue`, `Errors` fields
- **FluentValidation pipeline:** Auto-validation, HTTP 422 with `VALIDATION_ERROR` (1000)
- **Redis outbox pattern:** Async events for audit logging and notification dispatch
- **EF Core + PostgreSQL:** Per-service database, auto-migrations via `DatabaseMigrationHelper`
- **JWT Bearer auth:** With service-to-service JWT for inter-service calls
- **API versioning:** `/api/v1/...` URL path versioning
- **Soft delete:** `FlgStatus` field (A = Active, S = Suspended, D = Deactivated)

### 1.5 Acceptance Criteria Format

Each requirement follows this structure:

```
### REQ-NNN: Requirement Title

**User Story:** As a [role], I want [capability] so that [benefit].

**Acceptance Criteria:**

- WHEN [trigger/action] THEN [expected outcome]
- WHEN [trigger/action] THEN [expected outcome]
```

---

## 2. Glossary

| Term | Definition |
|------|-----------|
| **Organization** | Top-level tenant entity. Equivalent to "Tenant" in WEP. All data is scoped to an organization. |
| **Department** | Functional unit within an organization (e.g., Engineering, QA, DevOps, Product, Design). Five predefined departments are seeded on org creation; custom departments can be added. |
| **Team Member** | A user within an organization, assigned to one or more departments with department-scoped roles. Equivalent to "SmeUser" in WEP. |
| **Role** | Department-scoped permission level: OrgAdmin (100), DeptLead (75), Member (50), Viewer (25). A team member can hold different roles in different departments. |
| **Story** | A work item representing a feature, bug, improvement, or technical debt. Has a professional ID (e.g., `NEXUS-42`) and follows a defined workflow state machine. |
| **Story Key** | The human-readable professional ID for a story: `{OrgPrefix}-{SequenceNumber}` (e.g., `NEXUS-42`). |
| **Task** | An actionable work item within a story. Assigned to a specific department based on task type. Follows its own workflow state machine. |
| **Sprint** | A time-boxed iteration (1–4 weeks) containing stories. Only one sprint can be active per organization at a time. |
| **Board** | A visual representation of work items grouped by status (Kanban), sprint (Sprint Board), or department (Department Board). |
| **Label** | A tag applied to stories for categorization and filtering. Organization-scoped, max 10 per story. |
| **Comment** | A threaded discussion entry on a story or task, supporting @mentions. |
| **Activity Log** | An immutable record of changes to stories and tasks (status transitions, assignments, edits). |
| **Story Points** | Fibonacci-scale estimation (1, 2, 3, 5, 8, 13, 21) representing relative effort. |
| **Velocity** | Sum of completed story points in a sprint. |
| **Burndown** | Daily chart showing remaining story points vs. ideal linear decrease over a sprint. |
| **Professional ID** | The `{OrgPrefix}-{SequenceNumber}` format used for stories. Prefix is 2–10 uppercase alphanumeric characters, configured per organization. |
| **FlgStatus** | Soft-delete lifecycle field: `A` (Active), `S` (Suspended), `D` (Deactivated/Deleted). |
| **Outbox** | Redis-based async messaging pattern. Each service publishes events to `outbox:{service}`. UtilityService polls and processes all queues. |
| **CorrelationId** | End-to-end trace identifier (`X-Correlation-Id` header) propagated across all service calls and included in all API responses. |
| **DomainException** | Base exception class for all business rule violations. Contains `ErrorValue`, `ErrorCode`, `StatusCode`, and `CorrelationId`. |
| **ApiResponse\<T\>** | Standardized JSON envelope for all API responses. Contains `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, `Errors`. |
| **OrganizationSettings** | Typed class representing organization-level preferences, stored as JSON column (`SettingsJson`) on the Organization entity. Includes general, workflow, board, notification, and data settings. |
| **DepartmentPreferences** | Typed class representing department-level preferences, stored as JSON column (`PreferencesJson`) on the Department entity. Overrides organization defaults for department-specific workflow configuration. |
| **UserPreferences** | Entity storing per-user preferences with typed fields. Overrides organization and department defaults for personal workflow configuration. |

---

## 3. Error Code Registry

### 3.1 Shared Error Code

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| VALIDATION_ERROR | 1000 | 422 | FluentValidation pipeline failure |

### 3.2 SecurityService Error Codes (2001–2030)

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| INVALID_CREDENTIALS | 2001 | 401 | Wrong email/password |
| ACCOUNT_LOCKED | 2002 | 423 | Too many failed login attempts |
| ACCOUNT_INACTIVE | 2003 | 403 | Account suspended or deactivated |
| PASSWORD_REUSE_NOT_ALLOWED | 2004 | 400 | Same as temporary password |
| PASSWORD_RECENTLY_USED | 2005 | 400 | Matches one of last 5 passwords |
| FIRST_TIME_USER_RESTRICTED | 2006 | 403 | Must change password before accessing other endpoints |
| OTP_EXPIRED | 2007 | 400 | OTP code past TTL |
| OTP_VERIFICATION_FAILED | 2008 | 400 | Wrong OTP code |
| OTP_MAX_ATTEMPTS | 2009 | 429 | 3 failed OTP verification attempts |
| RATE_LIMIT_EXCEEDED | 2010 | 429 | Sliding window rate limit exceeded |
| INSUFFICIENT_PERMISSIONS | 2011 | 403 | Role lacks access to endpoint |
| TOKEN_REVOKED | 2012 | 401 | JWT is blacklisted |
| REFRESH_TOKEN_REUSE | 2013 | 401 | Refresh token rotation reuse detected |
| SERVICE_NOT_AUTHORIZED | 2016 | 403 | Service-to-service ACL denied |
| SUSPICIOUS_LOGIN | 2017 | 403 | Geo-location anomaly detected |
| PASSWORD_COMPLEXITY_FAILED | 2018 | 400 | Password does not meet complexity rules |
| ORGANIZATION_MISMATCH | 2019 | 403 | Cross-organization access attempt |
| DEPARTMENT_ACCESS_DENIED | 2020 | 403 | User not in target department |
| NOT_FOUND | 2021 | 404 | Entity not found |
| CONFLICT | 2022 | 409 | Duplicate or state conflict |
| SERVICE_UNAVAILABLE | 2023 | 503 | Downstream timeout or circuit open |
| SESSION_EXPIRED | 2024 | 401 | Session no longer valid |
| INVALID_DEPARTMENT_ROLE | 2025 | 403 | Role not valid for department operation |

### 3.3 ProfileService Error Codes (3001–3030)

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
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

### 3.4 WorkService Error Codes (4001–4040)

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| STORY_NOT_FOUND | 4001 | 404 | Story does not exist |
| TASK_NOT_FOUND | 4002 | 404 | Task does not exist |
| SPRINT_NOT_FOUND | 4003 | 404 | Sprint does not exist |
| INVALID_STORY_TRANSITION | 4004 | 400 | Invalid story workflow state transition |
| INVALID_TASK_TRANSITION | 4005 | 400 | Invalid task workflow state transition |
| SPRINT_NOT_IN_PLANNING | 4006 | 400 | Stories can only be added to sprints in Planning status |
| STORY_ALREADY_IN_SPRINT | 4007 | 409 | Story already assigned to this sprint |
| STORY_NOT_IN_SPRINT | 4008 | 400 | Story not in this sprint |
| SPRINT_OVERLAP | 4009 | 400 | Sprint dates overlap with existing sprint |
| LABEL_NOT_FOUND | 4010 | 404 | Label does not exist |
| LABEL_NAME_DUPLICATE | 4011 | 409 | Duplicate label name in organization |
| COMMENT_NOT_FOUND | 4012 | 404 | Comment does not exist |
| STORY_REQUIRES_ASSIGNEE | 4013 | 400 | Story must have assignee for InProgress transition |
| STORY_REQUIRES_TASKS | 4014 | 400 | Story must have tasks for InReview transition |
| STORY_REQUIRES_POINTS | 4015 | 400 | Story must have story points for Ready transition |
| ONLY_ONE_ACTIVE_SPRINT | 4016 | 400 | Only one sprint can be active per organization |
| COMMENT_NOT_AUTHOR | 4017 | 403 | Only author can edit/delete comment |
| ASSIGNEE_NOT_IN_DEPARTMENT | 4018 | 400 | Assignee not a member of target department |
| ASSIGNEE_AT_CAPACITY | 4019 | 400 | Assignee has reached max concurrent tasks |
| STORY_KEY_NOT_FOUND | 4020 | 404 | Story key does not resolve |
| SPRINT_ALREADY_ACTIVE | 4021 | 400 | Sprint is already active |
| SPRINT_ALREADY_COMPLETED | 4022 | 400 | Sprint is already completed |
| INVALID_STORY_POINTS | 4023 | 400 | Story points must be Fibonacci (1,2,3,5,8,13,21) |
| INVALID_PRIORITY | 4024 | 400 | Unknown priority value |
| INVALID_TASK_TYPE | 4025 | 400 | Unknown task type |
| STORY_IN_ACTIVE_SPRINT | 4026 | 400 | Cannot delete story in active sprint |
| TASK_IN_PROGRESS | 4027 | 400 | Cannot delete task that is in progress |
| SEARCH_QUERY_TOO_SHORT | 4028 | 400 | Search query must be at least 2 characters |
| MENTION_USER_NOT_FOUND | 4029 | 400 | @mentioned user not found in organization |
| ORGANIZATION_MISMATCH | 4030 | 403 | Cross-organization access |
| DEPARTMENT_ACCESS_DENIED | 4031 | 403 | User not in target department |
| INSUFFICIENT_PERMISSIONS | 4032 | 403 | Role lacks access for this operation |
| SPRINT_END_BEFORE_START | 4033 | 400 | End date must be after start date |
| STORY_SEQUENCE_INIT_FAILED | 4034 | 500 | Failed to initialize story sequence |
| HOURS_MUST_BE_POSITIVE | 4035 | 400 | Logged hours must be > 0 |
| NOT_FOUND | 4036 | 404 | Generic entity not found |
| CONFLICT | 4037 | 409 | Duplicate or state conflict |
| SERVICE_UNAVAILABLE | 4038 | 503 | Downstream timeout or circuit open |
| STORY_DESCRIPTION_REQUIRED | 4039 | 400 | Description required for Ready transition |
| MAX_LABELS_PER_STORY | 4040 | 400 | Maximum 10 labels per story |

### 3.5 UtilityService Error Codes (6001–6015)

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| AUDIT_LOG_IMMUTABLE | 6001 | 405 | Cannot modify or delete audit logs |
| ERROR_CODE_DUPLICATE | 6002 | 409 | Duplicate error code entry |
| ERROR_CODE_NOT_FOUND | 6003 | 404 | Unknown error code |
| NOTIFICATION_DISPATCH_FAILED | 6004 | 500 | All notification channels failed |
| REFERENCE_DATA_NOT_FOUND | 6005 | 404 | Unknown reference data ID |
| ORGANIZATION_MISMATCH | 6006 | 403 | Cross-organization access |
| TEMPLATE_NOT_FOUND | 6007 | 404 | Notification template not found |
| NOT_FOUND | 6008 | 404 | Entity not found |
| CONFLICT | 6009 | 409 | Duplicate or state conflict |
| SERVICE_UNAVAILABLE | 6010 | 503 | Downstream timeout or circuit open |
| INVALID_NOTIFICATION_TYPE | 6011 | 400 | Unknown notification type |
| INVALID_CHANNEL | 6012 | 400 | Unknown notification channel |
| RETENTION_PERIOD_INVALID | 6013 | 400 | Retention period must be > 0 days |
| REFERENCE_DATA_DUPLICATE | 6014 | 409 | Duplicate reference data entry |
| OUTBOX_PROCESSING_FAILED | 6015 | 500 | Outbox message could not be processed |

---

## 4. SecurityService Requirements

SecurityService handles all authentication, authorization, and security concerns. It does NOT own user records — ProfileService is the source of truth for TeamMember data. SecurityService resolves user identity by calling ProfileService via service-to-service JWT, with a 15-minute Redis cache.

**Port:** 5001  
**Database:** `nexus_security`

---

### REQ-001: Team Member Login

**User Story:** As a team member, I want to log in with my email and password so that I can access the platform.

**Acceptance Criteria:**

- WHEN a team member submits valid email and password to `POST /api/v1/auth/login` THEN the system returns HTTP 200 with `accessToken`, `refreshToken`, `expiresIn`, and `isFirstTimeUser` flag.
- WHEN the email does not match any team member THEN the system returns HTTP 401 with error code `INVALID_CREDENTIALS` (2001).
- WHEN the password does not match the stored BCrypt hash THEN the system increments the lockout counter in Redis (`lockout:{email}`) and returns HTTP 401 with `INVALID_CREDENTIALS` (2001).
- WHEN login succeeds THEN the system resets the lockout counter, creates a session in Redis (`session:{userId}:{deviceId}`), stores the BCrypt-hashed refresh token (`refresh:{userId}:{deviceId}`), and publishes an audit event to `outbox:security`.
- WHEN the user record is not in Redis cache (`user_cache:{userId}`) THEN the system calls ProfileService `GET /api/v1/team-members/by-email/{email}` via service-to-service JWT and caches the result for 15 minutes.

---

### REQ-002: JWT Token Structure (Department-Aware)

**User Story:** As the platform, I want JWT tokens to contain organization, department, and role claims so that downstream services can enforce department-scoped authorization.

**Acceptance Criteria:**

- WHEN a JWT access token is issued THEN it contains claims: `userId` (Guid), `organizationId` (Guid), `departmentId` (Guid — primary department), `roleName` (string: OrgAdmin/DeptLead/Member/Viewer), `departmentRole` (string), `deviceId` (string), and `jti` (unique token ID).
- WHEN the access token is issued THEN its TTL is configurable via `ACCESS_TOKEN_EXPIRY_MINUTES` (default 15 minutes).
- WHEN a refresh token is issued THEN it is BCrypt-hashed before storage in Redis with a configurable TTL via `REFRESH_TOKEN_EXPIRY_DAYS` (default 7 days).

---

### REQ-003: Token Refresh and Rotation

**User Story:** As a team member, I want my session to persist seamlessly so that I don't have to re-login frequently.

**Acceptance Criteria:**

- WHEN a valid refresh token is submitted to `POST /api/v1/auth/refresh` THEN the system invalidates the old refresh token, issues a new access/refresh token pair, and returns HTTP 200.
- WHEN a previously-used (already-rotated) refresh token is submitted THEN the system detects reuse, revokes ALL sessions for the user, and returns HTTP 401 with `REFRESH_TOKEN_REUSE` (2013).
- WHEN the refresh token has expired THEN the system returns HTTP 401 with `SESSION_EXPIRED` (2024).

---

### REQ-004: Logout

**User Story:** As a team member, I want to log out so that my session is invalidated.

**Acceptance Criteria:**

- WHEN a team member calls `POST /api/v1/auth/logout` with a valid Bearer token THEN the system removes the session from Redis, adds the JWT's `jti` to the token blacklist (`blacklist:{jti}`) with TTL equal to the remaining token lifetime, and returns HTTP 200.

---

### REQ-005: First Login Forced Password Reset

**User Story:** As a new team member, I want to be forced to change my temporary password on first login so that my account is secured.

**Acceptance Criteria:**

- WHEN a team member with `IsFirstTimeUser=true` attempts to access any endpoint other than `POST /api/v1/password/forced-change` THEN the `FirstTimeUserMiddleware` returns HTTP 403 with `FIRST_TIME_USER_RESTRICTED` (2006).
- WHEN the team member submits a new password to `POST /api/v1/password/forced-change` THEN the system validates password complexity, stores the BCrypt hash, sets `IsFirstTimeUser=false` via ProfileService, records the old password in `password_history`, and returns HTTP 200.
- WHEN the new password matches the temporary password THEN the system returns HTTP 400 with `PASSWORD_REUSE_NOT_ALLOWED` (2004).

---

### REQ-006: Organization Admin Credential Generation

**User Story:** As the platform, I want to generate initial credentials for invited team members so that they can log in for the first time.

**Acceptance Criteria:**

- WHEN ProfileService calls `POST /api/v1/auth/credentials/generate` with `{memberId, email}` via service-to-service JWT THEN SecurityService generates a temporary password, BCrypt-hashes it, stores it, sets `IsFirstTimeUser=true`, publishes a credential notification to `outbox:security`, and returns HTTP 200.
- WHEN the endpoint is called without a valid service-to-service JWT THEN the system returns HTTP 403 with `SERVICE_NOT_AUTHORIZED` (2016).

---

### REQ-007: Department-Based RBAC

**User Story:** As an organization, I want role-based access control scoped to departments so that team members only have permissions relevant to their department context.

**Acceptance Criteria:**

- WHEN a request reaches `RoleAuthorizationMiddleware` THEN the middleware extracts `roleName` and `departmentId` from JWT claims and compares against endpoint-level role requirements.
- WHEN an OrgAdmin makes any request THEN access is granted regardless of department (organization-wide access).
- WHEN a DeptLead makes a department-scoped request THEN access is granted only if the user belongs to the target department.
- WHEN a Member attempts to assign a task to another member THEN the system returns HTTP 403 with `INSUFFICIENT_PERMISSIONS` (2011) — only OrgAdmin and DeptLead can assign tasks.
- WHEN a Viewer attempts to create or modify any entity THEN the system returns HTTP 403 with `INSUFFICIENT_PERMISSIONS` (2011).
- WHEN a user attempts a department-scoped operation on a department they don't belong to THEN the system returns HTTP 403 with `DEPARTMENT_ACCESS_DENIED` (2020).

**Department Access Matrix:**

| Operation | OrgAdmin | DeptLead | Member | Viewer |
|-----------|----------|----------|--------|--------|
| Create story | ✅ | ✅ | ✅ | ❌ |
| Assign story to any dept | ✅ | ❌ | ❌ | ❌ |
| Assign story within dept | ✅ | ✅ | ❌ | ❌ |
| Create task | ✅ | ✅ | ✅ | ❌ |
| Assign task to any dept | ✅ | ❌ | ❌ | ❌ |
| Assign task within dept | ✅ | ✅ | ❌ | ❌ |
| Self-assign task | ✅ | ✅ | ✅ | ❌ |
| Manage sprint | ✅ | ✅ | ❌ | ❌ |
| View board | ✅ | ✅ | ✅ | ✅ |
| Manage organization | ✅ | ❌ | ❌ | ❌ |
| Manage department | ✅ | ✅ (own) | ❌ | ❌ |
| Invite members | ✅ | ✅ (own dept) | ❌ | ❌ |

---

### REQ-008: Session Management (Multi-Device, Redis-Backed)

**User Story:** As a team member, I want to manage my active sessions across multiple devices so that I can control where I'm logged in.

**Acceptance Criteria:**

- WHEN a team member calls `GET /api/v1/sessions` THEN the system returns all active sessions for the user, each with device info, IP address, and creation timestamp.
- WHEN a team member calls `DELETE /api/v1/sessions/{sessionId}` THEN the system removes that session from Redis and blacklists the corresponding JWT's `jti`.
- WHEN a team member calls `DELETE /api/v1/sessions/all` THEN the system revokes all sessions except the current one.
- WHEN a session is revoked THEN the JWT's `jti` is added to `blacklist:{jti}` with TTL equal to the remaining token lifetime.

---

### REQ-009: OTP Verification

**User Story:** As a team member, I want OTP-based verification for sensitive operations so that my account is protected.

**Acceptance Criteria:**

- WHEN `POST /api/v1/auth/otp/request` is called with a valid identity THEN the system generates a 6-digit numeric code, stores it in Redis (`otp:{identity}`) with 5-minute TTL and attempt counter, and dispatches the code via notification.
- WHEN `POST /api/v1/auth/otp/verify` is called with the correct code within the TTL THEN the system returns HTTP 200 with verification success.
- WHEN the wrong code is submitted THEN the attempt counter increments. After 3 failed attempts, the system returns HTTP 429 with `OTP_MAX_ATTEMPTS` (2009).
- WHEN the OTP has expired THEN the system returns HTTP 400 with `OTP_EXPIRED` (2007).
- WHEN OTP requests exceed 3 per 5-minute window THEN the system returns HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010).

---

### REQ-010: Account Lockout

**User Story:** As the platform, I want to lock accounts after repeated failed login attempts so that brute-force attacks are mitigated.

**Acceptance Criteria:**

- WHEN a user fails login 10 times (configurable via `ACCOUNT_LOCKOUT_MAX_ATTEMPTS`) within 24 hours (configurable via `ACCOUNT_LOCKOUT_WINDOW_HOURS`) THEN the system sets `lockout:locked:{identity}` in Redis with 60-minute TTL (configurable via `ACCOUNT_LOCKOUT_DURATION_MINUTES`) and publishes an audit event.
- WHEN a locked user attempts to log in THEN the system returns HTTP 423 with `ACCOUNT_LOCKED` (2002) without checking credentials.
- WHEN the lockout duration expires THEN the user can attempt login again.

---

### REQ-011: Password Management

**User Story:** As a team member, I want secure password management with complexity enforcement and history tracking so that my account remains protected.

**Acceptance Criteria:**

- WHEN a password is set or changed THEN it must meet complexity rules: minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 digit, 1 special character (`!@#$%^&*`).
- WHEN a password fails complexity validation THEN the system returns HTTP 400 with `PASSWORD_COMPLEXITY_FAILED` (2018).
- WHEN a new password matches any of the last 5 passwords in `password_history` THEN the system returns HTTP 400 with `PASSWORD_RECENTLY_USED` (2005).
- WHEN `POST /api/v1/password/reset/request` is called THEN the system sends an OTP to the registered email.
- WHEN `POST /api/v1/password/reset/confirm` is called with a valid OTP and new password THEN the system updates the password hash via ProfileService, records the old hash in `password_history`, and returns HTTP 200.

---

### REQ-012: Rate Limiting

**User Story:** As the platform, I want sliding-window rate limiting so that the system is protected from abuse.

**Acceptance Criteria:**

- WHEN login attempts exceed 5 per 15-minute window (per IP) THEN the system returns HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010) and a `Retry-After` header.
- WHEN OTP requests exceed 3 per 5-minute window (per IP) THEN the system returns HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010).
- WHEN authenticated requests exceed the configurable per-user limit THEN the system returns HTTP 429 with `RATE_LIMIT_EXCEEDED` (2010).
- WHEN rate limiting is enforced THEN it uses a sliding window algorithm implemented via Redis Lua script with key pattern `rate:{identity}:{endpoint}`.

---

### REQ-013: Service-to-Service JWT Auth

**User Story:** As a backend service, I want to authenticate with other services using short-lived JWTs so that inter-service communication is secure.

**Acceptance Criteria:**

- WHEN a service calls `POST /api/v1/service-tokens/issue` with valid service credentials THEN the system issues a short-lived JWT with `serviceId` and `serviceName` claims (no `organizationId`), caches it in Redis (`service_token:{serviceId}`, 23-hour TTL), and returns HTTP 200.
- WHEN a downstream service receives a request with a service JWT THEN it validates the token using the shared secret and checks the service ACL.
- WHEN the calling service is not in the ACL THEN the system returns HTTP 403 with `SERVICE_NOT_AUTHORIZED` (2016).
- WHEN the cached service token is within 30 seconds of expiry THEN the service client automatically refreshes it.

---

### REQ-014: Anomaly Detection

**User Story:** As the platform, I want to detect suspicious login activity so that compromised accounts are flagged.

**Acceptance Criteria:**

- WHEN a user logs in from a new IP address THEN the system checks the IP against the trusted set (`trusted_ips:{userId}`, 90-day TTL).
- WHEN the new IP's geo-location differs significantly from trusted IPs THEN the system flags the login as suspicious, publishes an audit event, and returns HTTP 403 with `SUSPICIOUS_LOGIN` (2017).
- WHEN a login succeeds from a known IP THEN the trusted IP set TTL is refreshed.

---

### REQ-015: Token Blacklist Enforcement

**User Story:** As the platform, I want revoked tokens to be immediately rejected so that logged-out sessions cannot be reused.

**Acceptance Criteria:**

- WHEN any request arrives with a Bearer token THEN `TokenBlacklistMiddleware` checks `blacklist:{jti}` in Redis.
- WHEN the `jti` exists in the blacklist THEN the system returns HTTP 401 with `TOKEN_REVOKED` (2012).
- WHEN the `jti` is not blacklisted THEN the request proceeds through the pipeline.

---

### REQ-016: Organization Scope Enforcement

**User Story:** As the platform, I want all requests to be scoped to the authenticated user's organization so that cross-organization data access is prevented.

**Acceptance Criteria:**

- WHEN any authenticated request is processed THEN `OrganizationScopeMiddleware` extracts `organizationId` from JWT claims and validates it against route/query parameters.
- WHEN a request attempts to access data from a different organization THEN the system returns HTTP 403 with `ORGANIZATION_MISMATCH` (2019).
- WHEN inter-service calls are made THEN the `X-Organization-Id` header is propagated.

---

### REQ-017: SecurityService Middleware Pipeline

**User Story:** As the platform, I want a well-defined middleware pipeline so that security concerns are enforced in the correct order.

**Acceptance Criteria:**

- WHEN a request enters SecurityService THEN middleware executes in this order: `CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → FirstTimeUserGuard → RoleAuthorization → OrganizationScope → Controllers`.
- WHEN `FirstTimeUserMiddleware` detects `IsFirstTimeUser=true` THEN it blocks all endpoints except `POST /api/v1/password/forced-change`.

---

### REQ-018: SecurityService API Endpoints

**User Story:** As a developer, I want a complete set of security endpoints so that all authentication and authorization flows are supported.

**Acceptance Criteria:**

- WHEN the SecurityService is deployed THEN the following endpoints are available:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/auth/login` | None | Team member login |
| POST | `/api/v1/auth/logout` | Bearer | Invalidate session |
| POST | `/api/v1/auth/refresh` | None | Rotate refresh token |
| POST | `/api/v1/auth/otp/request` | None | Request OTP |
| POST | `/api/v1/auth/otp/verify` | None | Verify OTP |
| POST | `/api/v1/auth/credentials/generate` | Service | Generate initial credentials |
| POST | `/api/v1/password/forced-change` | Bearer | First-time password change |
| POST | `/api/v1/password/reset/request` | None | Request password reset |
| POST | `/api/v1/password/reset/confirm` | None | Confirm password reset |
| GET | `/api/v1/sessions` | Bearer | List active sessions |
| DELETE | `/api/v1/sessions/{sessionId}` | Bearer | Revoke specific session |
| DELETE | `/api/v1/sessions/all` | Bearer | Revoke all except current |
| POST | `/api/v1/service-tokens/issue` | Service | Issue service-to-service JWT |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

---

### REQ-019: SecurityService Redis Key Patterns

**User Story:** As a developer, I want well-defined Redis key patterns so that caching, sessions, and rate limiting are consistent.

**Acceptance Criteria:**

- WHEN SecurityService uses Redis THEN it follows these key patterns:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `rate:{identity}:{endpoint}` | Sliding window rate limit counters | Window duration |
| `otp:{identity}` | OTP code + attempt counter | 5 min |
| `session:{userId}:{deviceId}` | Active session metadata | Access token expiry |
| `refresh:{userId}:{deviceId}` | Refresh token hash | 7 days |
| `blacklist:{jti}` | Revoked access token JTI | Remaining token TTL |
| `lockout:{identity}` | Failed login attempt counter | 24 hours |
| `lockout:locked:{identity}` | Account locked flag | 1 hour |
| `trusted_ips:{userId}` | Set of known IP addresses | 90 days |
| `service_token:{serviceId}` | Service-to-service JWT cache | 23 hours |
| `outbox:security` | Outbox queue for audit events | Until processed |
| `user_cache:{userId}` | Cached user record from ProfileService | 15 min |

---

### REQ-020: SecurityService Data Models

**User Story:** As a developer, I want well-defined data models so that the SecurityService database schema is clear.

**Acceptance Criteria:**

- WHEN SecurityService database is created THEN it contains:
  - `password_history` table: `PasswordHistoryId` (Guid PK), `UserId` (Guid, indexed), `PasswordHash` (string, required), `DateCreated` (DateTime).
  - `service_token` table: `ServiceTokenId` (Guid PK), `ServiceId` (string, indexed, required), `ServiceName` (string, required), `TokenHash` (string, required), `DateCreated` (DateTime), `ExpiryDate` (DateTime), `IsRevoked` (bool).

---

## 5. ProfileService Requirements

ProfileService is the identity and organization management hub. It owns all team member records and serves as the source of truth for user identity resolution by other services. It also manages the organization hierarchy, department structure, and story ID prefix configuration.

**Port:** 5002  
**Database:** `nexus_profile`

---

### REQ-021: Organization Management

**User Story:** As an OrgAdmin, I want to create and manage organizations so that the platform can support multiple tenants.

**Acceptance Criteria:**

- WHEN an OrgAdmin calls `POST /api/v1/organizations` with valid data THEN the system creates the organization with status `A`, seeds 5 default departments (Engineering/ENG, QA/QA, DevOps/DEVOPS, Product/PROD, Design/DESIGN), and returns HTTP 201.
- WHEN the organization name already exists THEN the system returns HTTP 409 with `ORGANIZATION_NAME_DUPLICATE` (3005).
- WHEN `GET /api/v1/organizations/{id}` is called THEN the system returns the organization details including `StoryIdPrefix`, `TimeZone`, and `DefaultSprintDurationWeeks`.
- WHEN `PUT /api/v1/organizations/{id}` is called THEN the system updates the organization and returns HTTP 200.
- WHEN `PATCH /api/v1/organizations/{id}/status` is called THEN the system transitions the organization through the `A → S → D` lifecycle.

---

### REQ-022: Organization Settings

**User Story:** As an OrgAdmin, I want to configure organization-level settings so that the platform adapts to our workflow.

**Acceptance Criteria:**

- WHEN `PUT /api/v1/organizations/{id}/settings` is called THEN the system updates the organization settings stored as a JSON column `SettingsJson` on the Organization entity, deserialized into a typed `OrganizationSettings` class.
- WHEN the `StoryIdPrefix` is set THEN it must match `^[A-Z0-9]{2,10}$` (2–10 uppercase alphanumeric characters). Invalid format returns HTTP 400 with `STORY_PREFIX_INVALID_FORMAT` (3020).
- WHEN the `StoryIdPrefix` already exists for another organization THEN the system returns HTTP 409 with `STORY_PREFIX_DUPLICATE` (3006).
- WHEN the `StoryIdPrefix` is changed after stories exist (checked via WorkService) THEN the system returns HTTP 400 with `STORY_PREFIX_IMMUTABLE` (3007).
- WHEN `DefaultSprintDurationWeeks` is set THEN it must be between 1 and 4 (inclusive).
- WHEN organization settings are updated THEN the Redis cache `org_settings:{organizationId}` is invalidated (60-min TTL on cache).

**Organization Settings Fields:**

*General Settings:*
- `StoryIdPrefix` — 2–10 uppercase alphanumeric characters (required, unique across orgs)
- `TimeZone` — IANA timezone string (e.g., `"Africa/Lagos"`), default `"UTC"`
- `DefaultSprintDurationWeeks` — integer 1–4, default `2`
- `WorkingDays` — array of day names, default `["Monday","Tuesday","Wednesday","Thursday","Friday"]`
- `WorkingHoursStart` / `WorkingHoursEnd` — time strings (e.g., `"09:00"` / `"17:00"`)
- `LogoUrl` — optional string, for white-labeling
- `PrimaryColor` — hex color string (e.g., `"#3B82F6"`), for branding

*Workflow Settings:*
- `StoryPointScale` — enum: `Fibonacci`, `Linear`, `TShirt` — default `Fibonacci`
- `RequiredFieldsByStoryType` — JSON object mapping story types to required field arrays (e.g., Bug requires `"StepsToReproduce"`)
- `AutoAssignmentEnabled` — bool, default `false`
- `AutoAssignmentStrategy` — enum: `LeastLoaded`, `RoundRobin` — default `LeastLoaded`

*Board Settings:*
- `DefaultBoardView` — enum: `Kanban`, `Sprint`, `Backlog` — default `Kanban`
- `WipLimitsEnabled` — bool, default `false`
- `DefaultWipLimit` — int, default `0` (0 = unlimited)

*Notification Defaults:*
- `DefaultNotificationChannels` — comma-separated string: `"Email,Push,InApp"` — default all enabled
- `DigestFrequency` — enum: `Realtime`, `Hourly`, `Daily` — default `Realtime`

*Data Settings:*
- `AuditRetentionDays` — int, default `90`

**Storage:** Stored as a JSON column `SettingsJson` on the Organization entity, with a typed `OrganizationSettings` class for deserialization. Cached in Redis `org_settings:{organizationId}` with 60-min TTL.

---

### REQ-023: Department Management

**User Story:** As an OrgAdmin, I want to manage departments so that the organization structure reflects our team composition.

**Acceptance Criteria:**

- WHEN `POST /api/v1/departments` is called with valid data THEN the system creates a custom department with `IsDefault=false` and returns HTTP 201.
- WHEN the department name already exists within the organization THEN the system returns HTTP 409 with `DEPARTMENT_NAME_DUPLICATE` (3008).
- WHEN the department code already exists within the organization THEN the system returns HTTP 409 with `DEPARTMENT_CODE_DUPLICATE` (3009).
- WHEN `GET /api/v1/departments` is called THEN the system returns all departments for the organization (paginated, cached in Redis `dept_list:{organizationId}` for 30 minutes).
- WHEN `GET /api/v1/departments/{id}` is called THEN the system returns department details including member count.
- WHEN `PUT /api/v1/departments/{id}` is called by OrgAdmin or DeptLead (own department) THEN the system updates the department.
- WHEN `PATCH /api/v1/departments/{id}/status` is called to deactivate a department with active members THEN the system returns HTTP 400 with `DEPARTMENT_HAS_ACTIVE_MEMBERS` (3017).
- WHEN a predefined department (Engineering, QA, DevOps, Product, Design) is targeted for deletion THEN the system returns HTTP 400 with `DEFAULT_DEPARTMENT_CANNOT_DELETE` (3010).
- WHEN `GET /api/v1/departments/{id}/members` is called THEN the system returns all team members in that department with their roles.

---

### REQ-024: Team Member Management

**User Story:** As an OrgAdmin or DeptLead, I want to manage team members so that the right people are in the right departments with the right roles.

**Acceptance Criteria:**

- WHEN `GET /api/v1/team-members` is called THEN the system returns a paginated list of team members, filterable by department, role, status, and availability.
- WHEN `GET /api/v1/team-members/{id}` is called THEN the system returns the full team member profile including all department memberships with roles, skills, availability, and `MaxConcurrentTasks`.
- WHEN `PUT /api/v1/team-members/{id}` is called by OrgAdmin, DeptLead (for members in their department), or the member themselves THEN the system updates the profile.
- WHEN `PATCH /api/v1/team-members/{id}/status` is called by OrgAdmin to deactivate the last OrgAdmin THEN the system returns HTTP 400 with `LAST_ORGADMIN_CANNOT_DEACTIVATE` (3004).
- WHEN `PATCH /api/v1/team-members/{id}/availability` is called by the member THEN the system updates availability to one of: `Available`, `Busy`, `Away`, `Offline`. Invalid values return HTTP 400 with `INVALID_AVAILABILITY_STATUS` (3019).

---

### REQ-025: Team Member Department Assignment

**User Story:** As an OrgAdmin, I want to assign team members to multiple departments with different roles so that cross-functional collaboration is supported.

**Acceptance Criteria:**

- WHEN `POST /api/v1/team-members/{id}/departments` is called with `{departmentId, roleId}` THEN the system creates a `DepartmentMember` record and returns HTTP 200.
- WHEN the member is already in the target department THEN the system returns HTTP 409 with `MEMBER_ALREADY_IN_DEPARTMENT` (3011).
- WHEN `DELETE /api/v1/team-members/{id}/departments/{deptId}` is called to remove the member's last department THEN the system returns HTTP 400 with `MEMBER_MUST_HAVE_DEPARTMENT` (3012).
- WHEN `PATCH /api/v1/team-members/{id}/departments/{deptId}/role` is called THEN the system updates the member's role in that specific department.
- WHEN a team member belongs to multiple departments THEN they can have different roles in each (e.g., DeptLead in Engineering, Member in QA).

---

### REQ-026: Professional ID System for Team Members

**User Story:** As the platform, I want team members to have professional IDs so that they are easily identifiable across the organization.

**Acceptance Criteria:**

- WHEN a team member is created THEN the system generates a professional ID in the format `NXS-{DeptCode}-{SequentialNumber}` (e.g., `NXS-ENG-001` for the first Engineering member).
- WHEN the professional ID is generated THEN it is unique within the organization and sequential within the department.
- WHEN a team member transfers departments THEN their professional ID remains unchanged (it reflects the original department assignment).

---

### REQ-027: Role Management

**User Story:** As a team member, I want to understand the role hierarchy so that I know what permissions I have.

**Acceptance Criteria:**

- WHEN `GET /api/v1/roles` is called THEN the system returns all roles: OrgAdmin (PermissionLevel 100), DeptLead (75), Member (50), Viewer (25).
- WHEN `GET /api/v1/roles/{id}` is called THEN the system returns role details including `PermissionLevel` and `IsSystemRole`.
- WHEN the system is initialized THEN the 4 system roles are seeded and marked as `IsSystemRole=true`.

---

### REQ-028: Invitation System

**User Story:** As an OrgAdmin or DeptLead, I want to invite new team members to join the organization so that onboarding is streamlined.

**Acceptance Criteria:**

- WHEN `POST /api/v1/invites` is called with `{email, firstName, lastName, departmentId, roleId}` THEN the system generates a cryptographic token (128 chars max), sets a 48-hour expiry, publishes an email notification to `outbox:profile`, and returns HTTP 201.
- WHEN the invitee's email is already registered as a member THEN the system returns HTTP 409 with `INVITE_EMAIL_ALREADY_MEMBER` (3014).
- WHEN a DeptLead creates an invite THEN the invite is scoped to their own department only.
- WHEN `GET /api/v1/invites/{token}/validate` is called THEN the system returns invite details (organization name, department name, role) if the token is valid and not expired.
- WHEN `POST /api/v1/invites/{token}/accept` is called with OTP verification THEN the system creates a TeamMember, creates a DepartmentMember with the specified role, calls SecurityService to generate credentials, and returns HTTP 200.
- WHEN the invite token is expired or already used THEN the system returns HTTP 410 with `INVITE_EXPIRED_OR_INVALID` (3002).
- WHEN `DELETE /api/v1/invites/{id}` is called THEN the invite is cancelled.

---

### REQ-029: Device Management

**User Story:** As a team member, I want to manage my registered devices so that I can control which devices have access.

**Acceptance Criteria:**

- WHEN `GET /api/v1/devices` is called THEN the system returns all devices for the authenticated user with `DeviceName`, `DeviceType` (Desktop/Mobile/Tablet), `IsPrimary`, `IpAddress`, `LastActiveDate`.
- WHEN a new device is registered and the user already has 5 devices THEN the system returns HTTP 400 with `MAX_DEVICES_REACHED` (3003).
- WHEN `PATCH /api/v1/devices/{id}/primary` is called THEN the system sets the specified device as primary and unsets the previous primary.
- WHEN `DELETE /api/v1/devices/{id}` is called THEN the device is removed and its associated session is revoked.

---

### REQ-030: Notification Settings

**User Story:** As a team member, I want to configure my notification preferences so that I only receive relevant notifications.

**Acceptance Criteria:**

- WHEN `GET /api/v1/notification-settings` is called THEN the system returns per-notification-type preferences with `IsEmail`, `IsPush`, and `IsInApp` toggles.
- WHEN `PUT /api/v1/notification-settings/{typeId}` is called THEN the system updates the preference for that notification type.
- WHEN `GET /api/v1/notification-types` is called THEN the system returns all 8 notification types: StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching.
- WHEN notification channel preferences are resolved THEN they cascade in this order: User preference → Department override (`NotificationChannelOverrides`) → Organization default (`DefaultNotificationChannels`) → System default (all channels enabled).

---

### REQ-031: Team Member Capacity and Availability Tracking

**User Story:** As a DeptLead, I want to see team member capacity and availability so that I can make informed task assignment decisions.

**Acceptance Criteria:**

- WHEN a team member profile is retrieved THEN it includes `MaxConcurrentTasks` (default 5), `Availability` (Available/Busy/Away/Offline), and current active task count.
- WHEN `MaxConcurrentTasks` is updated THEN it must be a positive integer.
- WHEN a team member's availability is `Away` or `Offline` THEN the auto-assignment system excludes them from suggestions.

---

### REQ-032: ProfileService Internal Endpoints

**User Story:** As a backend service, I want internal endpoints for user resolution so that SecurityService and WorkService can look up team members.

**Acceptance Criteria:**

- WHEN SecurityService calls `GET /api/v1/team-members/by-email/{email}` with a service JWT THEN ProfileService returns the team member record including `TeamMemberId`, `PasswordHash`, `FlgStatus`, `IsFirstTimeUser`, `OrganizationId`, `PrimaryDepartmentId`, and `RoleName`.
- WHEN SecurityService calls `PATCH /api/v1/team-members/{id}/password` with a service JWT THEN ProfileService updates the password hash.
- WHEN these endpoints are called without a valid service JWT THEN the system returns HTTP 403 with `SERVICE_NOT_AUTHORIZED`.

---

### REQ-033: ProfileService API Endpoints (Complete)

**User Story:** As a developer, I want a complete set of profile endpoints so that all organization and team management flows are supported.

**Acceptance Criteria:**

- WHEN ProfileService is deployed THEN the following endpoints are available:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/organizations` | OrgAdmin | Create organization |
| GET | `/api/v1/organizations/{id}` | Bearer | Get organization |
| PUT | `/api/v1/organizations/{id}` | OrgAdmin | Update organization |
| PATCH | `/api/v1/organizations/{id}/status` | OrgAdmin | Activate/deactivate |
| PUT | `/api/v1/organizations/{id}/settings` | OrgAdmin | Update settings |
| POST | `/api/v1/departments` | OrgAdmin | Create department |
| GET | `/api/v1/departments` | Bearer | List departments |
| GET | `/api/v1/departments/{id}` | Bearer | Get department |
| PUT | `/api/v1/departments/{id}` | OrgAdmin, DeptLead | Update department |
| PATCH | `/api/v1/departments/{id}/status` | OrgAdmin | Activate/deactivate |
| GET | `/api/v1/departments/{id}/members` | Bearer | List department members |
| GET | `/api/v1/team-members` | Bearer | List team members |
| GET | `/api/v1/team-members/{id}` | Bearer | Get team member |
| PUT | `/api/v1/team-members/{id}` | OrgAdmin, DeptLead, Self | Update profile |
| PATCH | `/api/v1/team-members/{id}/status` | OrgAdmin | Activate/deactivate |
| PATCH | `/api/v1/team-members/{id}/availability` | Bearer, Self | Update availability |
| POST | `/api/v1/team-members/{id}/departments` | OrgAdmin | Add to department |
| DELETE | `/api/v1/team-members/{id}/departments/{deptId}` | OrgAdmin | Remove from department |
| PATCH | `/api/v1/team-members/{id}/departments/{deptId}/role` | OrgAdmin | Change department role |
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
| GET | `/api/v1/team-members/by-email/{email}` | Service | Internal: fetch member for auth |
| PATCH | `/api/v1/team-members/{id}/password` | Service | Internal: update password hash |
| PUT | `/api/v1/departments/{id}/preferences` | OrgAdmin, DeptLead | Update department preferences |
| GET | `/api/v1/departments/{id}/preferences` | Bearer | Get department preferences |
| GET | `/api/v1/preferences` | Bearer | Get user preferences |
| PUT | `/api/v1/preferences` | Bearer | Update user preferences |
| GET | `/api/v1/preferences/resolved` | Bearer | Get resolved preferences (all levels merged) |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

---

### REQ-034: ProfileService Redis Key Patterns

**User Story:** As a developer, I want well-defined Redis key patterns for ProfileService caching.

**Acceptance Criteria:**

- WHEN ProfileService uses Redis THEN it follows these key patterns:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `org_settings:{organizationId}` | Cached organization settings (prefix, timezone, sprint duration) | 60 min |
| `dept_list:{organizationId}` | Cached department list | 30 min |
| `member_profile:{memberId}` | Cached team member profile | 15 min |
| `dept_prefs:{departmentId}` | Cached department preferences | 30 min |
| `user_prefs:{userId}` | Cached user preferences | 15 min |
| `resolved_prefs:{userId}` | Cached resolved preferences (all levels merged) | 5 min |
| `outbox:profile` | Outbox queue for audit events | Until processed |
| `blacklist:{jti}` | Token deny list (shared) | Remaining token TTL |

---

### REQ-035: ProfileService Seed Data

**User Story:** As a developer, I want predefined seed data so that the system is ready to use after deployment.

**Acceptance Criteria:**

- WHEN the ProfileService database is initialized THEN the following seed data is created:
  - **Roles (4):** OrgAdmin (PermissionLevel 100), DeptLead (75), Member (50), Viewer (25) — all marked `IsSystemRole=true`.
  - **Default Departments (5, per organization):** Engineering (ENG), QA (QA), DevOps (DEVOPS), Product (PROD), Design (DESIGN) — all marked `IsDefault=true`.
  - **Notification Types (8):** StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching.

---

## 6. WorkService Requirements

WorkService is the core service of Nexus-2.0, managing the entire Agile workflow: stories with professional IDs, tasks with department-based assignment, sprints, boards, activity feeds, comments, and search.

**Port:** 5003  
**Database:** `nexus_work`

---

### REQ-036: Professional Story ID Generation

**User Story:** As a team member, I want stories to have professional, human-readable IDs so that they are easy to reference in conversations and documentation.

**Acceptance Criteria:**

- WHEN a story is created THEN the system generates a story key in the format `{OrgPrefix}-{SequenceNumber}` (e.g., `NEXUS-1`, `NEXUS-42`, `ACME-100`).
- WHEN the organization prefix is needed THEN the system checks Redis cache (`org_prefix:{organizationId}`, 60-min TTL) first, falling back to ProfileService `GET /api/v1/organizations/{orgId}/settings`.
- WHEN the sequence number is generated THEN it uses an atomic PostgreSQL `UPDATE ... RETURNING` on the `story_sequence` table, ensuring gap-free, monotonically increasing IDs even under concurrent creation.
- WHEN the organization's first story is created THEN the `story_sequence` row is initialized via `INSERT ... ON CONFLICT DO NOTHING`.
- WHEN sequence initialization fails THEN the system returns HTTP 500 with `STORY_SEQUENCE_INIT_FAILED` (4034).
- WHEN the story key is stored THEN it is unique per organization (unique index on `(OrganizationId, StoryKey)`).

---

### REQ-037: Story CRUD Operations

**User Story:** As a team member, I want to create, read, update, and delete stories so that I can manage the product backlog.

**Acceptance Criteria:**

- WHEN `POST /api/v1/stories` is called with `{title, description, acceptanceCriteria, priority, storyPoints, departmentId}` THEN the system generates a story key, sets status to `Backlog`, sets `ReporterId` to the authenticated user, and returns HTTP 201 with the full story including `StoryKey`.
- WHEN `GET /api/v1/stories/{id}` is called THEN the system returns the story detail including all tasks, comments count, labels, activity log, assignee info, and completion percentage (based on task completion).
- WHEN `GET /api/v1/stories` is called THEN the system returns a paginated list of stories, filterable by status, priority, department, assignee, sprint, labels, and date range.
- WHEN `GET /api/v1/stories/by-key/{storyKey}` is called (e.g., `NEXUS-42`) THEN the system resolves the story by key and returns the detail. If not found, returns HTTP 404 with `STORY_KEY_NOT_FOUND` (4020).
- WHEN `PUT /api/v1/stories/{id}` is called THEN the system updates the story fields and records changes in the activity log.
- WHEN `DELETE /api/v1/stories/{id}` is called (soft delete, sets `FlgStatus=D`) and the story is in an active sprint THEN the system returns HTTP 400 with `STORY_IN_ACTIVE_SPRINT` (4026).
- WHEN story points are set THEN they must be a Fibonacci number (1, 2, 3, 5, 8, 13, 21). Invalid values return HTTP 400 with `INVALID_STORY_POINTS` (4023).
- WHEN priority is set THEN it must be one of: Critical, High, Medium, Low. Invalid values return HTTP 400 with `INVALID_PRIORITY` (4024).

---

### REQ-038: Story Workflow State Machine

**User Story:** As a team member, I want stories to follow a defined workflow so that the development process is structured and trackable.

**Acceptance Criteria:**

- WHEN a story is created THEN its status is set to `Backlog`.
- WHEN `PATCH /api/v1/stories/{id}/status` is called with a new status THEN the system validates the transition against the state machine:

| From | To | Conditions |
|------|----|------------|
| Backlog | Ready | Story has title, description, and story points |
| Ready | InProgress | Story is assigned to a team member |
| InProgress | InReview | At least one task exists and all dev tasks are done |
| InReview | InProgress | Reviewer requests changes |
| InReview | QA | Reviewer approves |
| QA | InProgress | QA finds defects |
| QA | Done | All tasks complete, QA passed |
| Done | Closed | Stakeholder accepts |

- WHEN an invalid transition is attempted THEN the system returns HTTP 400 with `INVALID_STORY_TRANSITION` (4004).
- WHEN a story transitions to `Ready` without a description THEN the system returns HTTP 400 with `STORY_DESCRIPTION_REQUIRED` (4039).
- WHEN a story transitions to `Ready` without story points THEN the system returns HTTP 400 with `STORY_REQUIRES_POINTS` (4015).
- WHEN a story transitions to `InProgress` without an assignee THEN the system returns HTTP 400 with `STORY_REQUIRES_ASSIGNEE` (4013).
- WHEN a story transitions to `InReview` without tasks THEN the system returns HTTP 400 with `STORY_REQUIRES_TASKS` (4014).
- WHEN a story transitions to `Done` THEN `CompletedDate` is set to `DateTime.UtcNow`.
- WHEN a story status changes THEN an activity log entry is created and a `StoryStatusChanged` notification is published to `outbox:work`.

---

### REQ-039: Story Assignment and Department Tracking

**User Story:** As a DeptLead, I want to assign stories to team members and track which departments contribute so that cross-department collaboration is visible.

**Acceptance Criteria:**

- WHEN a story is assigned to a team member THEN the `AssigneeId` is set and a `StoryAssigned` notification is published.
- WHEN a story has tasks assigned to multiple departments THEN the story detail response includes a `departmentContributions` field showing which departments have tasks and their completion status.
- WHEN an OrgAdmin assigns a story to any department THEN the assignment succeeds.
- WHEN a DeptLead assigns a story THEN it can only be assigned within their department.

---

### REQ-040: Story Linking

**User Story:** As a team member, I want to link related stories so that dependencies and relationships are visible.

**Acceptance Criteria:**

- WHEN `POST /api/v1/stories/{id}/links` is called with `{targetStoryId, linkType}` THEN the system creates a bidirectional link. Link types: `blocks`, `is_blocked_by`, `relates_to`, `duplicates`.
- WHEN a story link is created THEN the inverse link is automatically created (e.g., if A `blocks` B, then B `is_blocked_by` A).
- WHEN `GET /api/v1/stories/{id}` is called THEN linked stories are included in the response.
- WHEN `DELETE /api/v1/stories/{id}/links/{linkId}` is called THEN both directions of the link are removed.

---

### REQ-041: Task CRUD Operations

**User Story:** As a team member, I want to create and manage tasks within stories so that work is broken down into actionable items.

**Acceptance Criteria:**

- WHEN `POST /api/v1/tasks` is called with `{storyId, title, description, taskType, priority, estimatedHours}` THEN the system creates the task with status `ToDo`, auto-maps the department based on task type, and returns HTTP 201.
- WHEN the task type is not one of `Development`, `Testing`, `DevOps`, `Design`, `Documentation`, `Bug` THEN the system returns HTTP 400 with `INVALID_TASK_TYPE` (4025).
- WHEN `GET /api/v1/tasks/{id}` is called THEN the system returns the task detail including parent story key, assignee info, department, and time tracking.
- WHEN `GET /api/v1/stories/{storyId}/tasks` is called THEN the system returns all tasks for the story.
- WHEN `PUT /api/v1/tasks/{id}` is called THEN the system updates the task and records changes in the activity log.
- WHEN `DELETE /api/v1/tasks/{id}` is called (soft delete) and the task is in `InProgress` status THEN the system returns HTTP 400 with `TASK_IN_PROGRESS` (4027).

---

### REQ-042: Task Department-Based Assignment

**User Story:** As the platform, I want tasks to be automatically routed to the correct department based on task type so that work flows to the right team.

**Acceptance Criteria:**

- WHEN a task is created with a `TaskType` THEN the system auto-maps the department:

| Task Type | Default Department Code |
|-----------|------------------------|
| Development | ENG |
| Testing | QA |
| DevOps | DEVOPS |
| Design | DESIGN |
| Documentation | PROD |
| Bug | ENG |

- WHEN a task is created without an `AssigneeId` THEN the system can suggest an assignee via `GET /api/v1/tasks/suggest-assignee?taskType={type}&organizationId={orgId}`.
- WHEN suggesting an assignee THEN the system selects the available member in the mapped department with the lowest active task count who is under their `MaxConcurrentTasks` limit.
- WHEN a task is assigned to a member not in the target department THEN the system returns HTTP 400 with `ASSIGNEE_NOT_IN_DEPARTMENT` (4018).
- WHEN a task is assigned to a member who has reached `MaxConcurrentTasks` THEN the system returns HTTP 400 with `ASSIGNEE_AT_CAPACITY` (4019).

---

### REQ-043: Task Workflow State Machine

**User Story:** As a team member, I want tasks to follow a defined workflow so that progress is tracked consistently.

**Acceptance Criteria:**

- WHEN a task is created THEN its status is set to `ToDo`.
- WHEN `PATCH /api/v1/tasks/{id}/status` is called with a new status THEN the system validates the transition:

| From | To | Conditions |
|------|----|------------|
| ToDo | InProgress | Task has an assignee |
| InProgress | InReview | Assignee submits for review |
| InReview | InProgress | Reviewer requests changes |
| InReview | Done | Reviewer approves |

- WHEN an invalid transition is attempted THEN the system returns HTTP 400 with `INVALID_TASK_TRANSITION` (4005).
- WHEN a task transitions to `Done` THEN `CompletedDate` is set to `DateTime.UtcNow`.
- WHEN a task status changes THEN an activity log entry is created and a `TaskStatusChanged` notification is published to `outbox:work`.

---

### REQ-044: Task Time Tracking

**User Story:** As a team member, I want to log time against tasks so that effort is tracked accurately.

**Acceptance Criteria:**

- WHEN `POST /api/v1/tasks/{id}/time-log` is called with `{hours, description}` THEN the system adds the hours to `ActualHours` and records the time entry.
- WHEN hours is zero or negative THEN the system returns HTTP 400 with `HOURS_MUST_BE_POSITIVE` (4035).
- WHEN `GET /api/v1/tasks/{id}` is called THEN the response includes `EstimatedHours`, `ActualHours`, and the list of time log entries.

---

### REQ-045: Story-Task Traceability

**User Story:** As a project manager, I want complete traceability between stories and tasks so that I can track progress and department contributions.

**Acceptance Criteria:**

- WHEN `GET /api/v1/stories/{id}` is called THEN the response includes:
  - Total task count and completed task count
  - Completion percentage: `(CompletedTasks / TotalTasks) * 100`
  - Department contribution breakdown: `{ departmentName, taskCount, completedTaskCount }`
  - All tasks with their status, assignee, and department
- WHEN all tasks in a story reach `Done` THEN the story is eligible for the `QA → Done` transition.
- WHEN a task's parent story is retrieved THEN the story key is included in the task response.

---

### REQ-046: Sprint CRUD Operations

**User Story:** As a DeptLead or OrgAdmin, I want to create and manage sprints so that work is organized into time-boxed iterations.

**Acceptance Criteria:**

- WHEN `POST /api/v1/sprints` is called with `{sprintName, goal, startDate, endDate}` THEN the system creates the sprint with status `Planning` and returns HTTP 201.
- WHEN `endDate` is before `startDate` THEN the system returns HTTP 400 with `SPRINT_END_BEFORE_START` (4033).
- WHEN `GET /api/v1/sprints/{id}` is called THEN the system returns sprint details including stories, metrics, and burndown data.
- WHEN `GET /api/v1/sprints` is called THEN the system returns all sprints for the organization (paginated, filterable by status).
- WHEN `PUT /api/v1/sprints/{id}` is called THEN the system updates the sprint (only allowed in `Planning` status).
- WHEN the default sprint duration is not specified THEN it uses the organization's `DefaultSprintDurationWeeks` setting.

---

### REQ-047: Sprint Lifecycle

**User Story:** As a DeptLead, I want to manage the sprint lifecycle so that iterations are properly started, completed, and reviewed.

**Acceptance Criteria:**

- WHEN `PATCH /api/v1/sprints/{id}/start` is called THEN the sprint transitions from `Planning` to `Active`.
- WHEN a sprint is started and another sprint is already `Active` for the organization THEN the system returns HTTP 400 with `ONLY_ONE_ACTIVE_SPRINT` (4016).
- WHEN `PATCH /api/v1/sprints/{id}/complete` is called THEN the sprint transitions from `Active` to `Completed`, velocity is calculated (sum of story points for stories that reached `Done` or `Closed`), and incomplete stories are moved back to `Backlog` status with `SprintId` set to null.
- WHEN `PATCH /api/v1/sprints/{id}/cancel` is called THEN the sprint transitions to `Cancelled` and all stories are moved back to `Backlog`.
- WHEN a sprint is already completed THEN the system returns HTTP 400 with `SPRINT_ALREADY_COMPLETED` (4022).

---

### REQ-048: Sprint Planning (Story Assignment to Sprint)

**User Story:** As a DeptLead, I want to add and remove stories from a sprint so that the sprint backlog is properly planned.

**Acceptance Criteria:**

- WHEN `POST /api/v1/sprints/{sprintId}/stories` is called with `{storyId}` THEN the system adds the story to the sprint (creates `SprintStory` record) and sets the story's `SprintId`.
- WHEN stories are added to a sprint that is not in `Planning` status THEN the system returns HTTP 400 with `SPRINT_NOT_IN_PLANNING` (4006).
- WHEN a story is already in the sprint THEN the system returns HTTP 409 with `STORY_ALREADY_IN_SPRINT` (4007).
- WHEN `DELETE /api/v1/sprints/{sprintId}/stories/{storyId}` is called THEN the story is removed from the sprint (sets `RemovedDate` on `SprintStory`, clears story's `SprintId`).
- WHEN a story not in the sprint is targeted for removal THEN the system returns HTTP 400 with `STORY_NOT_IN_SPRINT` (4008).

---

### REQ-049: Sprint Metrics and Burndown

**User Story:** As a project manager, I want sprint metrics and burndown data so that I can track iteration progress.

**Acceptance Criteria:**

- WHEN `GET /api/v1/sprints/{id}/metrics` is called THEN the system returns:
  - `TotalStories`, `CompletedStories`, `TotalStoryPoints`, `CompletedStoryPoints`
  - `CompletionRate`: `(CompletedStories / TotalStories) * 100`
  - `Velocity`: `CompletedStoryPoints`
  - `StoriesByStatus`: Dictionary of status → count
  - `TasksByDepartment`: Dictionary of department → task count
  - `BurndownData`: Array of `{ date, remainingPoints, idealRemainingPoints }`
- WHEN burndown data is calculated THEN `IdealRemainingPoints` is a linear decrease from total points to 0 over the sprint duration, and `RemainingPoints` is total points minus completed points as of each day.
- WHEN sprint metrics are requested THEN results are cached in Redis (`sprint_metrics:{sprintId}`, 5-min TTL).

---

### REQ-050: Sprint Velocity Tracking

**User Story:** As a project manager, I want to track velocity across sprints so that I can forecast future capacity.

**Acceptance Criteria:**

- WHEN `GET /api/v1/sprints/velocity` is called THEN the system returns velocity data for the last N completed sprints (default 10), each with `SprintName`, `Velocity`, `StartDate`, `EndDate`.
- WHEN a sprint is completed THEN its velocity is calculated and stored in the `Velocity` field.
- WHEN velocity history is requested THEN it is sorted by sprint end date descending.

---

### REQ-051: Kanban Board View

**User Story:** As a team member, I want a kanban board view so that I can visualize work in progress.

**Acceptance Criteria:**

- WHEN `GET /api/v1/boards/kanban` is called with optional `sprintId` THEN the system returns stories grouped by workflow status columns.
- WHEN a `sprintId` is provided THEN only stories in that sprint are shown. When omitted, all active stories are shown.
- WHEN the board is returned THEN each column includes: `Status`, `CardCount`, `TotalPoints`, and an array of `KanbanCard` objects with `StoryKey`, `Title`, `Priority`, `StoryPoints`, `AssigneeName`, `AssigneeAvatarUrl`, `Labels`, `TaskCount`, `CompletedTaskCount`.
- WHEN board data is requested THEN results are cached in Redis (`board_kanban:{organizationId}:{sprintId}`, 2-min TTL).
- WHEN the board is filtered THEN it supports filtering by department, assignee, priority, and labels.

---

### REQ-052: Sprint Board View

**User Story:** As a team member, I want a sprint board showing the current sprint's tasks grouped by status so that I can track daily progress.

**Acceptance Criteria:**

- WHEN `GET /api/v1/boards/sprint` is called THEN the system returns the active sprint's tasks grouped by task status columns (ToDo, InProgress, InReview, Done).
- WHEN no sprint is active THEN the system returns an empty board with a message indicating no active sprint.
- WHEN the sprint board is returned THEN each card includes: `StoryKey`, `TaskTitle`, `TaskType`, `AssigneeName`, `DepartmentName`, `Priority`.

---

### REQ-053: Backlog View

**User Story:** As a product owner, I want a backlog view showing prioritized stories not in any sprint so that I can plan upcoming work.

**Acceptance Criteria:**

- WHEN `GET /api/v1/boards/backlog` is called THEN the system returns stories where `SprintId IS NULL`, sorted by priority (Critical > High > Medium > Low) then by `DateCreated`.
- WHEN the backlog is returned THEN it includes `TotalStories`, `TotalPoints`, and an array of `BacklogItem` objects with `StoryKey`, `Title`, `Priority`, `StoryPoints`, `Status`, `AssigneeName`, `Labels`, `TaskCount`, `DateCreated`.
- WHEN backlog data is requested THEN results are cached in Redis (`board_backlog:{organizationId}`, 2-min TTL).

---

### REQ-054: Department Board View

**User Story:** As a DeptLead, I want a department board showing tasks grouped by department so that I can see workload distribution.

**Acceptance Criteria:**

- WHEN `GET /api/v1/boards/department` is called with optional `sprintId` THEN the system returns tasks grouped by department, each department showing task count, member count, and tasks by status.
- WHEN department board data is requested THEN results are cached in Redis (`board_dept:{organizationId}:{sprintId}`, 2-min TTL).

---

### REQ-055: Comments on Stories and Tasks

**User Story:** As a team member, I want to comment on stories and tasks so that I can collaborate with my team.

**Acceptance Criteria:**

- WHEN `POST /api/v1/comments` is called with `{entityType, entityId, content, parentCommentId?}` THEN the system creates a comment, sets `AuthorId` to the authenticated user, and returns HTTP 201.
- WHEN `entityType` is `Story` or `Task` THEN the comment is associated with the corresponding entity.
- WHEN `parentCommentId` is provided THEN the comment is a reply (threaded comments).
- WHEN `PUT /api/v1/comments/{id}` is called by the author THEN the comment content is updated and `IsEdited` is set to `true`.
- WHEN `PUT /api/v1/comments/{id}` is called by a non-author THEN the system returns HTTP 403 with `COMMENT_NOT_AUTHOR` (4017).
- WHEN `DELETE /api/v1/comments/{id}` is called by the author or OrgAdmin THEN the comment is soft-deleted (`FlgStatus=D`).
- WHEN `GET /api/v1/stories/{id}/comments` or `GET /api/v1/tasks/{id}/comments` is called THEN the system returns threaded comments sorted by creation date.

---

### REQ-056: @Mentions in Comments

**User Story:** As a team member, I want to @mention colleagues in comments so that they are notified.

**Acceptance Criteria:**

- WHEN a comment contains `@{displayName}` or `@{email}` THEN the system resolves the mentioned user within the organization.
- WHEN the mentioned user is found THEN a `MentionedInComment` notification is published to `outbox:work` with `MentionerName`, `StoryKey`, and `CommentPreview` (first 100 characters).
- WHEN the mentioned user is not found in the organization THEN the system returns HTTP 400 with `MENTION_USER_NOT_FOUND` (4029).

---

### REQ-057: Label Management

**User Story:** As a team member, I want to create and apply labels to stories so that I can categorize and filter work.

**Acceptance Criteria:**

- WHEN `POST /api/v1/labels` is called with `{name, color}` THEN the system creates an organization-scoped label and returns HTTP 201.
- WHEN the label name already exists in the organization THEN the system returns HTTP 409 with `LABEL_NAME_DUPLICATE` (4011).
- WHEN `POST /api/v1/stories/{id}/labels` is called with `{labelId}` THEN the label is applied to the story.
- WHEN a story already has 10 labels THEN the system returns HTTP 400 with `MAX_LABELS_PER_STORY` (4040).
- WHEN `DELETE /api/v1/stories/{id}/labels/{labelId}` is called THEN the label is removed from the story.
- WHEN `GET /api/v1/labels` is called THEN the system returns all labels for the organization.

---

### REQ-058: Activity Log (Story/Task Timeline)

**User Story:** As a team member, I want to see a timeline of all changes to a story or task so that I can understand the history.

**Acceptance Criteria:**

- WHEN a story or task is created, updated, status-changed, assigned, or commented on THEN an `ActivityLog` entry is created with: `EntityType`, `EntityId`, `StoryKey`, `Action`, `ActorId`, `ActorName`, `OldValue`, `NewValue`, `Description`.
- WHEN `GET /api/v1/stories/{id}/activity` or `GET /api/v1/tasks/{id}/activity` is called THEN the system returns the activity timeline sorted by date descending.
- WHEN activity is logged THEN it is scoped to the organization via `OrganizationId`.

---

### REQ-059: Full-Text Search

**User Story:** As a team member, I want to search across stories and tasks so that I can quickly find relevant work items.

**Acceptance Criteria:**

- WHEN `GET /api/v1/search?q={query}` is called THEN the system performs full-text search across story titles, descriptions, acceptance criteria, task titles, and task descriptions.
- WHEN the query is less than 2 characters THEN the system returns HTTP 400 with `SEARCH_QUERY_TOO_SHORT` (4028).
- WHEN search results are returned THEN each result includes: `EntityType` (Story/Task), `StoryKey`, `Title`, `Status`, `Priority`, `AssigneeName`, relevance score.
- WHEN search is performed THEN results are scoped to the authenticated user's organization.
- WHEN search supports filtering THEN it accepts optional filters: `status`, `priority`, `department`, `assignee`, `sprint`, `labels`, `dateRange`.
- WHEN search results are requested THEN they are cached in Redis (`search_results:{hash}`, 1-min TTL).

---

### REQ-060: Saved Filters / Custom Views

**User Story:** As a team member, I want to save frequently used filter combinations so that I can quickly access my preferred views.

**Acceptance Criteria:**

- WHEN `POST /api/v1/saved-filters` is called with `{name, filters}` THEN the system saves the filter configuration for the authenticated user and returns HTTP 201.
- WHEN `GET /api/v1/saved-filters` is called THEN the system returns all saved filters for the user.
- WHEN `DELETE /api/v1/saved-filters/{id}` is called THEN the saved filter is removed.
- WHEN a saved filter is applied THEN it uses the same search/filter endpoint with the saved parameters.

---

### REQ-061: Reports — Sprint Velocity Chart

**User Story:** As a project manager, I want a sprint velocity chart so that I can track team performance over time.

**Acceptance Criteria:**

- WHEN `GET /api/v1/reports/velocity` is called with optional `count` parameter THEN the system returns velocity data for the last N completed sprints (default 10).
- WHEN velocity data is returned THEN each entry includes: `SprintName`, `Velocity` (completed story points), `TotalStoryPoints`, `CompletionRate`, `StartDate`, `EndDate`.

---

### REQ-062: Reports — Department Workload Distribution

**User Story:** As an OrgAdmin, I want to see workload distribution across departments so that I can identify bottlenecks.

**Acceptance Criteria:**

- WHEN `GET /api/v1/reports/department-workload` is called with optional `sprintId` THEN the system returns per-department metrics: `DepartmentName`, `TotalTasks`, `CompletedTasks`, `InProgressTasks`, `MemberCount`, `AvgTasksPerMember`.
- WHEN a `sprintId` is provided THEN metrics are scoped to that sprint. When omitted, metrics cover all active tasks.

---

### REQ-063: Reports — Team Member Capacity Utilization

**User Story:** As a DeptLead, I want to see team member capacity utilization so that I can balance workload.

**Acceptance Criteria:**

- WHEN `GET /api/v1/reports/capacity` is called with optional `departmentId` THEN the system returns per-member metrics: `MemberName`, `Department`, `ActiveTasks`, `MaxConcurrentTasks`, `UtilizationRate` (ActiveTasks/MaxConcurrentTasks * 100), `Availability`.
- WHEN `departmentId` is provided THEN results are filtered to that department.

---

### REQ-064: Reports — Story Cycle Time

**User Story:** As a project manager, I want to track story cycle time so that I can identify process improvements.

**Acceptance Criteria:**

- WHEN `GET /api/v1/reports/cycle-time` is called with optional date range THEN the system returns cycle time data for completed stories: `StoryKey`, `Title`, `CycleTimeDays` (Backlog → Done), `LeadTimeDays` (Created → Done), `CompletedDate`.
- WHEN cycle time is calculated THEN it measures the time from when a story entered `InProgress` to when it reached `Done`.

---

### REQ-065: Reports — Task Completion Rate by Department

**User Story:** As an OrgAdmin, I want to see task completion rates by department so that I can assess team effectiveness.

**Acceptance Criteria:**

- WHEN `GET /api/v1/reports/task-completion` is called with optional date range and `sprintId` THEN the system returns per-department: `DepartmentName`, `TotalTasks`, `CompletedTasks`, `CompletionRate`, `AvgCompletionTimeHours`.

---

### REQ-066: Workflow Customization

**User Story:** As an OrgAdmin, I want to customize workflows so that the platform adapts to our specific process.

**Acceptance Criteria:**

- WHEN `GET /api/v1/workflows` is called THEN the system returns the default workflow definitions for stories and tasks.
- WHEN `PUT /api/v1/workflows/organization` is called by OrgAdmin THEN the system saves organization-level workflow overrides (custom status names, additional statuses, modified transitions).
- WHEN `PUT /api/v1/workflows/department/{departmentId}` is called by OrgAdmin or DeptLead THEN the system saves department-level workflow overrides.
- WHEN workflow transitions are validated THEN the system checks organization-level overrides first, then falls back to default workflows.

---

### REQ-067: Real-Time Board Updates

**User Story:** As a team member, I want the board to update in near-real-time so that I see changes as they happen.

**Acceptance Criteria:**

- WHEN a story or task status changes THEN the system invalidates the relevant board caches (`board_kanban:*`, `board_dept:*`, `sprint_metrics:*`).
- WHEN the frontend polls for board updates THEN the short cache TTLs (2 minutes for boards, 5 minutes for metrics) ensure near-real-time data.
- WHEN WebSocket support is implemented (future enhancement) THEN the system publishes board change events to a Redis pub/sub channel for real-time push.

---

### REQ-068: WorkService API Endpoints (Complete)

**User Story:** As a developer, I want a complete set of work management endpoints so that all Agile workflow operations are supported.

**Acceptance Criteria:**

- WHEN WorkService is deployed THEN the following endpoints are available:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/stories` | Bearer (OrgAdmin, DeptLead, Member) | Create story |
| GET | `/api/v1/stories` | Bearer | List stories (paginated, filtered) |
| GET | `/api/v1/stories/{id}` | Bearer | Get story detail |
| GET | `/api/v1/stories/by-key/{storyKey}` | Bearer | Get story by key |
| PUT | `/api/v1/stories/{id}` | Bearer (OrgAdmin, DeptLead, Member) | Update story |
| DELETE | `/api/v1/stories/{id}` | Bearer (OrgAdmin, DeptLead) | Soft delete story |
| PATCH | `/api/v1/stories/{id}/status` | Bearer | Transition story status |
| POST | `/api/v1/stories/{id}/links` | Bearer | Create story link |
| DELETE | `/api/v1/stories/{id}/links/{linkId}` | Bearer | Remove story link |
| POST | `/api/v1/stories/{id}/labels` | Bearer | Apply label to story |
| DELETE | `/api/v1/stories/{id}/labels/{labelId}` | Bearer | Remove label from story |
| GET | `/api/v1/stories/{id}/comments` | Bearer | List story comments |
| GET | `/api/v1/stories/{id}/activity` | Bearer | Get story activity log |
| POST | `/api/v1/tasks` | Bearer (OrgAdmin, DeptLead, Member) | Create task |
| GET | `/api/v1/tasks/{id}` | Bearer | Get task detail |
| GET | `/api/v1/stories/{storyId}/tasks` | Bearer | List tasks for story |
| PUT | `/api/v1/tasks/{id}` | Bearer (OrgAdmin, DeptLead, Member) | Update task |
| DELETE | `/api/v1/tasks/{id}` | Bearer (OrgAdmin, DeptLead) | Soft delete task |
| PATCH | `/api/v1/tasks/{id}/status` | Bearer | Transition task status |
| POST | `/api/v1/tasks/{id}/time-log` | Bearer | Log time against task |
| GET | `/api/v1/tasks/{id}/comments` | Bearer | List task comments |
| GET | `/api/v1/tasks/{id}/activity` | Bearer | Get task activity log |
| GET | `/api/v1/tasks/suggest-assignee` | Bearer | Suggest assignee for task type |
| POST | `/api/v1/sprints` | Bearer (OrgAdmin, DeptLead) | Create sprint |
| GET | `/api/v1/sprints` | Bearer | List sprints |
| GET | `/api/v1/sprints/{id}` | Bearer | Get sprint detail |
| PUT | `/api/v1/sprints/{id}` | Bearer (OrgAdmin, DeptLead) | Update sprint |
| PATCH | `/api/v1/sprints/{id}/start` | Bearer (OrgAdmin, DeptLead) | Start sprint |
| PATCH | `/api/v1/sprints/{id}/complete` | Bearer (OrgAdmin, DeptLead) | Complete sprint |
| PATCH | `/api/v1/sprints/{id}/cancel` | Bearer (OrgAdmin, DeptLead) | Cancel sprint |
| POST | `/api/v1/sprints/{sprintId}/stories` | Bearer (OrgAdmin, DeptLead) | Add story to sprint |
| DELETE | `/api/v1/sprints/{sprintId}/stories/{storyId}` | Bearer (OrgAdmin, DeptLead) | Remove story from sprint |
| GET | `/api/v1/sprints/{id}/metrics` | Bearer | Get sprint metrics |
| GET | `/api/v1/sprints/velocity` | Bearer | Get velocity history |
| POST | `/api/v1/comments` | Bearer | Create comment |
| PUT | `/api/v1/comments/{id}` | Bearer (Author) | Edit comment |
| DELETE | `/api/v1/comments/{id}` | Bearer (Author, OrgAdmin) | Delete comment |
| POST | `/api/v1/labels` | Bearer (OrgAdmin, DeptLead) | Create label |
| GET | `/api/v1/labels` | Bearer | List labels |
| PUT | `/api/v1/labels/{id}` | Bearer (OrgAdmin, DeptLead) | Update label |
| DELETE | `/api/v1/labels/{id}` | Bearer (OrgAdmin) | Delete label |
| GET | `/api/v1/boards/kanban` | Bearer | Kanban board view |
| GET | `/api/v1/boards/sprint` | Bearer | Sprint board view |
| GET | `/api/v1/boards/backlog` | Bearer | Backlog view |
| GET | `/api/v1/boards/department` | Bearer | Department board view |
| GET | `/api/v1/search` | Bearer | Full-text search |
| POST | `/api/v1/saved-filters` | Bearer | Save filter |
| GET | `/api/v1/saved-filters` | Bearer | List saved filters |
| DELETE | `/api/v1/saved-filters/{id}` | Bearer | Delete saved filter |
| GET | `/api/v1/reports/velocity` | Bearer | Velocity chart data |
| GET | `/api/v1/reports/department-workload` | Bearer | Department workload |
| GET | `/api/v1/reports/capacity` | Bearer | Capacity utilization |
| GET | `/api/v1/reports/cycle-time` | Bearer | Story cycle time |
| GET | `/api/v1/reports/task-completion` | Bearer | Task completion rate |
| GET | `/api/v1/workflows` | Bearer | Get workflow definitions |
| PUT | `/api/v1/workflows/organization` | OrgAdmin | Override org workflows |
| PUT | `/api/v1/workflows/department/{deptId}` | OrgAdmin, DeptLead | Override dept workflows |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

---

### REQ-069: WorkService Redis Key Patterns

**User Story:** As a developer, I want well-defined Redis key patterns for WorkService caching.

**Acceptance Criteria:**

- WHEN WorkService uses Redis THEN it follows these key patterns:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `org_prefix:{organizationId}` | Cached organization story ID prefix | 60 min |
| `sprint_active:{organizationId}` | Cached active sprint ID | 5 min |
| `board_kanban:{organizationId}:{sprintId}` | Cached kanban board data | 2 min |
| `board_backlog:{organizationId}` | Cached backlog data | 2 min |
| `board_dept:{organizationId}:{sprintId}` | Cached department board data | 2 min |
| `sprint_metrics:{sprintId}` | Cached sprint metrics | 5 min |
| `story_detail:{storyId}` | Cached story detail | 5 min |
| `search_results:{hash}` | Cached search results by query hash | 1 min |
| `outbox:work` | Outbox queue for audit events and notifications | Until processed |
| `blacklist:{jti}` | Token deny list (shared) | Remaining token TTL |

---

### REQ-070: WorkService Data Models

**User Story:** As a developer, I want well-defined data models so that the WorkService database schema is clear.

**Acceptance Criteria:**

- WHEN WorkService database is created THEN it contains the following tables:

**story** — `StoryId` (Guid PK), `OrganizationId` (Guid FK), `StoryKey` (string, unique per org), `SequenceNumber` (long), `Title` (string, max 200), `Description` (string, max 5000), `AcceptanceCriteria` (string, max 5000), `StoryPoints` (int?), `Priority` (string), `Status` (string), `AssigneeId` (Guid?), `ReporterId` (Guid), `SprintId` (Guid?), `DepartmentId` (Guid?), `DueDate` (DateTime?), `CompletedDate` (DateTime?), `FlgStatus` (string), `DateCreated`, `DateUpdated`.

**task** — `TaskId` (Guid PK), `OrganizationId` (Guid FK), `StoryId` (Guid FK), `Title` (string, max 200), `Description` (string, max 3000), `TaskType` (string), `Status` (string), `Priority` (string), `AssigneeId` (Guid?), `DepartmentId` (Guid?), `EstimatedHours` (decimal?), `ActualHours` (decimal?), `DueDate` (DateTime?), `CompletedDate` (DateTime?), `FlgStatus` (string), `DateCreated`, `DateUpdated`.

**sprint** — `SprintId` (Guid PK), `OrganizationId` (Guid FK), `SprintName` (string, max 100), `Goal` (string, max 500), `StartDate` (DateTime), `EndDate` (DateTime), `Status` (string), `Velocity` (int?), `DateCreated`, `DateUpdated`.

**sprint_story** — `SprintStoryId` (Guid PK), `SprintId` (Guid FK), `StoryId` (Guid FK), `AddedDate` (DateTime), `RemovedDate` (DateTime?). Unique index on `(SprintId, StoryId)` where `RemovedDate IS NULL`.

**comment** — `CommentId` (Guid PK), `OrganizationId` (Guid FK), `EntityType` (string), `EntityId` (Guid), `AuthorId` (Guid FK), `Content` (string), `ParentCommentId` (Guid?), `IsEdited` (bool), `FlgStatus` (string), `DateCreated`, `DateUpdated`.

**activity_log** — `ActivityLogId` (Guid PK), `OrganizationId` (Guid FK), `EntityType` (string), `EntityId` (Guid), `StoryKey` (string), `Action` (string), `ActorId` (Guid), `ActorName` (string), `OldValue` (string?), `NewValue` (string?), `Description` (string), `DateCreated`.

**label** — `LabelId` (Guid PK), `OrganizationId` (Guid FK), `Name` (string), `Color` (string), `DateCreated`.

**story_label** — `StoryLabelId` (Guid PK), `StoryId` (Guid FK), `LabelId` (Guid FK). Unique index on `(StoryId, LabelId)`.

**story_link** — `StoryLinkId` (Guid PK), `OrganizationId` (Guid FK), `SourceStoryId` (Guid FK), `TargetStoryId` (Guid FK), `LinkType` (string), `DateCreated`.

**story_sequence** — `OrganizationId` (Guid PK), `CurrentValue` (long, default 0).

**saved_filter** — `SavedFilterId` (Guid PK), `OrganizationId` (Guid FK), `TeamMemberId` (Guid FK), `Name` (string), `Filters` (string — JSON), `DateCreated`.

---

## 7. UtilityService Requirements

UtilityService provides cross-cutting operational capabilities consumed by all other services, adapted for the Agile domain with specific notification templates for story-driven workflow events.

**Port:** 5200  
**Database:** `nexus_utility`

---

### REQ-071: Audit Logging

**User Story:** As an OrgAdmin, I want an immutable audit trail of all significant platform events so that I can review activity for compliance and troubleshooting.

**Acceptance Criteria:**

- WHEN `POST /api/v1/audit-logs` is called by a service (via service JWT) THEN the system creates an immutable audit log entry with: `OrganizationId`, `ServiceName`, `Action`, `EntityType`, `EntityId`, `UserId`, `OldValue` (JSON), `NewValue` (JSON), `IpAddress`, `CorrelationId`, `DateCreated`.
- WHEN `GET /api/v1/audit-logs` is called THEN the system returns paginated, filterable audit logs (by service, action, entity type, user, date range).
- WHEN any attempt is made to UPDATE or DELETE an audit log THEN the system returns HTTP 405 with `AUDIT_LOG_IMMUTABLE` (6001).
- WHEN `GET /api/v1/audit-logs/archive` is called THEN the system returns archived audit logs (moved by retention job).

---

### REQ-072: Error Logging with PII Redaction

**User Story:** As a developer, I want error logs with PII automatically redacted so that debugging is possible without exposing sensitive data.

**Acceptance Criteria:**

- WHEN `POST /api/v1/error-logs` is called by a service THEN the system creates an error log entry with PII fields (emails, names, IPs) replaced with `[REDACTED]`.
- WHEN `GET /api/v1/error-logs` is called by OrgAdmin THEN the system returns paginated, filterable error logs (by service, error code, severity, date range).
- WHEN error logs are stored THEN they include: `OrganizationId`, `ServiceName`, `ErrorCode`, `Message` (redacted), `StackTrace` (redacted), `CorrelationId`, `Severity` (Info/Warning/Error/Critical), `DateCreated`.

---

### REQ-073: Error Code Registry

**User Story:** As a developer, I want a centralized error code registry so that all services resolve error codes consistently.

**Acceptance Criteria:**

- WHEN `POST /api/v1/error-codes` is called by OrgAdmin THEN the system creates an error code entry with: `Code`, `Value`, `HttpStatusCode`, `ResponseCode`, `Description`, `ServiceName`.
- WHEN the error code already exists THEN the system returns HTTP 409 with `ERROR_CODE_DUPLICATE` (6002).
- WHEN `GET /api/v1/error-codes` is called THEN the system returns all error codes (used by `ErrorCodeResolverService` in each service for cache refresh).
- WHEN `PUT /api/v1/error-codes/{code}` is called THEN the system updates the error code entry.
- WHEN `DELETE /api/v1/error-codes/{code}` is called THEN the system removes the entry.
- WHEN an error code is resolved by a service THEN the resolution chain is: (1) in-memory `ConcurrentDictionary`, (2) Redis hash `error_codes_registry` (24h TTL), (3) HTTP call to UtilityService, (4) local static fallback map.

---

### REQ-074: Notification Dispatch

**User Story:** As the platform, I want to dispatch notifications via email, push, and in-app channels so that team members are informed of relevant events.

**Acceptance Criteria:**

- WHEN `POST /api/v1/notifications/dispatch` is called by a service THEN the system dispatches the notification via the specified channels (Email, Push, InApp).
- WHEN a notification is dispatched THEN a `NotificationLog` entry is created with: `OrganizationId`, `UserId`, `NotificationType`, `Channel`, `Recipient`, `Subject`, `Status` (Pending/Sent/Failed/PermanentlyFailed), `RetryCount`.
- WHEN all channels fail THEN the system returns HTTP 500 with `NOTIFICATION_DISPATCH_FAILED` (6004).
- WHEN `GET /api/v1/notification-logs` is called THEN the system returns the user's notification history (paginated).
- WHEN notifications are dispatched THEN user preferences are checked — if a user has disabled a channel for a notification type, that channel is skipped.

---

### REQ-075: Agile-Specific Notification Types

**User Story:** As a team member, I want to receive notifications for Agile workflow events so that I stay informed about relevant changes.

**Acceptance Criteria:**

- WHEN the following events occur THEN the corresponding notification is dispatched:

| Notification Type | Trigger | Template Variables |
|-------------------|---------|-------------------|
| `StoryAssigned` | Story assigned to member | `StoryKey`, `StoryTitle`, `AssignerName` |
| `TaskAssigned` | Task assigned to member | `StoryKey`, `TaskTitle`, `TaskType`, `AssignerName` |
| `SprintStarted` | Sprint activated | `SprintName`, `StartDate`, `EndDate`, `StoryCount` |
| `SprintEnded` | Sprint completed | `SprintName`, `Velocity`, `CompletionRate` |
| `MentionedInComment` | @mention in comment | `MentionerName`, `StoryKey`, `CommentPreview` |
| `StoryStatusChanged` | Story state transition | `StoryKey`, `StoryTitle`, `OldStatus`, `NewStatus` |
| `TaskStatusChanged` | Task state transition | `StoryKey`, `TaskTitle`, `OldStatus`, `NewStatus` |
| `DueDateApproaching` | Due date within 24 hours | `EntityType`, `StoryKey`, `Title`, `DueDate` |

- WHEN a notification is dispatched THEN it uses the outbox pattern — the originating service publishes to `outbox:{service}` and UtilityService processes it.

---

### REQ-076: Notification Templates

**User Story:** As the platform, I want pre-built notification templates so that notifications are professional and consistent.

**Acceptance Criteria:**

- WHEN a notification is dispatched THEN the system renders the appropriate template from the `Templates/` folder.
- WHEN email templates are rendered THEN they use HTML with Razor-style placeholders for template variables.
- WHEN push/in-app templates are rendered THEN they use plain-text versions.
- WHEN a template is not found THEN the system returns HTTP 404 with `TEMPLATE_NOT_FOUND` (6007).
- WHEN the system is deployed THEN 8 email templates and 8 push/in-app templates are available (one per notification type).

---

### REQ-077: Reference Data Management

**User Story:** As the platform, I want centralized reference data so that all services use consistent values.

**Acceptance Criteria:**

- WHEN `GET /api/v1/reference/department-types` is called THEN the system returns all department types with `TypeName` and `TypeCode`.
- WHEN `GET /api/v1/reference/priority-levels` is called THEN the system returns all priority levels with `Name`, `SortOrder`, and `Color`.
- WHEN `GET /api/v1/reference/task-types` is called THEN the system returns all task types with `TypeName` and `DefaultDepartmentCode`.
- WHEN `GET /api/v1/reference/workflow-states` is called THEN the system returns all workflow states with `EntityType` (Story/Task), `StateName`, and `SortOrder`.
- WHEN reference data is requested THEN results are cached in Redis (24h TTL) with keys: `ref:department_types`, `ref:priority_levels`, `ref:task_types`, `ref:workflow_states`.
- WHEN `POST /api/v1/reference/{type}` is called by OrgAdmin THEN the system creates a new reference data entry and invalidates the cache.
- WHEN a duplicate reference data entry is created THEN the system returns HTTP 409 with `REFERENCE_DATA_DUPLICATE` (6014).

---

### REQ-078: Retention Archival

**User Story:** As an OrgAdmin, I want audit logs to be automatically archived after a configurable period so that the active database stays performant.

**Acceptance Criteria:**

- WHEN the `RetentionArchivalHostedService` runs (daily at configured hour via `RETENTION_SCHEDULE_CRON`) THEN it moves audit logs older than `RETENTION_PERIOD_DAYS` (default 90) to the `archived_audit_log` table.
- WHEN archived logs are queried via `GET /api/v1/audit-logs/archive` THEN they are returned with the same pagination and filtering as active logs.
- WHEN `RETENTION_PERIOD_DAYS` is set to 0 or negative THEN the system returns HTTP 400 with `RETENTION_PERIOD_INVALID` (6013).

---

### REQ-079: Activity Feed Aggregation

**User Story:** As a team member, I want an aggregated activity feed so that I can see recent changes across all stories and tasks.

**Acceptance Criteria:**

- WHEN UtilityService processes outbox messages from all services THEN it creates audit log entries that can be queried as an activity feed.
- WHEN `GET /api/v1/audit-logs?action=StoryCreated,TaskAssigned,StatusChanged` is called THEN the system returns a filtered activity feed showing recent Agile events.
- WHEN the activity feed is queried THEN it is scoped to the authenticated user's organization.

---

### REQ-080: Background Services

**User Story:** As the platform, I want background services to handle async processing so that the system operates reliably.

**Acceptance Criteria:**

- WHEN UtilityService starts THEN the following background services are running:

| Service | Description | Interval |
|---------|-------------|----------|
| `OutboxProcessorHostedService` | Polls Redis outbox queues (`outbox:profile`, `outbox:security`, `outbox:work`), dispatches notifications and creates audit logs | Configurable (default 30s) |
| `RetentionArchivalHostedService` | Moves audit logs older than retention period to archive table | Daily at configured hour |
| `NotificationRetryHostedService` | Retries failed notification dispatches with exponential backoff (2^retryCount minutes), max 3 retries | Every 60 seconds |
| `DueDateNotificationHostedService` | Scans for stories/tasks with due dates within 24 hours and publishes `DueDateApproaching` notifications | Every 6 hours |

- WHEN outbox processing fails THEN the message is re-queued with an incremented retry counter. After 3 retries, it is moved to a dead-letter queue (`dlq:{service}`).

---

### REQ-081: UtilityService API Endpoints (Complete)

**User Story:** As a developer, I want a complete set of utility endpoints so that all operational concerns are addressed.

**Acceptance Criteria:**

- WHEN UtilityService is deployed THEN the following endpoints are available:

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/audit-logs` | Service | Create audit log entry |
| GET | `/api/v1/audit-logs` | Bearer | Query audit logs (paginated, filtered) |
| GET | `/api/v1/audit-logs/archive` | Bearer | Query archived audit logs |
| POST | `/api/v1/error-logs` | Service | Create error log (PII redacted) |
| GET | `/api/v1/error-logs` | OrgAdmin | Query error logs |
| POST | `/api/v1/error-codes` | OrgAdmin | Create error code entry |
| GET | `/api/v1/error-codes` | Bearer | List all error codes |
| PUT | `/api/v1/error-codes/{code}` | OrgAdmin | Update error code |
| DELETE | `/api/v1/error-codes/{code}` | OrgAdmin | Delete error code |
| POST | `/api/v1/notifications/dispatch` | Service | Dispatch notification event |
| GET | `/api/v1/notification-logs` | Bearer | User notification history |
| GET | `/api/v1/reference/department-types` | None | List department types |
| GET | `/api/v1/reference/priority-levels` | None | List priority levels |
| GET | `/api/v1/reference/task-types` | None | List task types with department mapping |
| GET | `/api/v1/reference/workflow-states` | None | List workflow states |
| POST | `/api/v1/reference/department-types` | OrgAdmin | Create department type |
| POST | `/api/v1/reference/priority-levels` | OrgAdmin | Create priority level |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

---

### REQ-082: UtilityService Redis Key Patterns

**User Story:** As a developer, I want well-defined Redis key patterns for UtilityService caching.

**Acceptance Criteria:**

- WHEN UtilityService uses Redis THEN it follows these key patterns:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `ref:department_types` | Cached department type list | 24h |
| `ref:priority_levels` | Cached priority level list | 24h |
| `ref:task_types` | Cached task type list | 24h |
| `ref:workflow_states` | Cached workflow state list | 24h |
| `notif_pref:{userId}:{typeId}` | Cached notification preferences | 5 min |
| `outbox:profile` | Inbound outbox from ProfileService | Until processed |
| `outbox:security` | Inbound outbox from SecurityService | Until processed |
| `outbox:work` | Inbound outbox from WorkService | Until processed |
| `blacklist:{jti}` | Token deny list (shared) | Remaining token TTL |
| `error_codes_registry` | Cached error code registry (hash) | 24h |

---

### REQ-083: UtilityService Data Models

**User Story:** As a developer, I want well-defined data models so that the UtilityService database schema is clear.

**Acceptance Criteria:**

- WHEN UtilityService database is created THEN it contains:

**audit_log** — `AuditLogId` (Guid PK), `OrganizationId` (Guid), `ServiceName` (string), `Action` (string), `EntityType` (string), `EntityId` (string), `UserId` (string), `OldValue` (string?), `NewValue` (string?), `IpAddress` (string?), `CorrelationId` (string), `DateCreated` (DateTime).

**archived_audit_log** — Same fields as `audit_log` plus `ArchivedAuditLogId` (Guid PK) and `ArchivedDate` (DateTime).

**error_log** — `ErrorLogId` (Guid PK), `OrganizationId` (Guid), `ServiceName` (string), `ErrorCode` (string), `Message` (string), `StackTrace` (string?), `CorrelationId` (string), `Severity` (string), `DateCreated` (DateTime).

**error_code_entry** — `ErrorCodeEntryId` (Guid PK), `Code` (string, unique index), `Value` (int), `HttpStatusCode` (int), `ResponseCode` (string, max 10), `Description` (string), `ServiceName` (string), `DateCreated`, `DateUpdated`.

**notification_log** — `NotificationLogId` (Guid PK), `OrganizationId` (Guid), `UserId` (Guid), `NotificationType` (string), `Channel` (string), `Recipient` (string), `Subject` (string?), `Status` (string), `RetryCount` (int), `LastRetryDate` (DateTime?), `DateCreated` (DateTime).

**department_type** — `DepartmentTypeId` (Guid PK), `TypeName` (string), `TypeCode` (string), `FlgStatus` (string).

**priority_level** — `PriorityLevelId` (Guid PK), `Name` (string), `SortOrder` (int), `Color` (string), `FlgStatus` (string).

**task_type_ref** — `TaskTypeRefId` (Guid PK), `TypeName` (string), `DefaultDepartmentCode` (string), `FlgStatus` (string).

**workflow_state** — `WorkflowStateId` (Guid PK), `EntityType` (string), `StateName` (string), `SortOrder` (int), `FlgStatus` (string).

---

### REQ-084: UtilityService Seed Data

**User Story:** As a developer, I want predefined reference data so that the system has consistent defaults.

**Acceptance Criteria:**

- WHEN the UtilityService database is initialized THEN the following seed data is created:

**Department Types (5+):** Engineering (ENG), QA (QA), DevOps (DEVOPS), Product (PROD), Design (DESIGN).

**Priority Levels (4):** Critical (SortOrder 1, Color #DC2626), High (SortOrder 2, Color #EA580C), Medium (SortOrder 3, Color #CA8A04), Low (SortOrder 4, Color #16A34A).

**Task Types (6):** Development (ENG), Testing (QA), DevOps (DEVOPS), Design (DESIGN), Documentation (PROD), Bug (ENG).

**Workflow States — Story (7):** Backlog (1), Ready (2), InProgress (3), InReview (4), QA (5), Done (6), Closed (7).

**Workflow States — Task (4):** ToDo (1), InProgress (2), InReview (3), Done (4).

---

### REQ-085: Outbox Message Format

**User Story:** As a developer, I want a standardized outbox message format so that all services publish events consistently.

**Acceptance Criteria:**

- WHEN a service publishes to its outbox queue THEN the message follows this format:

```json
{
  "Type": "notification | audit",
  "Payload": {
    "OrganizationId": "guid",
    "NotificationType": "StoryAssigned",
    "Channel": "Email,Push",
    "Recipient": "user@example.com",
    "Subject": "Story NEXUS-42 assigned to you",
    "TemplateVariables": {
      "StoryKey": "NEXUS-42",
      "StoryTitle": "Implement user authentication",
      "AssignerName": "Jane Smith"
    }
  },
  "Timestamp": "2025-01-01T00:00:00Z",
  "Id": "guid"
}
```

- WHEN an audit event is published THEN the `Type` is `"audit"` and the `Payload` contains audit log fields.
- WHEN a notification event is published THEN the `Type` is `"notification"` and the `Payload` contains notification dispatch fields.

---

## 8. Cross-Cutting Requirements

These requirements apply to ALL four services and define the shared patterns, conventions, and infrastructure concerns.

---

### REQ-086: Clean Architecture Layer Structure

**User Story:** As a developer, I want each service to follow Clean Architecture so that the codebase is maintainable and testable.

**Acceptance Criteria:**

- WHEN a service is structured THEN it has four projects: `{Service}.Domain`, `{Service}.Application`, `{Service}.Infrastructure`, `{Service}.Api`.
- WHEN the Domain layer is built THEN it has zero `ProjectReference` entries and zero ASP.NET Core or EF Core package references.
- WHEN the Application layer is built THEN it references only `{Service}.Domain` and contains no infrastructure packages.
- WHEN the Infrastructure layer is built THEN it references `{Service}.Domain` and `{Service}.Application`.
- WHEN the Api layer is built THEN it references `{Service}.Application` and `{Service}.Infrastructure` and serves as the composition root.

---

### REQ-087: Organization Isolation (Global Query Filters)

**User Story:** As the platform, I want all database queries to be automatically scoped to the current organization so that data isolation is enforced at the database level.

**Acceptance Criteria:**

- WHEN EF Core queries are executed THEN global query filters automatically scope all queries by `OrganizationId`.
- WHEN an entity implements `IOrganizationEntity` THEN the global query filter is applied.
- WHEN `OrganizationScopeMiddleware` processes a request THEN it extracts `organizationId` from JWT claims and stores it in `HttpContext.Items["OrganizationId"]`.
- WHEN inter-service calls are made THEN the `X-Organization-Id` header is propagated via `CorrelationIdDelegatingHandler`.

---

### REQ-088: Standardized Error Handling

**User Story:** As a developer, I want all services to handle errors consistently so that clients receive predictable error responses.

**Acceptance Criteria:**

- WHEN a `DomainException` is thrown THEN `GlobalExceptionHandlerMiddleware` catches it and returns an `ApiResponse<object>` with `application/problem+json` content type, the error's `ErrorCode`, `ErrorValue`, `Message`, and `CorrelationId`.
- WHEN an unhandled exception is thrown THEN the middleware returns HTTP 500 with `ErrorCode = "INTERNAL_ERROR"`, `Message = "An unexpected error occurred."`, and `CorrelationId`. No stack traces or internals are leaked.
- WHEN a `RateLimitExceededException` is thrown THEN the middleware adds a `Retry-After` header to the response.
- WHEN any error response is returned THEN it includes the `CorrelationId` from `HttpContext.Items["CorrelationId"]`.

---

### REQ-089: ApiResponse\<T\> Envelope

**User Story:** As a developer, I want all API responses wrapped in a standardized envelope so that clients can parse responses consistently.

**Acceptance Criteria:**

- WHEN any endpoint returns a response THEN it is wrapped in `ApiResponse<T>` with fields: `ResponseCode`, `ResponseDescription`, `Success`, `Data`, `ErrorCode`, `ErrorValue`, `Message`, `CorrelationId`, `Errors`.
- WHEN a successful response is returned THEN `ResponseCode = "00"`, `Success = true`, and `Data` contains the payload.
- WHEN a validation error occurs THEN `ResponseCode = "96"`, `ErrorCode = "VALIDATION_ERROR"`, and `Errors` contains per-field error details.
- WHEN a domain error occurs THEN `ResponseCode` is mapped from the error code category (e.g., "01" for credentials, "03" for permissions, "07" for not found).

---

### REQ-090: FluentValidation Pipeline

**User Story:** As a developer, I want automatic request validation so that invalid data is rejected before reaching business logic.

**Acceptance Criteria:**

- WHEN a request DTO has a corresponding FluentValidation validator THEN it is auto-discovered and executed before the controller action.
- WHEN validation fails THEN the system returns HTTP 422 with `ErrorCode = "VALIDATION_ERROR"`, `ErrorValue = 1000`, and per-field errors in the `Errors` array as `{ field, message }` objects.
- WHEN ASP.NET Core's built-in `ModelStateInvalidFilter` is configured THEN it is disabled via `SuppressModelStateInvalidFilter = true` to let FluentValidation handle all validation.

---

### REQ-091: Typed Service Clients with Polly Resilience

**User Story:** As a developer, I want typed service clients with resilience policies so that inter-service communication is reliable and type-safe.

**Acceptance Criteria:**

- WHEN a service communicates with another service THEN it uses a typed service client interface (e.g., `IProfileServiceClient`, `ISecurityServiceClient`).
- WHEN the typed client makes an HTTP call THEN Polly resilience policies are applied: 3 retries with exponential backoff (1s, 2s, 4s), circuit breaker (5 failures → 30s open), 10s timeout per request.
- WHEN a downstream service returns 4xx/5xx THEN the client attempts to deserialize the response as `ApiResponse<object>`. If successful, it throws `DomainException` with the downstream error code. If deserialization fails, it throws `DomainException` with `SERVICE_UNAVAILABLE`.
- WHEN the circuit breaker opens THEN the client throws `DomainException` with `SERVICE_UNAVAILABLE`.
- WHEN a downstream call fails THEN the client logs at Warning level with structured properties: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

---

### REQ-092: CorrelationId Propagation

**User Story:** As a developer, I want end-to-end request tracing so that I can debug issues across services.

**Acceptance Criteria:**

- WHEN a request enters any service THEN `CorrelationIdMiddleware` extracts `X-Correlation-Id` from the request header or generates a new GUID.
- WHEN the correlation ID is established THEN it is stored in `HttpContext.Items["CorrelationId"]` and added to the response header.
- WHEN an inter-service call is made THEN `CorrelationIdDelegatingHandler` attaches the `X-Correlation-Id` header to the outgoing request.
- WHEN any error response is returned THEN the `CorrelationId` is included in the `ApiResponse` body.

---

### REQ-093: Redis Outbox Pattern

**User Story:** As the platform, I want async event processing via Redis outbox so that audit logging and notifications don't block API responses.

**Acceptance Criteria:**

- WHEN a service needs to publish an audit event or notification THEN it calls `IOutboxService.PublishAsync(queueKey, serializedMessage)` which pushes to the service's Redis outbox queue.
- WHEN UtilityService's `OutboxProcessorHostedService` polls THEN it reads from all outbox queues (`outbox:security`, `outbox:profile`, `outbox:work`) and routes messages to the appropriate handler.
- WHEN outbox processing fails THEN the message is re-queued with an incremented retry counter. After 3 retries, it is moved to a dead-letter queue (`dlq:{service}`).

---

### REQ-094: Database Migrations (Auto-Apply)

**User Story:** As a developer, I want database migrations to auto-apply on startup so that deployment is simplified.

**Acceptance Criteria:**

- WHEN a service starts THEN `DatabaseMigrationHelper.ApplyMigrations(app)` checks for pending EF Core migrations and applies them.
- WHEN the database is InMemory (test environment) THEN `EnsureCreated()` is called instead of `Migrate()`.
- WHEN no pending migrations exist THEN the startup proceeds without database changes.

---

### REQ-095: Health Checks

**User Story:** As a DevOps engineer, I want health check endpoints so that I can monitor service availability.

**Acceptance Criteria:**

- WHEN `GET /health` is called THEN the service returns HTTP 200 if the process is running (liveness probe).
- WHEN `GET /ready` is called THEN the service checks database connectivity and Redis connection and returns HTTP 200 if both are healthy (readiness probe).

---

### REQ-096: Pagination

**User Story:** As a developer, I want consistent pagination across all list endpoints so that large datasets are handled efficiently.

**Acceptance Criteria:**

- WHEN any list endpoint is called THEN it supports `page` (default 1) and `pageSize` (default 20, max 100) query parameters.
- WHEN the response is paginated THEN it includes: `TotalCount`, `Page`, `PageSize`, `TotalPages`, and the `Data` array.
- WHEN `pageSize` exceeds 100 THEN it is capped at 100.

---

### REQ-097: Soft Delete Pattern

**User Story:** As the platform, I want soft deletes so that data is never permanently lost and can be recovered if needed.

**Acceptance Criteria:**

- WHEN an entity is "deleted" THEN its `FlgStatus` is set to `D` (Deactivated/Deleted) instead of being physically removed.
- WHEN entities are queried THEN EF Core global query filters exclude entities with `FlgStatus = 'D'` by default.
- WHEN an admin needs to see deleted entities THEN the query filter can be bypassed with `.IgnoreQueryFilters()`.

---

### REQ-098: Structured Logging

**User Story:** As a developer, I want structured logging so that logs are searchable and correlatable.

**Acceptance Criteria:**

- WHEN a `DomainException` is logged THEN the log entry includes: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName`, `RequestPath`.
- WHEN an unhandled exception is logged THEN the log entry includes: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`.
- WHEN a downstream call fails THEN the log entry includes: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

---

### REQ-099: Service-to-Service JWT Token Management

**User Story:** As a developer, I want automatic service JWT management in typed clients so that inter-service auth is seamless.

**Acceptance Criteria:**

- WHEN a typed service client makes a call THEN it automatically attaches a service JWT via `Authorization: Bearer {token}`.
- WHEN the cached service token is within 30 seconds of expiry THEN the client automatically refreshes it by calling SecurityService.
- WHEN the `X-Organization-Id` header is available in the current request context THEN it is propagated to the downstream call.

---

### REQ-100: API Versioning

**User Story:** As a developer, I want API versioning so that breaking changes can be introduced without affecting existing clients.

**Acceptance Criteria:**

- WHEN any endpoint is defined THEN it uses URL path versioning: `/api/v1/...`.
- WHEN a new version is needed THEN it is added as `/api/v2/...` without removing the v1 endpoints.

---

### REQ-101: Configuration via Environment Variables

**User Story:** As a DevOps engineer, I want all configuration via environment variables so that services are 12-factor compliant.

**Acceptance Criteria:**

- WHEN a service starts THEN it loads configuration from a `.env` file via `DotNetEnv` and populates an `AppSettings` singleton.
- WHEN a required environment variable is missing THEN the service throws `InvalidOperationException` at startup with a clear message.
- WHEN optional environment variables are missing THEN sensible defaults are used.

---

### REQ-102: CORS Configuration

**User Story:** As a developer, I want CORS configured so that the frontend can communicate with backend services.

**Acceptance Criteria:**

- WHEN a service starts THEN CORS is configured with allowed origins from the `ALLOWED_ORIGINS` environment variable (comma-separated).
- WHEN a preflight request is received THEN the service responds with appropriate CORS headers.

---

### REQ-103: Swagger Documentation

**User Story:** As a developer, I want Swagger UI so that I can explore and test API endpoints.

**Acceptance Criteria:**

- WHEN a service is running in Development mode THEN Swagger UI is available at `http://localhost:{port}/swagger`.
- WHEN Swagger is configured THEN it includes JWT Bearer authentication support for testing authenticated endpoints.

---

### REQ-104: Professional ID Generation Service

**User Story:** As the platform, I want a collision-free professional ID generation system so that stories and team members have unique, human-readable identifiers.

**Acceptance Criteria:**

- WHEN a story ID is generated THEN it uses the `IStoryIdGenerator` interface with atomic PostgreSQL sequence increment, ensuring collision-free IDs under concurrent creation.
- WHEN a team member professional ID is generated THEN it follows the format `NXS-{DeptCode}-{SequentialNumber}` and is unique within the organization.
- WHEN IDs are generated THEN they are sequential within their scope (per-organization for stories, per-department for team members).

---

### REQ-105: Inter-Service Communication Map

**User Story:** As a developer, I want a clear map of inter-service dependencies so that I understand the communication topology.

**Acceptance Criteria:**

- WHEN services communicate THEN they follow this dependency map:

| Caller | Callee | Purpose |
|--------|--------|---------|
| SecurityService | ProfileService | User identity resolution (`GET /team-members/by-email/{email}`), password update (`PATCH /team-members/{id}/password`) |
| ProfileService | SecurityService | Credential generation (`POST /auth/credentials/generate`) |
| WorkService | ProfileService | Organization settings, team member lookup, department member lists |
| WorkService | SecurityService | Service token issuance |
| SecurityService | UtilityService | Via outbox (audit events, notifications) |
| ProfileService | UtilityService | Via outbox (audit events, notifications) |
| WorkService | UtilityService | Via outbox (audit events, notifications) |

---

## 9. Preference & Settings Requirements

---

### REQ-106: Department Preferences

**User Story:** As a DeptLead, I want to configure department-specific preferences so that my team's workflow is optimized.

**Acceptance Criteria:**

- WHEN `GET /api/v1/departments/{id}/preferences` is called THEN the system returns the department's preferences.
- WHEN `PUT /api/v1/departments/{id}/preferences` is called by OrgAdmin or DeptLead (own dept) THEN the system updates the department's preferences.
- WHEN department preferences are updated THEN the Redis cache `dept_prefs:{departmentId}` is invalidated.
- WHEN a department preference is not set THEN the organization-level default is used (cascading: Department → Organization → System default).

**Department Preference Fields:**

- `DefaultTaskTypes` — array of task type codes relevant to the department (e.g., `["Development", "Testing"]`)
- `CustomWorkflowOverrides` — JSON object defining custom status names and transitions for the department
- `WipLimitPerStatus` — JSON object mapping status columns to WIP limits (e.g., `{"InProgress": 5, "InReview": 3}`)
- `DefaultAssigneeId` — `Guid?` — default assignee for unassigned tasks in this department
- `NotificationChannelOverrides` — JSON object defining department-specific notification channel defaults (overrides org-level `DefaultNotificationChannels`)
- `MaxConcurrentTasksDefault` — int — default `MaxConcurrentTasks` for new members added to this department, default `5`

**Storage:** Stored as a JSON column `PreferencesJson` on the Department entity, with a typed `DepartmentPreferences` class for deserialization. Cached in Redis `dept_prefs:{departmentId}` with 30-min TTL.

---

### REQ-107: User Preferences

**User Story:** As a team member, I want to configure my personal preferences so that the platform adapts to my workflow.

**Acceptance Criteria:**

- WHEN `GET /api/v1/preferences` is called THEN the system returns the authenticated user's preferences.
- WHEN `PUT /api/v1/preferences` is called THEN the system updates the authenticated user's preferences.
- WHEN user preferences are updated THEN the Redis cache `user_prefs:{userId}` is invalidated.
- WHEN a user preference is not set THEN the organization-level default is used (cascading: User → Organization → System default).

**User Preference Fields:**

- `Theme` — enum: `Light`, `Dark`, `System` — default `System`
- `Language` — string, ISO 639-1 code — default `"en"`
- `TimezoneOverride` — IANA timezone string, nullable — overrides organization `TimeZone`
- `DefaultBoardView` — enum: `Kanban`, `Sprint`, `Backlog` — overrides organization `DefaultBoardView`
- `DefaultBoardFilters` — JSON object defining saved default filters for board views (e.g., `{"showOnlyMyTasks": true}`)
- `DashboardLayout` — JSON object defining which widgets to show and their order
- `EmailDigestFrequency` — enum: `Realtime`, `Hourly`, `Daily`, `Off` — overrides organization `DigestFrequency`
- `KeyboardShortcutsEnabled` — bool, default `true`
- `DateFormat` — enum: `ISO`, `US`, `EU` — default `ISO`
- `TimeFormat` — enum: `H24`, `H12` — default `H24`

**Storage:** Stored as a `UserPreferences` entity with typed fields, linked to `TeamMemberId`. Cached in Redis `user_prefs:{userId}` with 15-min TTL.

---

### REQ-108: Preference Cascade Resolution

**User Story:** As the platform, I want preferences to cascade from system defaults through organization and department to user level so that configuration is flexible but consistent.

**Acceptance Criteria:**

- WHEN a preference value is needed THEN the system resolves it in this order: User → Department → Organization → System Default.
- WHEN `GET /api/v1/preferences/resolved` is called THEN the system returns the fully resolved preferences for the authenticated user, merging all levels.
- WHEN any level's preferences are updated THEN only that level's cache is invalidated — downstream resolution happens at read time.
- WHEN the resolved preferences are computed THEN the result is cached in Redis `resolved_prefs:{userId}` with 5-min TTL.
- WHEN any upstream preference level (org, dept) is updated THEN the `resolved_prefs:{userId}` cache for affected users is NOT proactively invalidated — it expires naturally via the short 5-min TTL.

---

## Appendix A: Key Workflows

### A.1 Organization Setup → First Member Login

1. OrgAdmin creates organization via ProfileService → 5 default departments seeded
2. OrgAdmin configures story ID prefix (e.g., `NEXUS`)
3. OrgAdmin creates invite for first team member
4. UtilityService dispatches invite email
5. New member validates and accepts invite → TeamMember created
6. ProfileService calls SecurityService to generate credentials
7. New member logs in with temporary password → `isFirstTimeUser: true`
8. New member forced to change password via `POST /api/v1/password/forced-change`

### A.2 Story Creation → Task Breakdown → Sprint Delivery

1. Member creates story → Professional ID generated (e.g., `NEXUS-42`)
2. Member creates tasks within story → Auto-mapped to departments (Development → ENG, Testing → QA)
3. System suggests assignees based on department, availability, and workload
4. DeptLead creates sprint and adds stories during Planning phase
5. DeptLead starts sprint (only one active per org)
6. Team works through task workflow: ToDo → InProgress → InReview → Done
7. Story progresses through workflow: Backlog → Ready → InProgress → InReview → QA → Done → Closed
8. Sprint completed → Velocity calculated, incomplete stories moved to backlog
9. All state changes generate activity logs and notifications via outbox

### A.3 Cross-Department Collaboration

1. Story `NEXUS-42` created by Product team
2. Development task created → Auto-assigned to Engineering department
3. Testing task created → Auto-assigned to QA department
4. Design task created → Auto-assigned to Design department
5. Each department works on their tasks independently
6. Story detail shows department contribution breakdown
7. When all tasks complete, story is eligible for Done transition
8. Department board shows workload distribution across all departments

---

## Appendix B: Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8 / ASP.NET Core 8 |
| ORM | Entity Framework Core (PostgreSQL via Npgsql) |
| Cache / Sessions | Redis (StackExchange.Redis) |
| Validation | FluentValidation (auto-validation pipeline) |
| Resilience | Polly (retry, circuit breaker, timeout) |
| Authentication | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Password Hashing | BCrypt.Net-Next |
| API Documentation | Swagger / Swashbuckle |
| Testing | xUnit, FsCheck (property-based), Moq |
| Environment Config | DotNetEnv |
| API Versioning | URL path versioning (`/api/v1/...`) |

---

## Appendix C: BillingService (Implemented)

> **Status:** Implemented — BillingService is built and operational. The documentation below serves as a technical reference for the service's architecture, data model, and API surface.

### Overview

BillingService handles subscription management, plan tiers, feature gating, and usage tracking for the Nexus-2.0 platform. It enables a SaaS model where organizations subscribe to different plan tiers with varying feature access and capacity limits.

**Port:** 5300
**Database:** `nexus_billing`

### Service Decomposition Update

| Service | Port | Database | Responsibility |
|---------|------|----------|----------------|
| SecurityService.API | 5001 | `nexus_security` | Authentication, JWT, sessions, RBAC |
| ProfileService.API | 5002 | `nexus_profile` | Organizations, departments, team members |
| WorkService.API | 5003 | `nexus_work` | Stories, tasks, sprints, boards |
| UtilityService.API | 5200 | `nexus_utility` | Audit logs, notifications, reference data |
| **BillingService.API** | **5300** | **`nexus_billing`** | **Subscriptions, plans, feature gates, usage, invoices** |

### Capabilities

- **Plan Management** — Define plan tiers (Free, Starter, Professional, Enterprise) with feature limits
- **Subscription Lifecycle** — Create, upgrade, downgrade, cancel subscriptions per organization
- **Feature Gating** — Enforce plan-tier limits on features (max team members, max departments, max stories, sprint analytics, etc.)
- **Usage Tracking** — Track organization usage metrics (active members, stories created, storage used) for billing and limit enforcement
- **Payment Integration** — Integrate with Stripe (or similar) for payment processing, invoicing, and webhook handling
- **Trial Management** — Support free trial periods with automatic conversion or expiry

### Plan Tiers

| Tier | Max Members | Max Departments | Sprint Analytics | Custom Workflows | Priority Support |
|------|-------------|-----------------|------------------|------------------|-----------------|
| Free | 5 | 3 (defaults only) | ❌ | ❌ | ❌ |
| Starter | 25 | 5 | Basic | ❌ | ❌ |
| Professional | 100 | Unlimited | Full | ✅ | ✅ |
| Enterprise | Unlimited | Unlimited | Full + Custom | ✅ | ✅ (SLA) |

### Integration Points with Existing Services

| Integration | Description |
|-------------|-------------|
| **ProfileService** | `OrganizationSettings.PlanTier` field (added to SettingsJson). BillingService updates plan tier on subscription changes. ProfileService checks member/department limits against plan tier. |
| **SecurityService** | `FeatureGateMiddleware` (new middleware, slotted after `OrganizationScopeMiddleware`). Reads plan tier from cached org settings and enforces feature access. JWT claims remain unchanged — plan tier is resolved at the org level, not the user level. |
| **WorkService** | Checks plan-tier limits on story creation, sprint analytics access, custom workflow features. Calls BillingService or reads cached plan info from Redis. |
| **UtilityService** | BillingService publishes billing events to `outbox:billing` for audit logging. UtilityService processes billing audit events alongside other service events. |

### Data Model

**Subscription**

| Field | Type | Description |
|-------|------|-------------|
| `SubscriptionId` | `Guid` (PK) | Primary key |
| `OrganizationId` | `Guid` (FK, unique) | One subscription per organization |
| `PlanId` | `Guid` (FK) | Current plan |
| `Status` | `string` | `Active`, `Trialing`, `PastDue`, `Cancelled`, `Expired` |
| `ExternalSubscriptionId` | `string?` | Stripe subscription ID |
| `CurrentPeriodStart` | `DateTime` | Billing period start |
| `CurrentPeriodEnd` | `DateTime` | Billing period end |
| `TrialEndDate` | `DateTime?` | Trial expiry |
| `CancelledAt` | `DateTime?` | Cancellation timestamp |
| `DateCreated` | `DateTime` | Creation timestamp |

**Plan**

| Field | Type | Description |
|-------|------|-------------|
| `PlanId` | `Guid` (PK) | Primary key |
| `PlanName` | `string` | Free, Starter, Professional, Enterprise |
| `PlanCode` | `string` | `free`, `starter`, `pro`, `enterprise` |
| `MaxTeamMembers` | `int` | 0 = unlimited |
| `MaxDepartments` | `int` | 0 = unlimited |
| `MaxStoriesPerMonth` | `int` | 0 = unlimited |
| `FeaturesJson` | `string` (JSON) | Feature flags as JSON |
| `PriceMonthly` | `decimal` | Monthly price |
| `PriceYearly` | `decimal` | Yearly price |
| `IsActive` | `bool` | Whether plan is available |

**UsageRecord**

| Field | Type | Description |
|-------|------|-------------|
| `UsageRecordId` | `Guid` (PK) | Primary key |
| `OrganizationId` | `Guid` (FK) | Organization reference |
| `MetricName` | `string` | `active_members`, `stories_created`, `storage_bytes` |
| `MetricValue` | `long` | Current value |
| `PeriodStart` | `DateTime` | Tracking period start |
| `PeriodEnd` | `DateTime` | Tracking period end |
| `DateUpdated` | `DateTime` | Last update |

### API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/v1/subscriptions/current` | Bearer (OrgAdmin) | Get current subscription |
| POST | `/api/v1/subscriptions` | Bearer (OrgAdmin) | Create subscription |
| PATCH | `/api/v1/subscriptions/upgrade` | Bearer (OrgAdmin) | Upgrade plan |
| PATCH | `/api/v1/subscriptions/downgrade` | Bearer (OrgAdmin) | Downgrade plan |
| POST | `/api/v1/subscriptions/cancel` | Bearer (OrgAdmin) | Cancel subscription |
| GET | `/api/v1/plans` | Bearer | List available plans |
| GET | `/api/v1/usage` | Bearer (OrgAdmin) | Get usage metrics |
| POST | `/api/v1/webhooks/stripe` | Webhook | Stripe webhook handler |
| GET | `/api/v1/feature-gates/{feature}` | Service | Check feature access for org |
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

### Redis Key Patterns

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `plan:{organizationId}` | Cached plan tier and feature flags | 60 min |
| `usage:{organizationId}:{metric}` | Cached usage counters | 5 min |
| `outbox:billing` | Outbox queue for billing audit events | Until processed |

### Architecture Notes

- Follows the same Clean Architecture pattern as all other services (Domain/Application/Infrastructure/Api)
- Uses the same cross-cutting patterns (ApiResponse envelope, DomainException, FluentValidation, Polly, outbox, health checks)
- Feature gate checks should be cached aggressively (plan changes are infrequent)
- Stripe webhooks handle async payment events (payment succeeded, failed, subscription updated)
- The `FeatureGateMiddleware` for SecurityService/WorkService reads from `plan:{organizationId}` Redis cache, falling back to BillingService API on cache miss

---

## Appendix D: Frontend Application (Planned)

> **Status:** Planned — to be built after backend services are operational. Documented here to capture the frontend vision and its integration with backend services.

### Overview

The Nexus-2.0 frontend is a single-page application (SPA) built with React, TypeScript, and Vite. It consumes all 4 backend microservices via their REST APIs and provides the user interface for the entire Agile platform — from authentication through story management, sprint planning, and board views.

### Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | React 18+ |
| Language | TypeScript |
| Build Tool | Vite |
| Routing | React Router |
| State Management | Zustand or React Query (TanStack Query) |
| UI Components | Shadcn/ui or Ant Design |
| HTTP Client | Axios (with interceptors for JWT refresh) |
| Forms | React Hook Form + Zod validation |
| Drag & Drop | dnd-kit (for Kanban boards) |
| Charts | Recharts (burndown, velocity) |
| Testing | Vitest + React Testing Library |

### Project Structure

```
src/frontend/
├── public/
├── src/
│   ├── api/                    # Typed API clients per backend service
│   │   ├── securityApi.ts      # Auth, sessions, OTP, password
│   │   ├── profileApi.ts       # Orgs, departments, members, invites
│   │   ├── workApi.ts          # Stories, tasks, sprints, boards, search
│   │   └── utilityApi.ts       # Notifications, reference data
│   ├── components/             # Shared/reusable UI components
│   │   ├── layout/             # AppShell, Sidebar, Header, Footer
│   │   ├── common/             # Button, Modal, Table, Pagination, Badge
│   │   └── forms/              # FormField, PasswordInput, OtpInput
│   ├── features/               # Feature-based modules
│   │   ├── auth/               # Login, ForcedPasswordChange, PasswordReset, OTP
│   │   ├── dashboard/          # Home dashboard, widgets
│   │   ├── stories/            # Story list, detail, create/edit, workflow
│   │   ├── tasks/              # Task list, detail, assignment
│   │   ├── sprints/            # Sprint planning, sprint board, metrics
│   │   ├── boards/             # Kanban, Sprint Board, Department Board, Backlog
│   │   ├── members/            # Team member list, profile, department assignment
│   │   ├── departments/        # Department management, member lists
│   │   ├── settings/           # Org settings, user preferences, notification prefs
│   │   ├── invites/            # Invite management, accept invite flow
│   │   └── search/             # Global search
│   ├── hooks/                  # Custom React hooks (useAuth, useOrg, useDebounce)
│   ├── stores/                 # Global state (auth store, org store, theme store)
│   ├── types/                  # Shared TypeScript types/interfaces
│   ├── utils/                  # Helpers (date formatting, story key parsing)
│   ├── App.tsx
│   └── main.tsx
├── package.json
├── tsconfig.json
├── vite.config.ts
└── .env.example
```

### Feature Modules → Backend Service Mapping

| Frontend Feature | Primary Backend Service | Key Endpoints |
|-----------------|------------------------|---------------|
| Auth (login, logout, OTP, password) | SecurityService | `/api/v1/auth/*`, `/api/v1/password/*`, `/api/v1/sessions/*` |
| Dashboard | WorkService + ProfileService | Sprint metrics, recent activity, my tasks |
| Stories | WorkService | `/api/v1/stories/*`, `/api/v1/stories/by-key/*` |
| Tasks | WorkService | `/api/v1/tasks/*` |
| Sprints | WorkService | `/api/v1/sprints/*` |
| Boards (Kanban, Sprint, Dept) | WorkService | `/api/v1/boards/*` |
| Team Members | ProfileService | `/api/v1/team-members/*` |
| Departments | ProfileService | `/api/v1/departments/*` |
| Organization Settings | ProfileService | `/api/v1/organizations/*`, `/api/v1/organizations/{id}/settings` |
| User Preferences | ProfileService | `/api/v1/preferences/*` |
| Invites | ProfileService | `/api/v1/invites/*` |
| Notifications | UtilityService + ProfileService | `/api/v1/notification-settings/*` |
| Search | WorkService | `/api/v1/search/*` |

### Authentication Flow (Frontend)

1. User enters email/password on Login page
2. Frontend calls `POST /api/v1/auth/login` → receives `accessToken`, `refreshToken`, `isFirstTimeUser`
3. If `isFirstTimeUser=true` → redirect to Forced Password Change page
4. Store tokens in memory (access) and httpOnly cookie or secure storage (refresh)
5. Axios interceptor attaches `Authorization: Bearer {accessToken}` to all requests
6. On 401 response → interceptor calls `POST /api/v1/auth/refresh` with refresh token
7. If refresh fails (expired/reuse) → redirect to Login page
8. Logout calls `POST /api/v1/auth/logout` and clears local state

### Key UI Views

- **Login** — Email/password form, OTP verification modal, "Forgot Password" link
- **Dashboard** — Sprint progress widget, my tasks widget, recent activity feed, velocity chart
- **Story Board (Kanban)** — Drag-and-drop columns by status (Backlog → Ready → InProgress → InReview → QA → Done → Closed)
- **Sprint Board** — Stories grouped by sprint with progress bars
- **Department Board** — Tasks grouped by department with workload indicators
- **Story Detail** — Full story view with tasks, comments (@mentions), labels, activity log, workflow state machine visualization
- **Sprint Planning** — Drag stories from backlog into sprint, capacity indicators
- **Settings** — Organization settings, department preferences, user preferences with cascade preview

### Planned Pages

| Page | Route | Auth Required | Description |
|------|-------|---------------|-------------|
| Login | `/login` | No | Email/password login |
| Forced Password Change | `/password/change` | Yes (first-time) | New password form |
| Password Reset | `/password/reset` | No | OTP-based reset flow |
| Dashboard | `/` | Yes | Home dashboard |
| Stories | `/stories` | Yes | Story list with filters |
| Story Detail | `/stories/:id` | Yes | Full story view |
| Story by Key | `/stories/key/:key` | Yes | Redirect via professional ID (e.g., NEXUS-42) |
| Kanban Board | `/boards/kanban` | Yes | Drag-and-drop kanban |
| Sprint Board | `/boards/sprint` | Yes | Sprint-grouped view |
| Department Board | `/boards/department` | Yes | Department workload view |
| Backlog | `/boards/backlog` | Yes | Prioritized backlog |
| Sprints | `/sprints` | Yes | Sprint list and planning |
| Sprint Detail | `/sprints/:id` | Yes | Sprint metrics, burndown |
| Team Members | `/members` | Yes | Member directory |
| Member Profile | `/members/:id` | Yes | Member detail |
| Departments | `/departments` | Yes | Department list |
| Department Detail | `/departments/:id` | Yes | Department members and tasks |
| Settings | `/settings` | Yes (OrgAdmin) | Organization settings |
| Preferences | `/preferences` | Yes | User preferences |
| Invites | `/invites` | Yes (OrgAdmin/DeptLead) | Manage invites |
| Accept Invite | `/invites/:token` | No | Accept invite flow |
| Search | `/search` | Yes | Global search results |
| Sessions | `/sessions` | Yes | Active session management |

---

*End of Requirements Document*
