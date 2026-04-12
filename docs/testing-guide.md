# Nexus 2.0 — Testing Guide

Realistic test data, seed scripts, and step-by-step setup for manual and automated testing.

---

## Auto-Seeded Data

The following data is automatically seeded on first startup:

### ProfileService
| Entity | Data |
|--------|------|
| Roles | OrgAdmin (100), DeptLead (75), Member (50), Viewer (25) |
| PlatformAdmin | `admin` / `Admin@123` (forced password change) |
| Notification Types | StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching |
| Navigation Items | Dashboard, Projects, Stories, Boards (Kanban, Sprint, Department, Backlog), Sprints, Members, Departments, Reports, Search, Settings, Invites, Billing |

### UtilityService
| Entity | Data |
|--------|------|
| Department Types | Engineering, QA, DevOps, Product, Design |
| Priority Levels | Critical, High, Medium, Low |
| Task Types | Development, Testing, DevOps, Design, Documentation, Bug |
| Workflow States (Story) | Backlog, Ready, InProgress, InReview, QA, Done, Closed |
| Workflow States (Task) | ToDo, InProgress, InReview, Done |
| Error Codes | INSUFFICIENT_PERMISSIONS, ORGADMIN_REQUIRED, DEPTLEAD_REQUIRED, PLATFORM_ADMIN_REQUIRED |

### BillingService
| Entity | Data |
|--------|------|
| Plans | Free (0/mo), Starter ($29/mo), Professional ($79/mo), Enterprise ($199/mo) |

---

## Realistic Test Data Setup

Follow these steps to populate a fully functional test environment.

### Step 1: Login as PlatformAdmin

```
POST http://localhost:5001/api/v1/auth/login
{
  "email": "admin",
  "password": "Admin@123"
}
```

Save the `accessToken`. Then change the password:

```
POST http://localhost:5001/api/v1/password/forced-change
Authorization: Bearer {{access_token}}
{
  "newPassword": "Platform@2025"
}
```

### Step 2: Create Organization

```
POST http://localhost:5002/api/v1/organizations
Authorization: Bearer {{platform_admin_token}}
{
  "name": "Acme Corp",
  "storyIdPrefix": "ACME"
}
```

Save the `organizationId`. This auto-creates 5 departments: Engineering, QA, DevOps, Product, Design.

### Step 3: Provision OrgAdmin

```
POST http://localhost:5002/api/v1/organizations/{{org_id}}/provision-admin
Authorization: Bearer {{platform_admin_token}}
{
  "email": "jane.admin@acme.com",
  "firstName": "Jane",
  "lastName": "Admin"
}
```

Login as the OrgAdmin (password: auto-generated, check logs or use forced change).

### Step 4: Create Subscription

```
POST http://localhost:5300/api/v1/subscriptions
Authorization: Bearer {{orgadmin_token}}
{
  "planId": "{{professional_plan_id}}"
}
```

### Step 5: Invite Team Members

Create invites for each role type:

```
POST http://localhost:5002/api/v1/invites
Authorization: Bearer {{orgadmin_token}}
```

| Email | First | Last | Role | Department |
|-------|-------|------|------|------------|
| sarah.lead@acme.com | Sarah | Lead | DeptLead | Engineering |
| mike.dev@acme.com | Mike | Developer | Member | Engineering |
| lisa.qa@acme.com | Lisa | Tester | Member | QA |
| tom.viewer@acme.com | Tom | Viewer | Viewer | Product |
| anna.devops@acme.com | Anna | DevOps | DeptLead | DevOps |
| chris.design@acme.com | Chris | Designer | Member | Design |

Accept each invite via the token endpoint, then login to get tokens.

### Step 6: Create Projects

Login as DeptLead (Sarah):

```
POST http://localhost:5003/api/v1/projects
Authorization: Bearer {{deptlead_token}}
```

| Name | Key | Description |
|------|-----|-------------|
| Mobile App | MOB | iOS and Android mobile application |
| Web Platform | WEB | Customer-facing web application |
| API Gateway | API | Backend API infrastructure |

### Step 7: Create Stories

```
POST http://localhost:5003/api/v1/stories
Authorization: Bearer {{member_token}}
```

| Project | Title | Priority | Points | Type |
|---------|-------|----------|--------|------|
| MOB | User login screen | High | 5 | Feature |
| MOB | Push notification setup | Medium | 8 | Feature |
| MOB | App crashes on logout | Critical | 3 | Bug |
| WEB | Dashboard redesign | High | 13 | Feature |
| WEB | Fix pagination on members page | Low | 2 | Bug |
| WEB | Add dark mode support | Medium | 8 | Feature |
| API | Rate limiting middleware | High | 5 | Feature |
| API | Database connection pooling | Medium | 3 | Tech Debt |

### Step 8: Create Sprint

```
POST http://localhost:5003/api/v1/projects/{{mob_project_id}}/sprints
Authorization: Bearer {{deptlead_token}}
{
  "name": "Sprint 1 - Authentication",
  "goal": "Complete user authentication flow",
  "startDate": "2025-07-01",
  "endDate": "2025-07-14"
}
```

Add stories to sprint, then start it.

### Step 9: Create Tasks

```
POST http://localhost:5003/api/v1/tasks
Authorization: Bearer {{member_token}}
{
  "storyId": "{{story_id}}",
  "title": "Implement login API call",
  "taskType": "Development",
  "description": "Connect login form to SecurityService auth endpoint"
}
```

### Step 10: Log Time

```
POST http://localhost:5003/api/v1/time-entries
Authorization: Bearer {{member_token}}
{
  "storyId": "{{story_id}}",
  "date": "2025-07-02",
  "hours": 4.5,
  "description": "Implemented login form and API integration",
  "isBillable": true
}
```

---

## Role Restriction Test Matrix

Test each endpoint with each role to verify access control:

| Action | PlatformAdmin | OrgAdmin | DeptLead | Member | Viewer |
|--------|:---:|:---:|:---:|:---:|:---:|
| **Organizations** |
| Create org | ✅ | ✅ | ✅ | ✅ | ✅ |
| List all orgs | ✅ | ❌ | ❌ | ❌ | ❌ |
| Update org | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update org settings | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update org status | ✅ | ✅ | ❌ | ❌ | ❌ |
| Provision admin | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Departments** |
| Create department | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update department | ✅ | ✅ | ✅ | ❌ | ❌ |
| Change dept status | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update dept prefs | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Team Members** |
| Update own profile | ✅ | ✅ | ✅ | ✅ | ✅ |
| Update other's profile | ✅ | ✅ | ❌ | ❌ | ❌ |
| Change member status | ✅ | ✅ | ❌ | ❌ | ❌ |
| Add to department | ✅ | ✅ | ❌ | ❌ | ❌ |
| Change dept role | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Invites** |
| Create invite | ✅ | ✅ | ✅ | ❌ | ❌ |
| Cancel invite | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Projects** |
| Create project | ✅ | ✅ | ✅ | ❌ | ❌ |
| Update project | ✅ | ✅ | ✅ | ❌ | ❌ |
| Change project status | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Stories** |
| Create/update story | ✅ | ✅ | ✅ | ✅ | ✅ |
| Delete story | ✅ | ✅ | ✅ | ❌ | ❌ |
| Assign/unassign story | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Sprints** |
| Create/start/complete | ✅ | ✅ | ✅ | ❌ | ❌ |
| Add/remove stories | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Time Tracking** |
| Create time entry | ✅ | ✅ | ✅ | ✅ | ✅ |
| Approve/reject | ✅ | ✅ | ✅ | ❌ | ❌ |
| Manage cost rates | ✅ | ✅ | ❌ | ❌ | ❌ |
| Update time policy | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Labels** |
| Create/update label | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete label | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Workflows** |
| Org workflow override | ✅ | ✅ | ❌ | ❌ | ❌ |
| Dept workflow override | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Billing** |
| Subscription CRUD | ✅ | ✅ | ❌ | ❌ | ❌ |
| Admin billing | ✅ | ❌ | ❌ | ❌ | ❌ |
| Admin plan mgmt | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Audit Logs** |
| Query audit logs | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Error Codes** |
| Create/update/delete | ✅ | ✅ | ❌ | ❌ | ❌ |

---

## Running Automated Tests

### Backend (569 tests)

```bash
# All backend tests
dotnet test Nexus-2.0.sln

# Individual service
dotnet test src/backend/SecurityService/SecurityService.Tests
dotnet test src/backend/ProfileService/ProfileService.Tests
dotnet test src/backend/WorkService/WorkService.Tests
dotnet test src/backend/UtilityService/UtilityService.Tests
dotnet test src/backend/BillingService/BillingService.Tests
```

### Frontend (93 tests)

```bash
cd src/frontend
npx vitest --run
```

### Test Coverage by Service

| Service | Unit | Property | Total |
|---------|------|----------|-------|
| SecurityService | 83 | — | 83 |
| ProfileService | 87 | — | 87 |
| WorkService | 159 | 20 | 179 |
| UtilityService | 109 | — | 109 |
| BillingService | 79 | 32 | 111 |
| Frontend | 93 | — | 93 |
| **Total** | | | **662** |
