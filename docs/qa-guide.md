# Nexus 2.0 — QA Guide

Manual QA checklist covering happy paths, edge cases, and role restriction scenarios.

---

## Prerequisites

- Platform running locally or via Docker Compose
- PlatformAdmin credentials: `admin` / `Admin@123` (forced password change on first login)
- Postman collection imported (`postman/nexus-2.0-api.postman_collection.json`)

---

## 1. Authentication & Sessions (SecurityService)

### Happy Paths
- [ ] Login with valid credentials → returns access token + refresh token
- [ ] Forced password change on first login → new password accepted, `isFirstTimeUser` becomes false
- [ ] Refresh token → new access token returned, old refresh token invalidated
- [ ] Logout → session revoked, JWT blacklisted
- [ ] OTP request → OTP generated (check logs/Seq)
- [ ] OTP verify → valid code accepted
- [ ] Password reset request → OTP sent
- [ ] Password reset confirm → password changed, can login with new password
- [ ] List sessions → shows active sessions with device/IP info
- [ ] Revoke single session → session removed
- [ ] Revoke all sessions → all except current revoked

### Edge Cases
- [ ] Login with wrong password → 401, account locks after 5 attempts
- [ ] Login with locked account → 423
- [ ] Refresh with expired token → 401
- [ ] Refresh with reused token → all sessions revoked (rotation detection)
- [ ] OTP verify with wrong code → fails, max 3 attempts
- [ ] OTP verify with expired code → 400
- [ ] Password reset with non-existent email → still returns 200 (no enumeration)
- [ ] Password complexity failure → 400 with `PASSWORD_COMPLEXITY_FAILED`

### Role Restrictions
- [ ] All auth endpoints are public (no role needed) ✅
- [ ] Session endpoints require authentication only ✅

---

## 2. Organizations & Departments (ProfileService)

### Happy Paths
- [ ] PlatformAdmin creates organization → 201, 5 default departments seeded
- [ ] PlatformAdmin provisions OrgAdmin → 201, credentials generated
- [ ] OrgAdmin updates organization name → 200
- [ ] OrgAdmin updates organization settings (sprint duration, story point scale) → 200
- [ ] OrgAdmin creates department → 201
- [ ] DeptLead updates department name → 200
- [ ] OrgAdmin changes department status → 200
- [ ] DeptLead updates department preferences → 200
- [ ] List departments → paginated results
- [ ] Get department members → returns members in department

### Edge Cases
- [ ] Duplicate organization name → 409 `ORGANIZATION_NAME_DUPLICATE`
- [ ] Duplicate story prefix → 409 `STORY_PREFIX_DUPLICATE`
- [ ] Duplicate department name within org → 409 `DEPARTMENT_NAME_DUPLICATE`
- [ ] Duplicate department code within org → 409 `DEPARTMENT_CODE_DUPLICATE`
- [ ] Update story prefix (immutable) → 400 `STORY_PREFIX_IMMUTABLE`

### Role Restrictions
- [ ] Member tries to create department → 403 `ORGADMIN_REQUIRED`
- [ ] Member tries to update org settings → 403 `ORGADMIN_REQUIRED`
- [ ] Member tries to change department status → 403 `ORGADMIN_REQUIRED`
- [ ] Viewer tries to update department → 403 `DEPTLEAD_REQUIRED`
- [ ] DeptLead can update their own department → 200
- [ ] PlatformAdmin can do everything → 200
- [ ] Non-PlatformAdmin tries to list all orgs → 403 `PLATFORM_ADMIN_REQUIRED`
- [ ] Non-PlatformAdmin tries to provision admin → 403 `PLATFORM_ADMIN_REQUIRED`

---

## 3. Team Members & Invites (ProfileService)

### Happy Paths
- [ ] OrgAdmin invites member via email → invite created with 48-hour token
- [ ] Validate invite token → returns org/dept/role info
- [ ] Accept invite → team member created, credentials generated
- [ ] OrgAdmin updates member status (activate/suspend/deactivate) → 200
- [ ] OrgAdmin adds member to department → 200
- [ ] OrgAdmin removes member from department → 200
- [ ] OrgAdmin changes member's department role → 200
- [ ] Member updates own profile → 200
- [ ] Member updates own availability → 200
- [ ] List team members with filters (department, role, status) → paginated

### Edge Cases
- [ ] Invite already-registered email → 409 `INVITE_EMAIL_ALREADY_MEMBER`
- [ ] Accept expired invite → 410
- [ ] Accept already-used invite → 410
- [ ] Deactivate last OrgAdmin → 400 `LAST_ORGADMIN_CANNOT_DEACTIVATE`
- [ ] Add member to department they're already in → 409 `MEMBER_ALREADY_IN_DEPARTMENT`
- [ ] Remove member from last department → 400 `MEMBER_MUST_HAVE_DEPARTMENT`

### Role Restrictions
- [ ] Member tries to invite → 403 `DEPTLEAD_REQUIRED`
- [ ] Member tries to cancel invite → 403 `DEPTLEAD_REQUIRED`
- [ ] Member tries to change another member's status → 403 `ORGADMIN_REQUIRED`
- [ ] Member tries to update another member's profile → 403 `INSUFFICIENT_PERMISSIONS`
- [ ] Member updates own profile → 200 ✅
- [ ] DeptLead creates invite → 200 ✅
- [ ] OrgAdmin manages all member operations → 200 ✅

---

## 4. Projects (WorkService)

### Happy Paths
- [ ] DeptLead creates project with unique key → 201
- [ ] List projects with status filter → paginated
- [ ] Get project details → includes story/sprint counts
- [ ] DeptLead updates project → 200
- [ ] OrgAdmin changes project status → 200
- [ ] Get project cost summary → returns cost data
- [ ] Get project utilization → returns resource data

### Edge Cases
- [ ] Duplicate project name within org → 409 `PROJECT_NAME_DUPLICATE`
- [ ] Duplicate project key → 409 `PROJECT_KEY_DUPLICATE`
- [ ] Invalid project key format → 400 `PROJECT_KEY_INVALID_FORMAT`

### Role Restrictions
- [ ] Member tries to create project → 403 `DEPTLEAD_REQUIRED`
- [ ] DeptLead tries to change project status → 403 `ORGADMIN_REQUIRED`
- [ ] Member can list/view projects → 200 ✅

---

## 5. Stories (WorkService)

### Happy Paths
- [ ] Create story → 201, auto-generated key (e.g., MOB-1)
- [ ] List stories with filters (project, status, priority, assignee, sprint, labels, dates) → paginated
- [ ] Get story by ID → full details with tasks, labels, links
- [ ] Get story by key (e.g., MOB-42) → same as by ID
- [ ] Update story → 200
- [ ] Transition status (Backlog → Ready → InProgress → InReview → QA → Done → Closed) → 200
- [ ] DeptLead assigns story → 200
- [ ] DeptLead unassigns story → 200
- [ ] Create story link → 201
- [ ] Apply label (max 10) → 200
- [ ] List comments on story → 200
- [ ] List activity log → 200

### Edge Cases
- [ ] Invalid status transition (e.g., Backlog → Done) → 400 `INVALID_STORY_TRANSITION`
- [ ] Delete story in active sprint → 400 `STORY_IN_ACTIVE_SPRINT`
- [ ] Apply 11th label → 400 `MAX_LABELS_PER_STORY`

### Role Restrictions
- [ ] Member can create/update stories → 200 ✅
- [ ] Member tries to delete story → 403 `DEPTLEAD_REQUIRED`
- [ ] Member tries to assign story → 403 `DEPTLEAD_REQUIRED`

---

## 6. Tasks (WorkService)

### Happy Paths
- [ ] Create task for story → 201
- [ ] Update task → 200
- [ ] Transition task status (ToDo → InProgress → InReview → Done) → 200
- [ ] DeptLead assigns task → 200
- [ ] Self-assign task → 200
- [ ] Log hours → 200
- [ ] Suggest assignee by task type → returns suggestion

### Role Restrictions
- [ ] Member can create/update/self-assign tasks → 200 ✅
- [ ] Member tries to delete task → 403 `DEPTLEAD_REQUIRED`
- [ ] Member tries to assign task to others → 403 `DEPTLEAD_REQUIRED`

---

## 7. Sprints (WorkService)

### Happy Paths
- [ ] DeptLead creates sprint → 201 (Planning status)
- [ ] DeptLead adds stories to sprint → 200
- [ ] DeptLead starts sprint → 200 (Active status)
- [ ] DeptLead completes sprint → 200 (velocity calculated, incomplete stories → Backlog)
- [ ] Get sprint metrics (burndown, completion rate) → 200
- [ ] Get velocity history → 200
- [ ] Get active sprint → 200

### Edge Cases
- [ ] Start sprint when another is active → 400 `ONLY_ONE_ACTIVE_SPRINT`
- [ ] Add story to non-Planning sprint → 400 `SPRINT_NOT_IN_PLANNING`
- [ ] Sprint date overlap → 400 `SPRINT_OVERLAP`
- [ ] End date before start date → 400 `SPRINT_END_BEFORE_START`

### Role Restrictions
- [ ] Member tries to create/start/complete sprint → 403 `DEPTLEAD_REQUIRED`
- [ ] Member can view sprints and metrics → 200 ✅

---

## 8. Time Tracking & Cost Rates (WorkService)

### Happy Paths
- [ ] Create manual time entry → 201
- [ ] Start timer → 200
- [ ] Stop timer → 200 (time entry auto-created)
- [ ] Get timer status → 200 or 204
- [ ] DeptLead approves time entry → 200
- [ ] DeptLead rejects time entry with reason → 200
- [ ] OrgAdmin creates cost rate → 201
- [ ] OrgAdmin updates time policy → 200

### Edge Cases
- [ ] Start timer when one is already active → 409 `TIMER_ALREADY_ACTIVE`
- [ ] Stop timer when none active → 400 `NO_ACTIVE_TIMER`
- [ ] Duplicate cost rate → 409 `COST_RATE_DUPLICATE`
- [ ] Daily hours exceeded → 400 `DAILY_HOURS_EXCEEDED`

### Role Restrictions
- [ ] Member can create time entries and use timer → 200 ✅
- [ ] Member tries to approve/reject → 403 `DEPTLEAD_REQUIRED`
- [ ] Member tries to create cost rate → 403 `ORGADMIN_REQUIRED`
- [ ] Member tries to update time policy → 403 `ORGADMIN_REQUIRED`

---

## 9. Boards, Search, Reports (WorkService)

### Happy Paths
- [ ] Kanban board with filters → returns columns with stories
- [ ] Sprint board → returns active sprint stories
- [ ] Backlog board → returns unassigned stories
- [ ] Department board → returns stories by department
- [ ] Search stories/tasks → paginated results
- [ ] Velocity report → chart data
- [ ] Department workload report → workload by department
- [ ] Capacity utilization report → utilization data
- [ ] Cycle time report → cycle time metrics
- [ ] Task completion report → completion data

### Role Restrictions
- [ ] All board/search/report endpoints accessible to any authenticated user → 200 ✅

---

## 10. Analytics (WorkService)

### Happy Paths
- [ ] Velocity trends → returns sprint velocity data
- [ ] Resource management → returns resource allocation
- [ ] Project cost analytics → returns cost breakdown
- [ ] Project health → returns health score
- [ ] Dependency analysis → returns dependency graph
- [ ] Bug metrics → returns bug statistics
- [ ] Dashboard summary → returns aggregated data
- [ ] Create/update/delete risk register entry → CRUD works
- [ ] DeptLead views snapshot status → 200

### Role Restrictions
- [ ] Member can view all analytics → 200 ✅
- [ ] Member tries to view snapshot status → 403 `DEPTLEAD_REQUIRED`
- [ ] Member tries to create/update/delete risk → 403 `DEPTLEAD_REQUIRED`

---

## 11. Billing & Subscriptions (BillingService)

### Happy Paths
- [ ] List active plans → returns Free, Starter, Professional, Enterprise
- [ ] OrgAdmin creates subscription (Free plan) → 201, activates immediately
- [ ] OrgAdmin creates subscription (paid plan) → 201, 14-day trial
- [ ] OrgAdmin upgrades plan → 200, prorated
- [ ] OrgAdmin schedules downgrade → 200, effective at period end
- [ ] OrgAdmin cancels subscription → 200, effective at period end
- [ ] OrgAdmin views usage → 200
- [ ] PlatformAdmin lists all subscriptions → 200
- [ ] PlatformAdmin overrides subscription → 200
- [ ] PlatformAdmin cancels subscription → 200
- [ ] PlatformAdmin views usage summary → 200
- [ ] PlatformAdmin creates/updates/deactivates plans → CRUD works

### Edge Cases
- [ ] Create subscription when one exists → 409 `SUBSCRIPTION_ALREADY_EXISTS`
- [ ] Upgrade to lower tier → 400 `INVALID_UPGRADE_PATH`
- [ ] Downgrade when usage exceeds limits → 400 `USAGE_EXCEEDS_PLAN_LIMITS`
- [ ] Cancel already-cancelled subscription → 400 `SUBSCRIPTION_ALREADY_CANCELLED`

### Role Restrictions
- [ ] Member tries subscription operations → 403 `ORGADMIN_REQUIRED`
- [ ] OrgAdmin tries admin billing operations → 403 `PLATFORM_ADMIN_REQUIRED`
- [ ] Member can list plans → 200 ✅

---

## 12. Audit Logs & Notifications (UtilityService)

### Happy Paths
- [ ] OrgAdmin queries audit logs with filters → paginated
- [ ] OrgAdmin queries archived audit logs → paginated
- [ ] User views notification history → own notifications only
- [ ] List error codes → returns all error codes
- [ ] Get reference data (department types, priorities, task types, workflow states) → 200

### Edge Cases
- [ ] PUT/DELETE audit logs → 405 (immutable)

### Role Restrictions
- [ ] Member tries to query audit logs → 403 `ORGADMIN_REQUIRED`
- [ ] Member can view own notification logs → 200 ✅
- [ ] Member tries to create error codes → 403 `ORGADMIN_REQUIRED`
- [ ] Unauthenticated user tries reference data → 401

---

## Cross-Cutting Concerns

### Rate Limiting
- [ ] Rapid login attempts → 429 after threshold
- [ ] Rapid OTP requests → 429 after threshold
- [ ] API rate limits enforced per endpoint

### Error Response Format
- [ ] All errors return `ApiResponse<T>` envelope with `errorCode`, `message`, `correlationId`, `responseCode`, `responseDescription`
- [ ] 403 responses include specific error codes (`ORGADMIN_REQUIRED`, `DEPTLEAD_REQUIRED`, `PLATFORM_ADMIN_REQUIRED`)
- [ ] No stack traces in production error responses

### Pagination
- [ ] All list endpoints support `page` and `pageSize` parameters
- [ ] Default page size is 20
- [ ] Max page size is enforced (100)

### Correlation ID
- [ ] Every response includes `correlationId`
- [ ] Correlation ID propagates across service-to-service calls
