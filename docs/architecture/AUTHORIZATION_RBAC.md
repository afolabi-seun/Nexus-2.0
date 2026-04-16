# Authorization & RBAC

Role hierarchy, middleware enforcement, department scoping, organization scoping, and role attributes.

## Role Hierarchy

Defined in `SecurityService.Domain/Helpers/RoleNames.cs`:

| Role | Permission Level | Scope | Access |
|------|-----------------|-------|--------|
| `PlatformAdmin` | — | Platform-wide | Full access to all organizations, system management |
| `OrgAdmin` | 100 | Organization | Full organization access, settings, billing, member management |
| `DeptLead` | 75 | Department | Department management, sprint/project operations, approvals |
| `Member` | 50 | Department | Create/update stories and tasks, log time |
| `Viewer` | 25 | Department | Read-only access |

`PlatformAdmin` is a separate entity (`PlatformAdmin` table in ProfileService) — not a team member. It has no `organizationId` or `departmentId`.

## RoleAuthorizationMiddleware

Located at `{Service}.Api/Middleware/RoleAuthorizationMiddleware.cs`. Runs after JWT authentication and `JwtClaimsMiddleware`.

Decision flow:

```
1. Not authenticated?           → pass through (public endpoints)
2. Service-auth token?          → pass through (inter-service calls)
3. No role assigned?            → InsufficientPermissionsException
4. Endpoint has [PlatformAdmin]? → reject if not PlatformAdmin
5. PlatformAdmin?               → allow all
6. OrgAdmin?                    → allow all (within org)
7. DeptLead?                    → allow, enforce department scope
8. Member / Viewer?             → allow, enforce department scope
9. Unknown role?                → InsufficientPermissionsException
```

### Department Scope Enforcement

For `DeptLead`, `Member`, and `Viewer`, the middleware checks if the request targets a different department:

```csharp
var routeDeptId = context.Request.RouteValues["departmentId"]?.ToString();
var queryDeptId = context.Request.Query["departmentId"].ToString();
var targetDeptId = routeDeptId ?? queryDeptId;

if (!string.IsNullOrEmpty(targetDeptId) && targetDeptId != userDepartmentId)
    throw new DepartmentAccessDeniedException();
```

This means a `DeptLead` of Department A cannot access Department B's resources.

## OrganizationScopeMiddleware

Runs after role authorization. Extracts `organizationId` from JWT claims and stores it in `HttpContext.Items["OrganizationId"]`. Controllers and services read this to scope all queries to the user's organization.

This is the tenant isolation mechanism — every database query filters by `organizationId`.

## Role Attributes

Custom attributes for endpoint-level access control:

| Attribute | Effect |
|-----------|--------|
| `[PlatformAdmin]` | Only `PlatformAdmin` role can access |
| `[Authorize]` | Any authenticated user (standard ASP.NET) |

Example:

```csharp
[HttpGet]
[PlatformAdmin]
public async Task<IActionResult> ListAllOrganizations(...)
```

Endpoints without `[PlatformAdmin]` are accessible to all authenticated users within their role's scope.

## JwtClaimsMiddleware

Runs after ASP.NET authentication. Extracts claims from the validated JWT and stores them in `HttpContext.Items` for downstream middleware and controllers:

| HttpContext.Items Key | JWT Claim | Used By |
|----------------------|-----------|---------|
| `userId` | `sub` / `userId` | Controllers, services |
| `organizationId` | `organizationId` | OrganizationScopeMiddleware |
| `departmentId` | `departmentId` | RoleAuthorizationMiddleware |
| `roleName` | `roleName` | RoleAuthorizationMiddleware |
| `email` | `email` | Audit logging |
| `serviceId` | `serviceId` | RoleAuthorizationMiddleware (skip for service tokens) |

## Endpoint Access Matrix

The complete 120-endpoint access matrix is documented in [endpoint-restrictions.md](../endpoint-restrictions.md).

Summary pattern:

| Operation | Minimum Role |
|-----------|-------------|
| Read own data | Viewer |
| Create/update stories, tasks, time entries | Member |
| Sprint operations, approvals, department management | DeptLead |
| Organization settings, member management, billing | OrgAdmin |
| Cross-organization operations, system management | PlatformAdmin |

## How Organization Scoping Works

1. User logs in → JWT contains `organizationId` claim
2. `JwtClaimsMiddleware` extracts it to `HttpContext.Items["OrganizationId"]`
3. `OrganizationScopeMiddleware` validates it
4. Controllers read `HttpContext.Items["OrganizationId"]` and pass to service methods
5. Service methods pass `organizationId` to repository queries
6. All queries filter by `WHERE organization_id = @orgId`

There is no global query filter in EF Core — scoping is explicit at the service/repository level.

## Related Docs

- [AUTHENTICATION_AND_SECURITY.md](AUTHENTICATION_AND_SECURITY.md) — How tokens are issued and validated
- [ERROR_HANDLING.md](ERROR_HANDLING.md) — `InsufficientPermissionsException`, `DepartmentAccessDeniedException`
- [endpoint-restrictions.md](../endpoint-restrictions.md) — Full endpoint-role matrix
