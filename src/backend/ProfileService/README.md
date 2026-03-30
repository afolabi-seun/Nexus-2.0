# ProfileService

Organization and team member management microservice for the Nexus 2.0 platform.

- **Port:** 5002
- **Database:** `nexus_profile` (PostgreSQL)
- **Base path:** `/api/v1`

## Responsibilities

- **Organization management** — Create, update, settings (sprint duration, story point scale), status changes (active/suspended/deactivated)
- **Department management** — 5 default departments seeded on org creation; CRUD, status, preferences, member listing
- **Team member profiles** — CRUD, status, availability, multi-department membership with per-department roles
- **Role management** — 5 roles: OrgAdmin (100), DeptLead (75), Member (50), Viewer (25), PlatformAdmin (200)
- **Invitation system** — OTP-based invites with 48-hour expiry, cryptographic tokens, accept/cancel flows
- **Device management** — Track up to 5 devices per user, set primary device
- **User preferences** — Theme (light/dark/system), date format, time format, language
- **Notification settings** — Per-type channel preferences (Email/Push/InApp)
- **Platform admin operations** — List all orgs, provision OrgAdmin for an org
- **DB-driven navigation** — Sidebar items stored in DB, filtered by role permission level

## API Endpoints

### Organizations (`/api/v1/organizations`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/organizations` | Bearer | Create organization |
| GET | `/organizations` | PlatformAdmin | List all organizations |
| GET | `/organizations/{id}` | Bearer | Get organization by ID |
| PUT | `/organizations/{id}` | Bearer | Update organization |
| PATCH | `/organizations/{id}/status` | Bearer | Update org status |
| PUT | `/organizations/{id}/settings` | Bearer | Update org settings |
| POST | `/organizations/{id}/provision-admin` | PlatformAdmin | Provision OrgAdmin |

### Team Members (`/api/v1/team-members`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/team-members` | Bearer | List members (paginated) |
| GET | `/team-members/{id}` | Bearer | Get member by ID |
| PUT | `/team-members/{id}` | Bearer | Update member |
| PATCH | `/team-members/{id}/status` | Bearer | Update member status |
| PATCH | `/team-members/{id}/availability` | Bearer | Update availability |
| POST | `/team-members/{id}/departments` | Bearer | Add to department |
| DELETE | `/team-members/{id}/departments/{deptId}` | Bearer | Remove from department |
| PATCH | `/team-members/{id}/departments/{deptId}/role` | Bearer | Change department role |
| GET | `/team-members/by-email/{email}` | Bearer | Lookup by email |
| PATCH | `/team-members/{id}/password` | Service | Update password hash |

### Departments (`/api/v1/departments`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/departments` | Bearer | Create department |
| GET | `/departments` | Bearer | List departments |
| GET | `/departments/{id}` | Bearer | Get department by ID |
| PUT | `/departments/{id}` | Bearer | Update department |
| PATCH | `/departments/{id}/status` | Bearer | Update status |
| GET | `/departments/{id}/members` | Bearer | List department members |
| GET | `/departments/{id}/preferences` | Bearer | Get department preferences |
| PUT | `/departments/{id}/preferences` | Bearer | Update department preferences |

### Invites (`/api/v1/invites`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/invites` | Bearer | Create invite |
| GET | `/invites` | Bearer | List pending invites |
| GET | `/invites/{token}/validate` | Public | Validate invite token |
| POST | `/invites/{token}/accept` | Public | Accept invite |
| DELETE | `/invites/{id}` | Bearer | Cancel invite |

### Other Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/roles` | Bearer | List roles |
| GET | `/roles/{id}` | Bearer | Get role by ID |
| GET | `/preferences` | Bearer | Get user preferences |
| PUT | `/preferences` | Bearer | Update preferences |
| GET | `/preferences/resolved` | Bearer | Get resolved preferences (user → dept → org cascade) |
| GET | `/devices` | Bearer | List devices |
| PATCH | `/devices/{id}/primary` | Bearer | Set primary device |
| DELETE | `/devices/{id}` | Bearer | Remove device |
| GET | `/notification-settings` | Bearer | Get notification settings |
| PUT | `/notification-settings/{typeId}` | Bearer | Update notification setting |
| GET | `/notification-types` | Bearer | List notification types |
| GET | `/navigation` | Bearer | Get navigation items (role-filtered) |
| GET | `/platform-admins/by-username/{username}` | Service | Get admin by username |
| PATCH | `/platform-admins/{id}/password` | Service | Update admin password |

## How to Run

```bash
cd src/backend/ProfileService/ProfileService.Api
cp .env.example .env   # Edit with your values
dotnet run
```

Service starts at `http://localhost:5002`. Swagger UI at `/swagger`.

## Environment Variables

See [`.env.example`](ProfileService.Api/.env.example) for all variables. Key settings:

| Variable | Description |
|----------|-------------|
| `DATABASE_CONNECTION_STRING` | PostgreSQL connection string |
| `REDIS_CONNECTION_STRING` | Redis host:port |
| `JWT_SECRET_KEY` | Shared JWT signing key |
| `SECURITY_SERVICE_BASE_URL` | SecurityService URL for credential generation |
| `UTILITY_SERVICE_BASE_URL` | UtilityService URL for audit logging |
| `INVITE_EXPIRY_HOURS` | Invite token TTL (default: 48) |
| `MAX_DEVICES_PER_USER` | Max registered devices (default: 5) |
| `SERVICE_SECRET` | Shared secret for service-to-service auth |
