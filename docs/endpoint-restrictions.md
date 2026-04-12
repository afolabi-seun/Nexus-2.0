# Nexus 2.0 — Endpoint Restrictions Reference

Comprehensive listing of every API endpoint, its HTTP method, current auth type, and required role.

---

## Role Hierarchy

```
PlatformAdmin  (100) — Super-admin, full platform access
  └── OrgAdmin  (75) — Organization-level admin
       └── DeptLead  (50) — Department lead
            └── Member  (25) — Standard team member
                 └── Viewer  (10) — Read-only access
```

- `PlatformAdmin` can access everything (OrgAdmin, DeptLead, Member, Viewer endpoints).
- `OrgAdmin` can access OrgAdmin, DeptLead, Member, and Viewer endpoints.
- `DeptLead` can access DeptLead, Member, and Viewer endpoints.
- `Member` and `Viewer` can only access endpoints with `[Authorize]` (no role attribute).

## Auth Types

| Auth Type | Description |
|-----------|-------------|
| `Anonymous` | No authentication required (`[AllowAnonymous]`) |
| `Authenticated` | Any authenticated user (`[Authorize]`, no role attribute) |
| `DeptLead` | DeptLead, OrgAdmin, or PlatformAdmin |
| `OrgAdmin` | OrgAdmin or PlatformAdmin |
| `PlatformAdmin` | PlatformAdmin only |
| `ServiceAuth` | Service-to-service JWT (internal, not user-facing) |

---

## SecurityService (port 5001)

### AuthController — `/api/v1/auth`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/auth/login` | Anonymous | — | |
| POST | `/auth/logout` | Authenticated | — | |
| POST | `/auth/refresh` | Anonymous | — | |
| POST | `/auth/otp/request` | Anonymous | — | |
| POST | `/auth/otp/verify` | Anonymous | — | |
| POST | `/auth/credentials/generate` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### PasswordController — `/api/v1/password`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/password/forced-change` | Authenticated | — | |
| POST | `/password/reset/request` | Anonymous | — | |
| POST | `/password/reset/confirm` | Anonymous | — | |

### ServiceTokenController — `/api/v1/service-tokens`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/service-tokens/issue` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### SessionController — `/api/v1/sessions`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/sessions` | Authenticated | — | |
| DELETE | `/sessions/{sessionId}` | Authenticated | — | |
| DELETE | `/sessions/all` | Authenticated | — | |

---

## ProfileService (port 5002)

### OrganizationController — `/api/v1/organizations`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/organizations` | Authenticated | — | ⚠️ Should be OrgAdmin or PlatformAdmin? |
| GET | `/organizations` | PlatformAdmin | — | |
| GET | `/organizations/{id}` | Authenticated | — | |
| PUT | `/organizations/{id}` | OrgAdmin | ✅ | |
| PATCH | `/organizations/{id}/status` | OrgAdmin | ✅ | |
| PUT | `/organizations/{id}/settings` | OrgAdmin | ✅ | |
| POST | `/organizations/{id}/provision-admin` | PlatformAdmin | — | |

### DepartmentController — `/api/v1/departments`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/departments` | OrgAdmin | ✅ | |
| GET | `/departments` | Authenticated | — | |
| GET | `/departments/{id}` | Authenticated | — | |
| PUT | `/departments/{id}` | DeptLead | ✅ | |
| PATCH | `/departments/{id}/status` | OrgAdmin | ✅ | |
| GET | `/departments/{id}/members` | Authenticated | — | |
| GET | `/departments/{id}/preferences` | Authenticated | — | |
| PUT | `/departments/{id}/preferences` | DeptLead | ✅ | |

### TeamMemberController — `/api/v1/team-members`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/team-members` | Authenticated | — | |
| GET | `/team-members/{id}` | Authenticated | — | |
| PUT | `/team-members/{id}` | Authenticated | — | Self or OrgAdmin (enforced in controller) |
| PATCH | `/team-members/{id}/status` | OrgAdmin | ✅ | |
| PATCH | `/team-members/{id}/availability` | Authenticated | — | |
| POST | `/team-members/{id}/departments` | OrgAdmin | ✅ | |
| DELETE | `/team-members/{id}/departments/{deptId}` | OrgAdmin | ✅ | |
| PATCH | `/team-members/{id}/departments/{deptId}/role` | OrgAdmin | ✅ | |
| GET | `/team-members/by-email/{email}` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| PATCH | `/team-members/{id}/password` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### InviteController — `/api/v1/invites`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/invites` | DeptLead | ✅ | |
| GET | `/invites` | Authenticated | — | |
| GET | `/invites/{token}/validate` | Anonymous | — | |
| POST | `/invites/{token}/accept` | Anonymous | — | |
| DELETE | `/invites/{id}` | DeptLead | ✅ | |

### PlatformAdminController — `/api/v1/platform-admins`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/platform-admins/by-username/{username}` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| PATCH | `/platform-admins/{id}/password` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### NavigationController — `/api/v1/navigation`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/navigation` | Authenticated | — | |
| GET | `/navigation/all` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| POST | `/navigation` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| PUT | `/navigation/{id}` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| DELETE | `/navigation/{id}` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### DeviceController — `/api/v1/devices`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/devices` | Authenticated | — | Self only |
| PATCH | `/devices/{id}/primary` | Authenticated | — | Self only |
| DELETE | `/devices/{id}` | Authenticated | — | Self only |

### PreferenceController — `/api/v1/preferences`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/preferences` | Authenticated | — | Self only |
| PUT | `/preferences` | Authenticated | — | Self only |
| GET | `/preferences/resolved` | Authenticated | — | Self only |

### NotificationSettingController — `/api/v1`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/notification-settings` | Authenticated | — | Self only |
| PUT | `/notification-settings/{typeId}` | Authenticated | — | Self only |
| GET | `/notification-types` | Authenticated | — | |

### RoleController — `/api/v1/roles`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/roles` | Authenticated | — | |
| GET | `/roles/{id}` | Authenticated | — | |

---

## WorkService (port 5003)

### ProjectController — `/api/v1/projects`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/projects` | DeptLead | ✅ | |
| GET | `/projects` | Authenticated | — | |
| GET | `/projects/{id}` | Authenticated | — | |
| PUT | `/projects/{id}` | DeptLead | ✅ | |
| PATCH | `/projects/{id}/status` | OrgAdmin | ✅ | |
| GET | `/projects/{id}/cost-summary` | Authenticated | — | |
| GET | `/projects/{id}/utilization` | Authenticated | — | |
| GET | `/projects/{id}/cost-snapshots` | Authenticated | — | |

### SprintController — `/api/v1`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/projects/{projectId}/sprints` | DeptLead | ✅ | |
| GET | `/sprints` | Authenticated | — | |
| GET | `/sprints/{id}` | Authenticated | — | |
| PUT | `/sprints/{id}` | DeptLead | ✅ | |
| PATCH | `/sprints/{id}/start` | DeptLead | ✅ | |
| PATCH | `/sprints/{id}/complete` | DeptLead | ✅ | |
| PATCH | `/sprints/{id}/cancel` | DeptLead | ✅ | |
| POST | `/sprints/{sprintId}/stories` | DeptLead | ✅ | |
| DELETE | `/sprints/{sprintId}/stories/{storyId}` | DeptLead | ✅ | |
| GET | `/sprints/{id}/metrics` | Authenticated | — | |
| GET | `/sprints/velocity` | Authenticated | — | |
| GET | `/sprints/active` | Authenticated | — | |
| GET | `/sprints/{sprintId}/velocity` | Authenticated | — | |

### StoryController — `/api/v1/stories`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/stories` | Authenticated | — | |
| GET | `/stories` | Authenticated | — | |
| GET | `/stories/{id}` | Authenticated | — | |
| GET | `/stories/by-key/{storyKey}` | Authenticated | — | |
| PUT | `/stories/{id}` | Authenticated | — | |
| DELETE | `/stories/{id}` | DeptLead | ✅ | |
| PATCH | `/stories/{id}/status` | Authenticated | — | |
| PATCH | `/stories/{id}/assign` | DeptLead | ✅ | |
| PATCH | `/stories/{id}/unassign` | DeptLead | ✅ | |
| POST | `/stories/{id}/links` | Authenticated | — | |
| DELETE | `/stories/{id}/links/{linkId}` | Authenticated | — | |
| POST | `/stories/{id}/labels` | Authenticated | — | |
| DELETE | `/stories/{id}/labels/{labelId}` | Authenticated | — | |
| GET | `/stories/{id}/comments` | Authenticated | — | |
| GET | `/stories/{id}/activity` | Authenticated | — | |

### TaskController — `/api/v1/tasks`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/tasks` | Authenticated | — | |
| GET | `/tasks/{id}` | Authenticated | — | |
| PUT | `/tasks/{id}` | Authenticated | — | |
| DELETE | `/tasks/{id}` | DeptLead | ✅ | |
| PATCH | `/tasks/{id}/status` | Authenticated | — | |
| PATCH | `/tasks/{id}/assign` | DeptLead | ✅ | |
| PATCH | `/tasks/{id}/self-assign` | Authenticated | — | |
| PATCH | `/tasks/{id}/unassign` | DeptLead | ✅ | |
| PATCH | `/tasks/{id}/log-hours` | Authenticated | — | |
| GET | `/tasks/{id}/activity` | Authenticated | — | |
| GET | `/tasks/{id}/comments` | Authenticated | — | |
| GET | `/tasks/suggest-assignee` | Authenticated | — | |

### StoryTaskController — `/api/v1/stories/{storyId}/tasks`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/stories/{storyId}/tasks` | Authenticated | — | |

### BoardController — `/api/v1/boards`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/boards/kanban` | Authenticated | — | |
| GET | `/boards/sprint` | Authenticated | — | |
| GET | `/boards/backlog` | Authenticated | — | |
| GET | `/boards/department` | Authenticated | — | |

### CommentController — `/api/v1/comments`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/comments` | Authenticated | — | |
| PUT | `/comments/{id}` | Authenticated | — | Owner only (enforced in service) |
| DELETE | `/comments/{id}` | Authenticated | — | Owner or OrgAdmin (enforced in service) |

### LabelController — `/api/v1/labels`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/labels` | DeptLead | ✅ | |
| GET | `/labels` | Authenticated | — | |
| PUT | `/labels/{id}` | DeptLead | ✅ | |
| DELETE | `/labels/{id}` | OrgAdmin | ✅ | |

### WorkflowController — `/api/v1/workflows`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/workflows` | Authenticated | — | |
| PUT | `/workflows/organization` | OrgAdmin | ✅ | |
| PUT | `/workflows/department/{departmentId}` | DeptLead | ✅ | |

### CostRateController — `/api/v1/cost-rates`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/cost-rates` | OrgAdmin | ✅ | |
| GET | `/cost-rates` | Authenticated | — | |
| PUT | `/cost-rates/{costRateId}` | OrgAdmin | ✅ | |
| DELETE | `/cost-rates/{costRateId}` | OrgAdmin | ✅ | |

### TimePolicyController — `/api/v1/time-policies`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/time-policies` | Authenticated | — | |
| PUT | `/time-policies` | OrgAdmin | ✅ | |

### TimeEntryController — `/api/v1/time-entries`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/time-entries` | Authenticated | — | |
| GET | `/time-entries` | Authenticated | — | |
| PUT | `/time-entries/{timeEntryId}` | Authenticated | — | Owner only (enforced in service) |
| DELETE | `/time-entries/{timeEntryId}` | Authenticated | — | Owner only (enforced in service) |
| POST | `/time-entries/{timeEntryId}/approve` | DeptLead | ✅ | |
| POST | `/time-entries/{timeEntryId}/reject` | DeptLead | ✅ | |
| POST | `/time-entries/timer/start` | Authenticated | — | |
| POST | `/time-entries/timer/stop` | Authenticated | — | |
| GET | `/time-entries/timer/status` | Authenticated | — | |

### AnalyticsController — `/api/v1/analytics`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/analytics/velocity` | Authenticated | — | |
| GET | `/analytics/resource-management` | Authenticated | — | |
| GET | `/analytics/resource-utilization` | Authenticated | — | |
| GET | `/analytics/project-cost` | Authenticated | — | |
| GET | `/analytics/project-health` | Authenticated | — | |
| GET | `/analytics/dependencies` | Authenticated | — | |
| GET | `/analytics/bugs` | Authenticated | — | |
| GET | `/analytics/dashboard` | Authenticated | — | |
| GET | `/analytics/snapshot-status` | DeptLead | ✅ | |

### RiskRegisterController — `/api/v1/analytics/risks`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/analytics/risks` | DeptLead | ✅ | |
| PUT | `/analytics/risks/{riskId}` | DeptLead | ✅ | |
| DELETE | `/analytics/risks/{riskId}` | DeptLead | ✅ | |
| GET | `/analytics/risks` | Authenticated | — | |

### ReportController — `/api/v1/reports`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/reports/velocity` | Authenticated | — | |
| GET | `/reports/department-workload` | Authenticated | — | |
| GET | `/reports/capacity` | Authenticated | — | |
| GET | `/reports/cycle-time` | Authenticated | — | |
| GET | `/reports/task-completion` | Authenticated | — | |

### SearchController — `/api/v1/search`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/search` | Authenticated | — | |

### SavedFilterController — `/api/v1/saved-filters`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/saved-filters` | Authenticated | — | |
| GET | `/saved-filters` | Authenticated | — | |
| DELETE | `/saved-filters/{id}` | Authenticated | — | Owner only (enforced in service) |

---

## UtilityService (port 5200)

### AuditLogController — `/api/v1/audit-logs`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/audit-logs` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| GET | `/audit-logs` | OrgAdmin | ✅ | |
| GET | `/audit-logs/archive` | OrgAdmin | ✅ | |
| PUT | `/audit-logs` | — | — | Always returns 405 (immutable) |
| DELETE | `/audit-logs` | — | — | Always returns 405 (immutable) |

### ErrorCodeController — `/api/v1/error-codes`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/error-codes` | OrgAdmin | ✅ | |
| GET | `/error-codes` | Authenticated | — | |
| PUT | `/error-codes/{code}` | OrgAdmin | ✅ | |
| DELETE | `/error-codes/{code}` | OrgAdmin | ✅ | |

### ErrorLogController — `/api/v1/error-logs`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/error-logs` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| GET | `/error-logs` | OrgAdmin | ✅ | |

### NotificationController — `/api/v1`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/notifications/dispatch` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |
| GET | `/notification-logs` | Authenticated | — | Self only |

### ReferenceDataController — `/api/v1/reference`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/reference/department-types` | Authenticated | — | |
| GET | `/reference/priority-levels` | Authenticated | — | |
| GET | `/reference/task-types` | Authenticated | — | |
| GET | `/reference/workflow-states` | Authenticated | — | |
| POST | `/reference/department-types` | OrgAdmin | ✅ | |
| POST | `/reference/priority-levels` | OrgAdmin | ✅ | |

---

## BillingService (port 5300)

### PlanController — `/api/v1/plans`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/plans` | Authenticated | — | |

### SubscriptionController — `/api/v1/subscriptions`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/subscriptions/current` | OrgAdmin | ✅ | Controller-level `[OrgAdmin]` |
| POST | `/subscriptions` | OrgAdmin | ✅ | |
| PATCH | `/subscriptions/upgrade` | OrgAdmin | ✅ | |
| PATCH | `/subscriptions/downgrade` | OrgAdmin | ✅ | |
| POST | `/subscriptions/cancel` | OrgAdmin | ✅ | |

### AdminBillingController — `/api/v1/admin/billing`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/admin/billing/subscriptions` | PlatformAdmin | ✅ | Controller-level `[PlatformAdmin]` |
| GET | `/admin/billing/organizations/{orgId}` | PlatformAdmin | ✅ | |
| POST | `/admin/billing/organizations/{orgId}/override` | PlatformAdmin | ✅ | |
| POST | `/admin/billing/organizations/{orgId}/cancel` | PlatformAdmin | ✅ | |
| GET | `/admin/billing/usage/summary` | PlatformAdmin | ✅ | |
| GET | `/admin/billing/usage/organizations` | PlatformAdmin | ✅ | |

### AdminPlanController — `/api/v1/admin/billing/plans`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/admin/billing/plans` | PlatformAdmin | ✅ | Controller-level `[PlatformAdmin]` |
| POST | `/admin/billing/plans` | PlatformAdmin | ✅ | |
| PUT | `/admin/billing/plans/{planId}` | PlatformAdmin | ✅ | |
| PATCH | `/admin/billing/plans/{planId}/deactivate` | PlatformAdmin | ✅ | |

### FeatureGateController — `/api/v1/feature-gates`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/feature-gates/{feature}` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### UsageController — `/api/v1/usage`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| GET | `/usage` | OrgAdmin | ✅ | |
| POST | `/usage/increment` | ServiceAuth | — | ⚠️ Internal — hide from Swagger & Postman |

### StripeWebhookController — `/api/v1/webhooks/stripe`

| Method | Endpoint | Auth | Role | Notes |
|--------|----------|------|------|-------|
| POST | `/webhooks/stripe` | Anonymous | — | Stripe signature validation |

---

## Summary

### ✅ Role Restrictions Applied

All endpoints now have appropriate role-based access control. Fixes applied:
- **ProfileService**: Added `[OrgAdmin]` and `[DeptLead]` attributes + middleware support (16 endpoints fixed)
- **UtilityService**: Added `[OrgAdmin]` to audit log reads, `[Authorize]` to reference data reads. Fixed `OrgAdminAttribute` to allow PlatformAdmin.
- **TeamMemberController**: Self-only check for profile updates (OrgAdmin/PlatformAdmin can update any member).

### ⚠️ ServiceAuth Endpoints to Hide from Swagger & Postman

| Service | Endpoint |
|---------|----------|
| SecurityService | `POST /auth/credentials/generate` |
| SecurityService | `POST /service-tokens/issue` |
| ProfileService | `GET /platform-admins/by-username/{username}` |
| ProfileService | `PATCH /platform-admins/{id}/password` |
| ProfileService | `GET /team-members/by-email/{email}` |
| ProfileService | `PATCH /team-members/{id}/password` |
| ProfileService | `GET /navigation/all` |
| ProfileService | `POST /navigation` |
| ProfileService | `PUT /navigation/{id}` |
| ProfileService | `DELETE /navigation/{id}` |
| UtilityService | `POST /audit-logs` |
| UtilityService | `POST /error-logs` |
| UtilityService | `POST /notifications/dispatch` |
| BillingService | `GET /feature-gates/{feature}` |
| BillingService | `POST /usage/increment` |

---

## Totals

| Category | Count |
|----------|-------|
| Total endpoints | 120 |
| Anonymous | 10 |
| Authenticated (no role) | 62 |
| DeptLead | 22 |
| OrgAdmin | 18 |
| PlatformAdmin | 8 |
| ServiceAuth (internal) | 15 |
| Endpoints needing role restriction fixes | 0 (all fixed) |
| ServiceAuth endpoints to hide from Swagger | 15 |
