# Nexus 2.0 — TODO

Prioritized roadmap for hardening, features, and documentation before release.

---

## Phase 1 — Security & Access Control

- [x] **Endpoint role restriction audit** — Audit every controller endpoint across all 5 services. Ensure Admin/Owner/Member distinctions are enforced. No endpoint should be accidentally open to any authenticated user.
- [x] **PlatformAdmin role** — Super-admin role above org-level roles in SecurityService. Capabilities: manage all orgs (suspend/delete/audit), platform-wide analytics, manage subscription plans & feature gates, user impersonation for support, access to internal/diagnostic endpoints.
- [x] **Hide `[ServiceAuth]` endpoints from Swagger** — Add a Swagger document filter to exclude service-to-service internal endpoints from public API docs.
- [x] **Remove internal endpoints from Postman** — Remove credentials/generate, by-username, and other internal endpoints from the public Postman collection. Optionally maintain a separate internal/dev collection.
- [x] **Endpoint restrictions reference doc** — Comprehensive markdown file (`docs/endpoint-restrictions.md`) listing every endpoint, its HTTP method, required role, and auth type (user JWT, service-to-service, public).

---

## Phase 2 — Communications

- [x] **Email integration** — Set up email sending (e.g., SendGrid, SES). Emails needed for:
  - Account registration (welcome / email verification)
  - OTP delivery (login, password reset)
  - Organization invite (join link)
  - Password reset confirmation
  - Subscription/billing events (plan change, payment failed, invoice)
- [x] **Sprint notifications** — Background service (daily schedule) for:
  - Sprint due soon (e.g., 2 days before end date)
  - Sprint overdue (past end date, still active)
  - Sprint at risk (too many incomplete stories relative to time remaining)
  - Story blocked / stuck notifications

---

## Phase 3 — Documentation

- [x] **QA guide** — Manual QA checklist covering happy paths and edge cases per feature. Include role restriction scenarios (what each role can/cannot do).
- [x] **Testing guide with realistic seed data** — Seed script or guide with realistic orgs, departments, members, projects, stories, sprints. Include role restriction test scenarios for each role type.
- [x] **Update main README** — Reflect new features, phases, role system, email integration, and link to new docs (QA guide, testing guide, endpoint restrictions).
- [ ] **Update QA email templates** — Include role restriction context in QA-related email communications.

---

## Phase 4 — Hardening

- [x] **API rate limiting audit** — Verify rate limits per endpoint (auth endpoints stricter, read endpoints more lenient).
- [x] **Input validation audit** — Ensure FluentValidation covers all edge cases (max lengths, special characters, injection attempts).
- [x] **CORS lockdown** — Production config only allows the frontend origin.
- [x] **Error message sanitization** — Production error responses must not leak stack traces or internal details.
- [x] **Pagination max limits** — All list endpoints have sensible max page sizes to prevent abuse.
- [x] **Soft delete audit** — Deleted entities (orgs, members, projects) properly excluded from all queries.

---

## Phase 5 — Enhancements

- [x] **Health check endpoints** — `/health` on each service for monitoring and Docker health checks.
- [x] **API versioning** — Add `v1` prefix to all routes now (easier before release than after). Already in place — all routes use `/api/v1/`.
- [x] **Export functionality** — CSV/PDF export for stories, sprint reports, time tracking, invoices.
- [x] **Activity feed** — Per-project or per-org feed showing recent actions (builds on existing audit log).
- [x] **Bulk operations** — Bulk move stories between sprints, bulk assign, bulk status change.
- [ ] **Story templates** — Reusable templates for common story types (bug report, feature request, tech debt).
- [ ] **SLA tracking** — Time-to-resolution metrics, especially for bug stories.
- [x] **Archival** — Archive completed sprints/projects to keep active views clean, preserve history.
- [ ] **Webhook support** — Let orgs configure webhooks for key events (Slack, Teams integration).
- [x] **Global search** — Search across stories, projects, members.
- [ ] **Onboarding flow** — Guided setup for new orgs (create first project, invite members, create first sprint).
- [ ] **Keyboard shortcuts** — For Kanban board and common frontend actions.
- [ ] **Offline indicator** — Frontend shows connection status, queues actions when offline.

---

## Phase 6 — Frontend: Page Integration & Data Gaps

Backend APIs exist for analytics, cost, time tracking, and export — but the frontend pages don't use them. These items connect existing backend capabilities to the UI where users expect to find them.

### ProjectDetailPage — Tabs (Priority 1)

The project detail page is the natural hub for all project-scoped data. Currently shows only stats + stories + sprints. Add tabbed layout:

- [x] **Overview tab** — Current content (stats, stories table, sprints table)
- [x] **Analytics tab** — Project health score/trend (`analyticsApi.getProjectHealth`), velocity trends chart (`analyticsApi.getVelocityTrends`), bug metrics (`analyticsApi.getBugMetrics`), active risks count, blocked stories, dependency analysis (`analyticsApi.getDependencies`)
- [x] **Cost & Time tab** — Cost summary + burn rate (`workApi.getProjectCostSummary`), resource utilization (`workApi.getProjectUtilization`), cost trend over time (`workApi.getProjectCostSnapshots`), time entries filtered by project (`timeTrackingApi.listTimeEntries`)
- [x] **Export tab** — CSV export for stories (`workApi.exportStoriesCsv`) and time entries (`workApi.exportTimeEntriesCsv`) scoped to this project

### StoryDetailPage — Time Tracking (Priority 2)

- [x] **Time Logged section** — Show time entries logged against this story (`timeTrackingApi.listTimeEntries({storyId})`). Display total time logged vs estimate. Add between tasks and labels sections.

### SprintDetailPage — Sprint Analytics (Priority 3)

- [x] **Bug metrics section** — Bug count, open/closed, bug rate scoped to sprint (`analyticsApi.getBugMetrics({projectId, sprintId})`)
- [x] **Time logged section** — Total time logged during sprint, per-member breakdown
- [x] **Velocity comparison** — Compare this sprint's velocity with previous sprints (`workApi.getSprintVelocity`)

### MemberProfilePage — Workload & Time (Priority 4)

- [x] **Time logged section** — Time entries logged by this member (`timeTrackingApi.listTimeEntries({memberId})`), total hours this week/month
- [x] **Assigned stories section** — Stories currently assigned to this member with status badges
- [x] **Resource utilization** — Utilization percentage from `analyticsApi.getResourceManagement`

### DepartmentDetailPage — Workload Summary (Priority 5)

- [x] **Workload summary** — Department workload chart (`workApi.getDepartmentWorkloadReport({departmentId})`)
- [ ] **Task overview** — Tasks by status for this department (from department board data)
- [ ] **Workflow overrides** — View/edit department workflow overrides (`workApi.saveDeptWorkflowOverride`)

### DashboardPage — Additional Widgets (Priority 6)

- [x] **Project health widget** — Health scores across all projects with trend indicators
- [ ] **Active bugs widget** — Total open bugs across projects
- [ ] **Pending approvals widget** — Time entry approvals pending (DeptLead/OrgAdmin)

### List Pages — Export Buttons (Priority 7)

- [x] **StoryListPage** — Add CSV export button (`workApi.exportStoriesCsv`)
- [x] **TimeTrackingPage** — Add CSV export button (`workApi.exportTimeEntriesCsv`)

### Settings Integration (Priority 8)

- [x] **Workflow management** — View/edit org-level workflow overrides on SettingsPage (`workApi.getWorkflows`, `workApi.saveOrgWorkflowOverride`)
- [x] **Snapshot status** — Show analytics snapshot status on admin settings (`analyticsApi.getSnapshotStatus`)

---

## Phase 7 — Frontend: User Help System

No contextual help exists in the UI. Users have no guidance on what features do, what actions to take, or how to get started.

### Reusable Components (build first)

- [ ] **PageHeader component** — Title + optional description line + optional dismiss (stored in localStorage). Replaces raw `<h1>` on every page.
- [ ] **HelpTooltip component** — Small `(?)` icon with popover text for field-level help. Used next to non-obvious metrics and form fields.
- [ ] **Enhanced EmptyState usage** — Replace all inline "No data" / "No tasks yet" text with the existing `EmptyState` component + contextual action buttons.

### Layer 1: Contextual Empty States (highest impact)

Replace generic "no data" messages with guidance on what to do next:

- [ ] **ProjectListPage** — "No projects yet. Create your first project to start tracking stories and sprints." + Create Project button
- [x] **StoryListPage** — "No stories found. Stories represent work items your team needs to complete." + Create Story button
- [ ] **SprintListPage** — "No sprints yet. Sprints are time-boxed iterations for delivering stories." + Create Sprint button
- [ ] **SprintDetailPage (no stories)** — "This sprint has no stories. Add stories from the backlog or story list."
- [ ] **KanbanBoardPage** — "No stories on the board. Create stories and assign them to a project to see them here."
- [x] **TimeTrackingPage** — "No time entries yet. Start the timer on a story or log time manually."
- [ ] **AnalyticsDashboardPage** — "Analytics data is generated when sprints are completed. Complete your first sprint to see velocity trends."
- [ ] **MemberListPage** — "No team members yet. Invite members to your organization to get started." + Invite button
- [ ] **DepartmentDetailPage (no members)** — "No members in this department. Add members from the team member list."
- [ ] **ReportsPage** — "Reports require completed sprint data. Complete a sprint to generate velocity, workload, and capacity reports."

### Layer 2: Page-Level Descriptions (medium impact)

One-line help text below each page title. Dismissible per user via localStorage.

- [ ] **Dashboard** — "Your overview of active sprints, assigned tasks, and team velocity."
- [ ] **Projects** — "Manage your organization's projects. Each project contains stories, sprints, and boards."
- [ ] **Stories** — "Work items that represent features, bugs, or tasks. Filter by project, status, or assignee."
- [ ] **Kanban Board** — "Drag stories between columns to update their status. Filter by project or sprint."
- [ ] **Sprint Board** — "View and manage tasks in the active sprint. Drag tasks between status columns."
- [ ] **Sprints** — "Time-boxed iterations for delivering stories. Plan, start, and complete sprints here."
- [ ] **Time Tracking** — "Log time against stories. Use the timer for real-time tracking or add entries manually."
- [ ] **Analytics** — "Project health, velocity trends, bug metrics, and cost analysis. Select a project to view."
- [ ] **Reports** — "Charts showing team performance across sprints. Select a date range to filter."
- [ ] **Members** — "Your organization's team members. Manage roles, departments, and availability."
- [ ] **Departments** — "Organizational units that own tasks. Each department has its own workflow preferences."
- [ ] **Settings** — "Organization-wide settings including sprint duration, story point scale, and notification channels."
- [ ] **Billing** — "Manage your subscription plan, view usage, and update payment details."

### Layer 3: Field-Level Help Tooltips (nice to have)

- [ ] **Story points** — "Relative estimate of effort. Common scales: 1, 2, 3, 5, 8, 13."
- [ ] **Health score** — "Composite score (0–100) based on velocity consistency, bug rate, overdue stories, and active risks."
- [ ] **Burn rate** — "Average daily cost based on billable time entries and cost rates."
- [ ] **WIP limit** — "Maximum stories allowed in this status column. Helps prevent overloading."
- [ ] **Velocity** — "Story points completed per sprint. Higher is better, but consistency matters more."
- [ ] **Completion rate** — "Percentage of committed stories completed in the sprint."
- [ ] **Capacity utilization** — "Percentage of available working hours that were logged as time entries."
- [ ] **Cost rate** — "Hourly rate used to calculate project costs from time entries."

---

## Phase 8 — Future Enhancements

- [ ] **Story templates** — Reusable templates for common story types (bug report, feature request, tech debt).
- [ ] **SLA tracking** — Time-to-resolution metrics, especially for bug stories.
- [ ] **Webhook support** — Let orgs configure webhooks for key events (Slack, Teams integration).
- [ ] **Onboarding flow** — Guided setup for new orgs (create first project, invite members, create first sprint).
- [ ] **Keyboard shortcuts** — For Kanban board and common frontend actions.
- [ ] **Offline indicator** — Frontend shows connection status, queues actions when offline.
- [ ] **Update QA email templates** — Include role restriction context in QA-related email communications.

---

## ✅ Recently Completed

### Frontend Navigation
- [x] **Sidebar restructure** — Reorganized from flat entity-centric list into 4 workflow-based sections (Work, Tracking, Team, Organization). Added Time Tracking, Analytics, Audit Logs, and Notifications to sidebar. Removed Search (header search bar is sufficient). Section-level permission filtering.

### CI/CD
- [x] **CI auto-merge fix** — Replaced `gh pr merge --auto --merge` with `--merge` (direct merge) in CI pipeline. The `--auto` flag failed because the PR's status checks hadn't registered yet ("unstable status" error). Since CI already passed before the merge job runs, auto-merge is unnecessary.

### Redis & Caching
- [x] **Cache TTL optimization** — Reduced cache durations across all services to improve data freshness:
  - ProfileService: dept list 30→10min, dept prefs 30→10min, org settings 60→15min, user prefs 15→5min, resolved prefs 5→2min
  - SecurityService: user cache 15→5min
  - BillingService: plan cache 60→30min (SubscriptionService, StripeWebhookService, TrialExpiryHostedService)
  - WorkService: sprint active 5→2min, sprint metrics 5→3min, analytics dashboard 5→3min
  - Frontend: sprint metrics polling 5→3min (aligned with backend)
- [x] **Redis key centralization** — Added `RedisKeys` static class to each service's `Infrastructure/Redis/` layer. All 42 inline Redis key patterns replaced with typed methods. Added `nexus:` namespace prefix to all keys for shared Redis cluster safety.
- [x] **Rate limiter key inconsistency fix** — SecurityService used `rate:` while other services used `rate_limit:`. Unified to `nexus:rate_limit:` across all services.
- [x] **BillingService outbox retry + DLQ** — Was fire-and-forget. Now has 3 retries with exponential backoff (1s, 2s, 4s) and dead-letter queue, matching ProfileService/WorkService pattern.
- [x] **UtilityService outbox processor** — Added `outbox:billing` to the queue list (was only polling security, profile, work).

### Error Handling & Database
- [x] **PostgreSQL constraint mapping in middleware** — Added `DbUpdateException` handler to `GlobalExceptionHandlerMiddleware` in all 5 services. Maps SQL state `23505` (unique violation) and `23503` (FK violation) to 409 Conflict with constraint name in the response. Acts as safety net for race conditions.
- [x] **Inner exception logging** — Added `InnerExceptionType` to structured log properties in `HandleUnhandledExceptionAsync` across all 5 services for better Seq diagnostics.
- [x] **Database constraint error codes** — Added `UNIQUE_CONSTRAINT_VIOLATION` (9001) and `FOREIGN_KEY_VIOLATION` (9002) to all 5 services' `ErrorCodes.cs`.

### Pagination
- [x] **Pagination normalization** — Added `PaginationHelper.Normalize(ref page, ref pageSize)` to all 5 services' Application layer. Clamps `pageSize` to 1–100 and `page` to minimum 1. Applied to all 22 paginated controller methods across 16 controllers.
- [x] **Frontend pagination gaps** — Added pagination to `DepartmentListPage` and `PlatformAdminOrganizationsPage` (both were fetching all records without page/pageSize). Updated `getAllOrganizations` API to accept `PaginationParams` and return `PaginatedResponse`.

### Documentation
- [x] **Architecture documentation** — 8 guides in `docs/architecture/`:
  - ERROR_HANDLING.md — DomainException hierarchy, GlobalExceptionHandler, PostgreSQL constraint mapping
  - ERROR_CODES.md — Error code registry, per-service ranges, 3-tier resolution
  - API_RESPONSES.md — ApiResponse envelope, correlation ID flow, status code mapping
  - VALIDATION.md — FluentValidation patterns, validation error response shape
  - AUTHENTICATION_AND_SECURITY.md — Login flow, JWT, refresh tokens, OTP, lockout, service-to-service auth
  - AUTHORIZATION_RBAC.md — Role hierarchy, middleware enforcement, department/org scoping
  - INTER_SERVICE_COMMUNICATION.md — Service clients, Polly resilience, outbox pattern, error propagation
  - CODE_STRUCTURE.md — Clean Architecture layers, GenericRepository, folder conventions, FlgStatus pattern
