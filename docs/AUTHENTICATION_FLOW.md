# Authentication Flow

## Overview

SecurityCoreService handles all authentication for the platform. It doesn't store user data — it fetches profiles from ProfileCoreService at login time and manages sessions, tokens, and credentials via Redis.

```
Client
  │
  ▼
SecurityCoreService (port 5001)
  ├── Login → fetch user from ProfileCoreService → issue JWT + session
  ├── Refresh → validate refresh token → issue new token pair
  ├── Logout → blacklist JWT + revoke session
  ├── Password → forced change / reset via OTP → sync hash to ProfileCoreService
  ├── OTP → generate / verify via Redis
  ├── Transaction PIN → create / verify / reset via Redis
  └── Service tokens → issue service-to-service JWTs
```

---

## Login Flow

```
POST /api/v1/auth/login   [AllowAnonymous]
{ "username": "adebayo", "password": "...", "deviceId": "iPhone-15" }
```

### Step-by-Step

```
1. Check account lockout (Redis: wep:lockout:locked:{identity})
   └── Locked? → 423 ACCOUNT_LOCKED

2. Fetch user from ProfileCoreService
   ├── Check Redis cache (wep:user_cache:{identity}, 2 min TTL)
   ├── Cache miss → GET /api/v1/sme-users/by-identity/{identity}
   │                 (falls back to /api/v1/customers/by-identity/{identity})
   └── Not found? → Record failed attempt → 401 INVALID_CREDENTIALS

3. Check account status (FlgStatus)
   └── Not "A"? → 403 ACCOUNT_INACTIVE

4. Verify password (BCrypt)
   ├── Wrong? → Record failed attempt (may trigger lockout) → 401 or 423
   └── Correct? → Continue

5. Anomaly detection (new IP analysis)
   └── Suspicious? → Logged but login proceeds

6. Reset lockout counter

7. Issue JWT access token (15 min TTL)
   Claims: sub, TenantId, RoleId, RoleName, userType, DeviceId, jti

8. Issue refresh token (opaque, 7 day TTL)
   └── SHA-256 hash stored in Redis (wep:refresh:{userId}:{deviceId})

9. Create session in Redis (wep:session:{userId}:{deviceId})

10. Publish device auto-registration event via outbox (async)

11. Audit log: LOGIN_SUCCESS

12. Return LoginResponse with tokens + profile data
```

### Login Response

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "dGhp...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "isFirstTimeUser": false,
  "phoneNo": "+2348012345678",
  "firstName": "Adebayo",
  "lastName": "Ogunlesi",
  "emailAddress": "adebayo@example.com",
  "referenceCode": "USR-20250615-A1B2C3D4",
  "roleId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "roleName": "PlatformAdmin",
  "smeId": "...",
  "smeName": "TechVentures Ltd",
  "smeReferenceCode": "SME-20250615-..."
}
```

Profile fields are returned once at login for client-side caching. The JWT carries only authorization-relevant claims.

### Login Identity Resolution

Users can log in with any of:
- Username (`adebayo`)
- Email (`adebayo@example.com`)
- Phone (`+2348012345678`)

The `by-identity` endpoint in ProfileCoreService searches across all three fields using `IgnoreQueryFilters()` (cross-tenant, since the user's tenant is unknown before authentication).

---

## JWT Structure

### User JWT Claims

| Claim | Example | Purpose |
|-------|---------|---------|
| `sub` | `e5f6a7b8-...` | User ID |
| `TenantId` | `11111111-...` | Tenant isolation |
| `RoleId` | `a1b2c3d4-...` | Role lookup |
| `RoleName` | `PlatformAdmin` | Role-based access |
| `userType` | `SmeUser` or `Customer` | User type routing |
| `DeviceId` | `iPhone-15` | Session binding |
| `jti` | `f1e2d3c4-...` | Token blacklisting |
| `exp` | `1718500000` | Expiry (Unix timestamp) |

### Token Configuration (.env)

```
ACCESS_TOKEN_EXPIRY_MINUTES=15
REFRESH_TOKEN_EXPIRY_DAYS=7
JWT_SECRET_KEY=<your-256-bit-key>
JWT_ISSUER=WEP
JWT_AUDIENCE=WEP
```

---

## Token Refresh

```
POST /api/v1/auth/refresh   [AllowAnonymous]
{ "accessToken": "eyJ...(expired)", "refreshToken": "dGhp..." }
```

```
1. Extract claims from expired access token (without validation)
   └── Get userId, deviceId from claims

2. Fetch stored refresh token hash from Redis
   └── wep:refresh:{userId}:{deviceId}
   └── Not found? → 401 REFRESH_TOKEN_REUSE (session revoked)

3. SHA-256 hash the provided refresh token
   └── Doesn't match stored hash? → 401 REFRESH_TOKEN_REUSE

4. Issue new access token (same claims)
5. Issue new refresh token (rotation)
6. Store new refresh token hash in Redis
7. Blacklist old access token (remaining TTL)

8. Return new token pair
```

Key security properties:
- **Refresh token rotation** — each refresh issues a new refresh token, old one is invalidated
- **Reuse detection** — if a stolen refresh token is used after rotation, the mismatch triggers `REFRESH_TOKEN_REUSE`
- **Old access token blacklisted** — prevents use of the previous access token after refresh

---

## Logout

```
POST /api/v1/auth/logout   [Authorize]
```

```
1. Extract jti from JWT claims
2. Blacklist the JWT in Redis (wep:blacklist:{jti}, remaining TTL)
3. Revoke session (delete wep:session:{userId}:{deviceId})
4. Delete refresh token (delete wep:refresh:{userId}:{deviceId})
5. Audit log: LOGOUT
```

### Token Blacklist Enforcement

`TokenBlacklistMiddleware` runs on every authenticated request (position #8 in pipeline):

```
Request with JWT
  → Extract jti from token
  → Check Redis: EXISTS wep:blacklist:{jti}
  → Exists? → 401 TOKEN_REVOKED
  → Not exists? → Continue
```

---

## Account Lockout

Redis-based sliding window lockout:

| Key | TTL | Purpose |
|-----|-----|---------|
| `wep:lockout:attempts:{identity}` | 15 min | Failed attempt counter |
| `wep:lockout:locked:{identity}` | 30 min | Lock flag |

```
Failed login attempt:
  → INCREMENT wep:lockout:attempts:{identity}
  → Count >= 5? → SET wep:lockout:locked:{identity} (30 min TTL)

Successful login:
  → DELETE wep:lockout:attempts:{identity}
  → DELETE wep:lockout:locked:{identity}
```

---

## Session Management

Sessions are stored in Redis hashes:

```
Key:    wep:session:{userId}:{deviceId}
Type:   Hash
TTL:    Same as access token (15 min, refreshed on activity)
Fields: deviceId, ipAddress, createdAt, lastActivity
```

### Endpoints

```
GET    /api/v1/sessions          List all active sessions (marks current with isCurrent)
DELETE /api/v1/sessions/{id}     Revoke specific session by device ID
DELETE /api/v1/sessions/all      Revoke all sessions except current device
```

---

## Password Flows

### Forced Password Change (First-Time Users)

```
POST /api/v1/password/forced-change   [Authorize]
{ "currentPassword": "temp123!", "newPassword": "MyNew$ecure1" }
```

```
1. Verify current password against stored hash (via ProfileCoreService)
2. Check user.IsFirstTimeUser == true
   └── Already changed? → 400 PASSWORD_REUSE_NOT_ALLOWED
3. Validate against password history (last 5, via PasswordHistoryService)
4. BCrypt hash new password
5. Sync to ProfileCoreService: PATCH /api/v1/sme-users/{id}/password
6. Invalidate user cache in Redis
7. Blacklist current JWT + revoke session (force re-login)
8. Audit log: FORCED_PASSWORD_CHANGE
```

### Password Reset (Forgotten Password)

**Step 1: Request OTP**
```
POST /api/v1/password/reset/request   [AllowAnonymous]
{ "phoneNo": "+2348012345678" }
```
- OTP generated only if phone belongs to a registered user
- Generic response regardless (prevents phone enumeration)

**Step 2: Confirm Reset**
```
POST /api/v1/password/reset/confirm   [AllowAnonymous]
{ "phoneNo": "+2348012345678", "otp": "123456", "newPassword": "MyNew$ecure1" }
```

```
1. Verify OTP
2. Lookup user by phone (unscoped by-identity endpoint)
3. Validate against password history (last 5)
4. BCrypt hash new password
5. Sync to ProfileCoreService
6. Invalidate user cache
7. Audit log: PASSWORD_RESET
```

### Password Strength Policy

All password fields enforce:
- 8–128 characters
- At least one uppercase, one lowercase, one digit, one special character

### Password History

`PasswordHistoryService` stores the last 5 password hashes per user in Redis. New passwords are checked against all 5 — reuse is rejected with `PASSWORD_RECENTLY_USED`.

---

## OTP System

```
POST /api/v1/auth/otp/request    { "phoneNo": "+234..." }
POST /api/v1/auth/otp/verify     { "phoneNo": "+234...", "otp": "123456" }
```

| Key | TTL | Value |
|-----|-----|-------|
| `wep:otp:{identity}` | 5 min | `{code}:{attempts}` |

- 6-digit numeric code
- Max 3 verification attempts before `OTP_MAX_ATTEMPTS` (429)
- OTP consumed on successful verification (deleted from Redis)
- Notification sent via outbox (SMS + Email)

---

## Transaction PIN

4-digit PIN required before financial transactions. Stored as BCrypt hash in Redis.

```
POST /api/v1/transaction-pin/create   { "pin": "1234" }
POST /api/v1/transaction-pin/verify   { "pin": "1234" }
POST /api/v1/transaction-pin/reset    { "otp": "123456", "newPin": "5678" }
```

| Key | TTL | Value |
|-----|-----|-------|
| `wep:pin:{userId}` | Permanent | BCrypt hash |

- Max 5 failed verification attempts → `TRANSACTION_PIN_LOCKED`
- Reset requires OTP verification
- Audit logged: create, verify failure, reset

---

## Service-to-Service Authentication

```
POST /api/v1/service-tokens/issue   [AllowAnonymous, hidden from Swagger]
{ "serviceId": "ProfileCoreService", "serviceName": "ProfileCoreService" }
```

See [Inter-Service Communication](./INTER_SERVICE_COMMUNICATION.md#service-to-service-jwt-authentication) for full details on service JWT issuance, caching, and ACL.

---

## Credential Generation (SME Onboarding)

```
POST /api/v1/auth/credentials/generate   [ServiceAuth, hidden from Swagger]
```

Called by ProfileCoreService during SME onboarding:

```
1. Generate username (caller-provided or auto: firstname.lastname{random4})
2. Generate temporary password (12 chars, meets complexity policy)
3. BCrypt hash the password
4. Create SmeUser in ProfileCoreService (POST /api/v1/sme-users)
5. Publish CredentialGenerated notification (SMS + Email with username + temp password)
6. Audit log: CREDENTIALS_GENERATED
7. Return username (password never in response — only via notification)
```

---

## Redis Key Summary

| Key Pattern | TTL | Purpose |
|-------------|-----|---------|
| `wep:session:{userId}:{deviceId}` | 15 min | Active session |
| `wep:refresh:{userId}:{deviceId}` | 7 days | Refresh token hash |
| `wep:blacklist:{jti}` | Remaining JWT TTL | Revoked access tokens |
| `wep:otp:{identity}` | 5 min | OTP code + attempt counter |
| `wep:pin:{userId}` | Permanent | Transaction PIN hash |
| `wep:lockout:attempts:{identity}` | 15 min | Failed login counter |
| `wep:lockout:locked:{identity}` | 30 min | Account lock flag |
| `wep:user_cache:{identity}` | 2 min | Login user profile cache |
| `wep:service_token:{serviceId}` | 23 hours | Service-to-service JWT |

---

## Security Properties

| Property | Implementation |
|----------|---------------|
| Passwords never in responses | Temp passwords delivered only via SMS/Email notification |
| Password hashing | BCrypt (work factor default) |
| Token blacklisting | Redis-based JTI blacklist, checked on every request |
| Refresh token rotation | New refresh token on every refresh, old one invalidated |
| Reuse detection | Refresh token hash mismatch → REFRESH_TOKEN_REUSE |
| Account lockout | 5 failed attempts → 30 min lock |
| OTP brute-force protection | 3 attempts per OTP, 5 min expiry |
| PIN brute-force protection | 5 failed attempts → TRANSACTION_PIN_LOCKED |
| Phone enumeration prevention | Generic responses on reset/OTP for unregistered phones |
| User cache invalidation | Cache cleared on password change/reset |
| Session isolation | Per-device sessions, revoke individual or all |

---

## Related Documentation

- [Inter-Service Communication](./INTER_SERVICE_COMMUNICATION.md) — Service JWT, Polly, header propagation
- [Error Management](./ERROR_MANAGEMENT.md) — Error codes 2001–2022 (SecurityCoreService range)
- [Authorization & RBAC](./AUTHORIZATION_RBAC.md) — Roles, attributes, operator restrictions
