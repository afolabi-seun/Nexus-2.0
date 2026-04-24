# Authorization & RBAC

## Overview

WEP enforces access control at three levels:

| Level | Mechanism | Where |
|-------|-----------|-------|
| **Role-based** | Auth attributes + middleware | Controllers (all services) + SecurityCoreService middleware |
| **Tenant-scoped** | TenantScopeMiddleware + EF Core query filters | All services |
| **Dynamic restrictions** | Operator restriction prefixes (Redis) | SecurityCoreService middleware |

---

## Roles

Three seeded roles, immutable:

| Role | ID | Description |
|------|----|-------------|
| PlatformAdmin | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` | Full access — manages SME, staff, customers, wallets, settings |
| Operator | `b2c3d4e5-f6a7-8901-bcde-f12345678901` | Limited access — customers, transactions, bill payments. Subject to dynamic restrictions |
| Customer | `c3d4e5f6-a7b8-9012-cdef-123456789012` | Self-service — own profile, wallet, beneficiaries, devices, payments |

The role is embedded in the JWT as `RoleName` claim and extracted by `JwtClaimsMiddleware` into `HttpContext.Items["RoleName"]`.

---

## Authorization Attributes

Three custom `IAuthorizationFilter` attributes enforce role-based access on controllers and actions:

### [PlatformAdmin]

Restricts to PlatformAdmin role only. Returns 403 `INSUFFICIENT_PERMISSIONS` for all other roles.

```csharp
[PlatformAdmin]
[HttpPost("create")]
public async Task<IActionResult> CreateStaffUser([FromBody] SmeUserCreateRequest request) => ...
```

Implementation:
```csharp
public class PlatformAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var roleClaim = user.FindFirst("RoleName");
        if (roleClaim?.Value != RoleNames.PlatformAdmin)
            context.Result = 403 INSUFFICIENT_PERMISSIONS;
    }
}
```

### [StaffOnly]

Allows PlatformAdmin and Operator. Blocks Customer role.

```csharp
[StaffOnly]
[HttpPost]
public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateRequest request) => ...
```

Implementation:
```csharp
public class StaffOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var roleClaim = user.FindFirst("RoleName");
        if (roleClaim?.Value != RoleNames.PlatformAdmin &&
            roleClaim?.Value != RoleNames.Operator)
            context.Result = 403 INSUFFICIENT_PERMISSIONS;
    }
}
```

### [ServiceAuth]

Validates service-to-service JWT tokens. Checks for `serviceId` claim. Used on internal endpoints hidden from Swagger.

```csharp
[ServiceAuth]
[ApiExplorerSettings(IgnoreApi = true)]
[HttpPost("credentials/generate")]
public async Task<IActionResult> GenerateCredentials(...) => ...
```

### [Authorize]

Standard ASP.NET `[Authorize]` — any authenticated user (PlatformAdmin, Operator, or Customer).

```csharp
[Authorize]
[HttpGet("me")]
public async Task<IActionResult> GetMyProfile() => ...
```

### Attribute Usage by Endpoint Pattern

| Attribute | Who Can Access | Example Endpoints |
|-----------|---------------|-------------------|
| `[PlatformAdmin]` | PlatformAdmin only | Create staff, manage SME, error codes, operator restrictions |
| `[StaffOnly]` | PlatformAdmin + Operator | Create customer, manage invites, KYC approval |
| `[Authorize]` | Any authenticated user | Get own profile, list devices, manage beneficiaries |
| `[ServiceAuth]` | Service-to-service only | Credential generation, password sync, internal lookups |
| `[AllowAnonymous]` | No auth required | Login, refresh, OTP, password reset, onboarding, invite accept |

---

## Middleware-Level Authorization (SecurityCoreService)

SecurityCoreService has two additional middleware components that enforce authorization beyond the attribute level:

### RoleAuthorizationMiddleware

Runs after `JwtClaimsMiddleware`. Enforces two types of checks:

**1. Exact-path role requirements:**
```csharp
private static readonly Dictionary<string, HashSet<string>> RoleRequirements = new()
{
    { "/api/v1/auth/credentials/generate", new() { "PlatformAdmin", "Service" } },
    { "/api/v1/service-tokens/issue", new() { "Service" } }
};
```

**2. Dynamic operator restrictions (Redis):**
```csharp
if (roleName == "Operator")
{
    if (await operatorRestrictionService.IsRestrictedAsync(path))
        throw new InsufficientPermissionsException();
}
```

Service-auth tokens (with `serviceId` claim) bypass all role checks.

### FirstTimeUserMiddleware

Blocks first-time users from accessing any endpoint except `/api/v1/password/forced-change`:

```csharp
if (isFirstTimeUser && !path.Equals("/api/v1/password/forced-change"))
{
    // 403 FIRST_TIME_USER_RESTRICTED
    // "First-time users must change their password before accessing other resources."
}
```

This ensures newly onboarded users can't skip the password change step.

---

## Operator Restrictions (Dynamic)

PlatformAdmin can dynamically restrict which API paths Operators can access, stored in Redis.

### Default Restrictions

Seeded on startup:
```
/api/v1/settings
/api/v1/staff
/api/v1/wallets
/api/v1/reports
/api/v1/sme-users
/api/v1/tenants
/api/v1/smes
```

### Management Endpoints

```
GET    /api/v1/operator-restrictions       [PlatformAdmin]  List all restricted prefixes
POST   /api/v1/operator-restrictions       [PlatformAdmin]  Add a prefix
DELETE /api/v1/operator-restrictions       [PlatformAdmin]  Remove a prefix
```

### How It Works

```
Operator makes request: GET /api/v1/sme-users
  │
  ▼
RoleAuthorizationMiddleware
  ├── RoleName == "Operator"
  ├── Load restricted prefixes from Redis (wep:operator_restrictions)
  ├── "/api/v1/sme-users" starts with "/api/v1/sme-users" → MATCH
  └── Throw InsufficientPermissionsException → 403
```

Redis key: `wep:operator_restrictions` (Redis Set)

If Redis is unavailable, the hardcoded defaults are used as fallback.

Changes take effect immediately — no service restart needed. All changes are audit logged.

---

## Tenant Scoping

Tenant isolation is enforced at the middleware level, not just the database level. See [Data Model Patterns](./DATA_MODEL_PATTERNS.md#multi-tenant-isolation) for the full 3-level isolation model.

### TenantScopeMiddleware

Runs after `JwtClaimsMiddleware` (position #10 in pipeline):

```
Authenticated request
  │
  ▼
Read TenantId from HttpContext.Items (set by JwtClaimsMiddleware from JWT)
  │
  ├── Service-auth token? → Read X-Tenant-Id header instead
  │
  ├── Route has tenantId param? → Validate matches JWT TenantId
  │   └── Mismatch? → 403 TENANT_MISMATCH
  │
  ├── Query has tenantId param? → Validate matches JWT TenantId
  │   └── Mismatch? → 403 TENANT_MISMATCH
  │
  └── Set DbContext.TenantId → EF Core global query filters activate
```

This means:
- A user from Tenant A cannot access Tenant B's data even if they know the IDs
- Route/query parameter tampering is caught before the request reaches the controller
- Service-to-service calls propagate tenant context via `X-Tenant-Id` header

---

## JWT Claims → HttpContext.Items Mapping

`JwtClaimsMiddleware` extracts claims and stores them for downstream use:

| JWT Claim | HttpContext.Items Key | Used By |
|-----------|----------------------|---------|
| `sub` / `userId` | `UserId` | Audit logging, rate limiting |
| `TenantId` | `TenantId` (as `Guid`) | TenantScopeMiddleware, DbContext |
| `RoleId` | `RoleId` | — |
| `RoleName` | `RoleName` | Auth attributes, RoleAuthorizationMiddleware |
| `IsFirstTimeUser` | `IsFirstTimeUser` | FirstTimeUserMiddleware (SecurityCoreService only) |

---

## Authorization Flow Summary

```
Request arrives
  │
  ▼
Authentication (JWT Bearer validation)
  │ Invalid/missing? → 401
  ▼
JwtClaimsMiddleware (extract claims → HttpContext.Items)
  │
  ▼
TokenBlacklistMiddleware (check wep:blacklist:{jti})
  │ Blacklisted? → 401 TOKEN_REVOKED
  ▼
FirstTimeUserMiddleware (SecurityCoreService only)
  │ First-time user + not /password/forced-change? → 403
  ▼
RoleAuthorizationMiddleware (SecurityCoreService only)
  │ Exact-path role check failed? → 403
  │ Operator + restricted path? → 403
  ▼
TenantScopeMiddleware
  │ Route/query tenantId mismatch? → 403 TENANT_MISMATCH
  │ Set DbContext.TenantId
  ▼
Controller
  │
  ▼
Auth Attribute ([PlatformAdmin], [StaffOnly], [Authorize])
  │ Wrong role? → 403 INSUFFICIENT_PERMISSIONS
  ▼
Action executes
```

---

## Endpoint-Role Matrix

### ProfileCoreService

| Endpoint | PlatformAdmin | Operator | Customer | No Auth |
|----------|:---:|:---:|:---:|:---:|
| `POST /api/v1/onboarding/complete` | — | — | — | ✅ |
| `POST /api/v1/customers` | ✅ | ✅ | — | — |
| `GET /api/v1/customers` | ✅ | ✅ | — | — |
| `GET /api/v1/customers/me` | — | — | ✅ | — |
| `POST /api/v1/sme-users/create` | ✅ | — | — | — |
| `POST /api/v1/invites` | ✅ | ✅ | — | — |
| `POST /api/v1/invites/{token}/accept` | — | — | — | ✅ |
| `GET /api/v1/devices` | ✅ | ✅ | ✅ | — |
| `GET /api/v1/beneficiaries` | — | — | ✅ | — |
| `POST /api/v1/kyc/{id}/approve` | ✅ | ✅ | — | — |

### SecurityCoreService

| Endpoint | PlatformAdmin | Operator | Customer | No Auth |
|----------|:---:|:---:|:---:|:---:|
| `POST /api/v1/auth/login` | — | — | — | ✅ |
| `POST /api/v1/auth/refresh` | — | — | — | ✅ |
| `POST /api/v1/auth/logout` | ✅ | ✅ | ✅ | — |
| `POST /api/v1/password/forced-change` | ✅ | ✅ | ✅ | — |
| `POST /api/v1/password/reset/request` | — | — | — | ✅ |
| `POST /api/v1/password/reset/confirm` | — | — | — | ✅ |
| `GET /api/v1/sessions` | ✅ | ✅ | ✅ | — |
| `POST /api/v1/transaction-pin/create` | ✅ | ✅ | ✅ | — |
| `GET /api/v1/operator-restrictions` | ✅ | — | — | — |

### General Pattern

| Attribute | Endpoints |
|-----------|-----------|
| No auth | Login, refresh, OTP, password reset, onboarding, invite accept |
| `[Authorize]` (any role) | Own profile, sessions, devices, beneficiaries, cards, transaction PIN |
| `[StaffOnly]` | Customer CRUD, invites, KYC management, transactions |
| `[PlatformAdmin]` | Staff CRUD, SME management, operator restrictions, error codes, settings |
| `[ServiceAuth]` | Credential generation, password sync, internal lookups |

---

## Related Documentation

- [Authentication Flow](./AUTHENTICATION_FLOW.md) — Login, JWT, sessions, password flows
- [Data Model Patterns](./DATA_MODEL_PATTERNS.md) — Tenant isolation at the database level
- [Error Management](./ERROR_MANAGEMENT.md) — Error codes for auth/permission failures (2001–2022)
- [API Reference](./API_REFERENCE.md) — Complete endpoint-by-role matrix
